# 03_AzureFunction｜場勘諮詢 → LINE 推播

`02_website` 的場勘諮詢表單送出後，由本後端接收、驗證、節流，再用 LINE Messaging API 推播到員工群組。
同一個 Function App 也是 LINE channel 的 webhook 端點 —— webhook 除了接事件，也是取得推播目標 `groupId` 的唯一途徑。

## 端點

| 方法 | 路徑 | 授權 | 用途 |
| --- | --- | --- | --- |
| `POST` | `/api/inquiry` | anonymous | 表單送出。驗證 → 節流 → 推播 LINE → 存 Table |
| `POST` | `/api/line/webhook` | anonymous | LINE webhook。驗簽 → 記錄來源 ID → 必要時回覆該 ID |
| `GET` | `/api/line/sources` | function key | 列出 webhook 記錄過的 ID，用來填 `LINE_TO_IDS` |

目前部署位置：

- Inquiry API：`https://jenwa-inquiry-func.azurewebsites.net/api/inquiry`
- LINE webhook：`https://jenwa-inquiry-func.azurewebsites.net/api/line/webhook`

## 防濫用

端點公開，而 LINE 免費方案每月只有 **200 則**推播額度，因此保護的重點不只是擋垃圾訊息，更是避免額度被一次灌爆：

- **honeypot**：表單有一個移出畫面的 `company_url` 欄位。有填 → 回 `200 {"ok":true}` 但不推播，讓自動送單程式得不到任何訊號。
- **填表時間**：前端帶 `elapsed`（毫秒）。小於 2 秒 → 同上靜默丟棄。
- **節流**（Azure Table `throttle`）：同 IP 60 秒 1 次、單日 20 次；同電話（只取數字比對）10 分鐘 1 次 → 回 `429`。IP 與電話都以 SHA-256 雜湊存放，不存原文。單日上限刻意放寬，因為行動網路大量共用公網 IP，抓太緊會擋掉不相干的真實客戶；真正擋洪水的是 60 秒間隔，最後一道防線是月額度上限。
- **月額度上限**：`MONTHLY_PUSH_CAP`（預設 150）。達上限後**仍然收單、仍然存 Table**，只跳過推播並記 warning，回 `200 {"ok":true,"queued":true}` —— 客戶不會被擋在門外，商機也不會遺失。
- **CORS**：瀏覽器用的標頭由 **Function App 平台 CORS** 提供，`provision.ps1` 依 `-AllowedOrigins` 同步設定。Functions host 會自己回應 preflight `OPTIONS` 且**完全不會呼叫 Function**，所以白名單必須設在平台層；程式碼裡只保留 `Origin` 比對，不在白名單就回 `403`。兩邊都寫標頭會產生重複的 `Vary` / `Access-Control-Allow-Origin`，因此程式碼刻意不再輸出 CORS 標頭。

推播失敗或額度用盡時，諮詢內容仍在 Table `inquiries`（`Pushed` 欄位記錄是否送達），可事後補撈。

## App Settings

憑證只存在 Azure App Settings，不進 git。

| 名稱 | 說明 |
| --- | --- |
| `LINE_CHANNEL_ACCESS_TOKEN` | Messaging API channel access token |
| `LINE_CHANNEL_SECRET` | 用於驗證 `X-Line-Signature` |
| `LINE_TO_IDS` | 推播目標，逗號分隔可多個。**空值＝不會推播** |
| `ALLOWED_ORIGINS` | 允許的網站來源，逗號分隔。`*` 或空值＝全部允許。**必須與平台 CORS 一致**，改的時候用 `provision.ps1 -AllowedOrigins`（會同步兩邊）。預設含 `npm run dev` 的 4190 與 VS Code Live Server 的 5500 |
| `MONTHLY_PUSH_CAP` | 每月推播上限，預設 150 |
| `AzureWebJobsStorage` | Functions 執行與三張 Table 共用同一個儲存體帳戶 |

## Azure 資源

`jenwa-rg`（japaneast）：

| 資源 | 名稱 | 說明 |
| --- | --- | --- |
| Storage | `stjenwainquiry` | Functions 與 Table（`throttle`／`inquiries`／`linesources`） |
| Log Analytics | `jenwa-inquiry-logs` | App Insights 的工作區 |
| App Insights | `jenwa-inquiry-ai` | 記錄與例外 |
| Function App | `jenwa-inquiry-func` | Flex Consumption（FC1、例項下限 0）／dotnet-isolated 9 |

## 操作

```powershell
# 1. 建立／更新資源與 App Settings（可重複執行）
./scripts/provision.ps1

# 2. 發佈並部署（dotnet publish + az zip deploy，不需 Azure Functions Core Tools）
./scripts/deploy.ps1

# 3. 設定並驗證 LINE webhook
./scripts/set-line-webhook.ps1
```

本機驗證（同樣不需 Core Tools）：

```powershell
dotnet build ./src/Jenwa.Inquiry
dotnet test  ./tests/Jenwa.Inquiry.Tests   # 驗簽、欄位驗證、honeypot、訊息組字、CORS 與來源 IP 解析
```

`func start` 需要 Azure Functions Core Tools 與 Node 18+，本機目前都沒有，因此純邏輯以 `dotnet test` 覆蓋，整合行為以部署後的煙霧測試驗證。要在本機跑整個 host 時，把 `local.settings.json.example` 複製成 `local.settings.json` 再填值（該檔已列入 `.gitignore`）。

查看記錄：

```powershell
az monitor app-insights query --app jenwa-inquiry-ai -g jenwa-rg -o table `
  --analytics-query "traces | where timestamp > ago(1h) | project timestamp, message | order by timestamp desc | take 30"
```

## LINE 端必須手動設定的三件事

Messaging API 沒有對應的 API，只能在主控台操作：

1. **LINE Developers Console → Messaging API → 開啟「Use webhook」。**
   端點已註冊但 `active: false` 時，LINE 完全不會送出事件。
2. **LINE Official Account Manager → 回應設定 → 允許「加入群組及多人聊天室」。**
   沒開啟的話，bot 無法被邀進員工群組。
3. **LINE Official Account Manager → 回應設定 → 關閉「自動回應訊息」。**
   否則自動回應會與 webhook 的回覆互相干擾。

## 取得 `groupId`（一次性）

Push API 必須指定收訊者，而 group id 只會在 webhook 事件裡出現，無法從憑證推導。

1. 完成上述三項主控台設定。
2. 建立員工 LINE 群組，邀請 `@515gjwug`。
3. bot 會在加入時回覆該群組的 ID。之後在群組裡傳 `id` 也可以再問一次。
   或用 function key 查詢：`GET /api/line/sources?code=<function key>`。
4. 寫回 App Setting：

```powershell
az functionapp config appsettings set -g jenwa-rg -n jenwa-inquiry-func --settings LINE_TO_IDS=<groupId>
```

設定完立即生效，不需重新部署。

## 部署後煙霧測試

以 UTF-8 檔案送出，避免終端編碼把中文弄壞：

```powershell
$api = 'https://jenwa-inquiry-func.azurewebsites.net/api/inquiry'
$body = @{ name='測試'; phone='0912345678'; location='台中市'; elapsed=9000 } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri $api -ContentType 'application/json; charset=utf-8' -Body ([Text.Encoding]::UTF8.GetBytes($body))
```

| 情境 | 預期 |
| --- | --- |
| CORS preflight（`OPTIONS`，帶 `Origin` 與 `Access-Control-Request-Method: POST`） | `204`，且回應帶 `Access-Control-Allow-Origin`。**沒有這個標頭，瀏覽器根本不會發出 POST**，而 curl 測不出來 |
| 正常送出 | `200 {"ok":true}`，LINE 群組收到訊息。若回 `{"ok":true,"queued":true}` 表示沒送達 LINE（`LINE_TO_IDS` 空、額度用盡或推播失敗） |
| 同 IP 60 秒內／同電話 10 分鐘內再送 | `429` |
| `company_url` 有值 | `200 {"ok":true}`，群組**沒有**新訊息 |
| `elapsed` 小於 2000 | 同上 |
| 缺 `phone` | `400`，訊息指出要填電話 |
| 沒有 JSON content-type | `415` |
| `Origin` 不在白名單 | `403` |
| webhook 簽章錯誤或缺少 | `403` |
| LINE 主控台的 Verify | `success`／HTTP 200 |
| 額度已達 `MONTHLY_PUSH_CAP` | `200 {"ok":true,"queued":true}`，資料仍存 Table |

## 待辦

- **輪替 LINE 憑證。** `AGENTS.md` 的 channel secret 與 access token 目前明文 commit 在 `github.com/swzinfotech/jenwa`。確認本端點運作正常後，請到 LINE Developers Console 重新產生，再用 `provision.ps1 -ChannelAccessToken ... -ChannelSecret ...` 寫入 App Settings，並把 `AGENTS.md` 的值改為指向 App Settings 的說明文字。
- **設定正式網域。** 一定要透過 `provision.ps1` 改，**不要**只改 `ALLOWED_ORIGINS` app setting —— 平台 CORS 沒一起更新的話，瀏覽器仍會在 preflight 就被擋掉：

  ```powershell
  ./scripts/provision.ps1 -AllowedOrigins "https://<domain>,http://127.0.0.1:4190,http://localhost:4190,http://127.0.0.1:5500,http://localhost:5500"
  ```

  傳入的清單會**整批取代**現有設定（平台 CORS 與 app setting 同步），所以開發用的埠要一起列進去，或直接改 `provision.ps1` 的 `$AllowedOrigins` 預設值。前端則以 `INQUIRY_API=<endpoint> npm run build` 重新產生。
- **.NET 9 的支援到 2026-11-10。** 之後把 `Jenwa.Inquiry.csproj` 的 `TargetFramework` 與 `provision.ps1` 的 `--runtime-version` 一起升到 10。
