# Azure 資源清單

易立構場勘諮詢表單的後端（`03_AzureFunction`）在 Azure 上用到的所有服務。內容以 `az` 實際查詢結果為準，非規劃值。

最後核對：2026-09-13

- **訂閱**：`Subscription-projects`（`2ca86765-55ce-453e-8657-98bdc5005b0f`）
- **租用戶**：`9e4c5a09-7a25-4ba5-b14f-80299321eec4`（`swzinfotechgmail.onmicrosoft.com`）
- **登入帳號**：`swzinfotech@gmail.com`
- **資源群組**：`jenwa-rg`（Japan East）—— 本專案所有資源都在這裡
- **區域**：Japan East（唯一例外是 Action Group，Azure 強制為 `global`）

> 訂閱裡另有一個 `ai-agent-cli` 資源群組（Japan East），目前**沒有任何資源**，與本專案無關。

## 資源總覽

| 資源類型 | 名稱 | 說明 |
| --- | --- | --- |
| `Microsoft.Web/sites` | `jenwa-inquiry-func` | Function App，表單 API 與 LINE webhook |
| `Microsoft.Web/serverFarms` | `ASP-jenwarg-a873` | 主機方案（Flex Consumption FC1），建立 Function App 時自動產生 |
| `Microsoft.Storage/storageAccounts` | `stjenwainquiry` | Functions 執行儲存體 + 部署套件 + 三張資料表 |
| `Microsoft.Insights/components` | `jenwa-inquiry-ai` | Application Insights |
| `Microsoft.OperationalInsights/workspaces` | `jenwa-inquiry-logs` | App Insights 的 Log Analytics 工作區 |
| `microsoft.insights/actiongroups` | `Application Insights Smart Detection` | App Insights 自動建立的智慧偵測通知群組，我們沒有主動建立 |

---

## Function App：`jenwa-inquiry-func`

| 項目 | 值 |
| --- | --- |
| 主機名稱 | `jenwa-inquiry-func.azurewebsites.net` |
| 狀態 / 種類 | Running / `functionapp,linux` |
| 主機方案 | `ASP-jenwarg-a873`（FlexConsumption / FC1 / capacity 0 / Linux） |
| 執行環境 | `dotnet-isolated` **9.0** |
| 每例項記憶體 | 2048 MB |
| 最大例項數 | 100 |
| 永備例項（alwaysReady） | 無 —— 閒置時不佔用運算資源 |
| 部署方式 | Blob 容器 `app-package-jenwainquiryfunc-7298534`，憑 `DEPLOYMENT_STORAGE_CONNECTION_STRING` 存取 |
| 更新策略 | `Recreate` |
| `httpsOnly` | **False**（見文末待改善） |
| 用戶端憑證 | `clientCertEnabled: False`（`clientCertMode: Required` 未啟用，無作用） |
| HTTP/2 | 未啟用 |

### 函式與路由

| 函式 | 方法 | 路由 | 授權 |
| --- | --- | --- | --- |
| `Inquiry` | `POST` | `/api/inquiry` | Anonymous |
| `LineWebhook` | `POST` | `/api/line/webhook` | Anonymous |
| `LineSources` | `GET` | `/api/line/sources` | Function（需 function key） |

`Inquiry` **只註冊 `POST`**：Functions host 會自己回應 CORS preflight 的 `OPTIONS`，完全不會呼叫函式，所以瀏覽器用的標頭必須來自平台 CORS 設定（見下）。

### 對外連出 IP

若日後 LINE 或其他服務需要 IP 白名單，Function App 的連出位址為：

```
48.218.216.140, 48.218.216.143, 48.218.216.145, 48.218.216.146,
48.218.216.148, 48.218.216.149, 48.218.216.106, 48.218.216.108,
48.218.216.112, 48.218.216.115, 48.218.216.120, 48.218.216.121,
20.210.64.21, 20.89.14.5
```

這些位址會隨平台調整，不是保證固定值。

### 平台 CORS

```
http://127.0.0.1:4190   （02_website 的 npm run dev）
http://localhost:4190
http://127.0.0.1:5500   （VS Code Live Server）
http://localhost:5500
supportCredentials: false
```

**必須與 `ALLOWED_ORIGINS` app setting 保持一致**，並且只能透過 `provision.ps1 -AllowedOrigins` 修改 —— 該腳本會同步兩邊。只改 app setting 的話，preflight 仍會被平台擋掉，瀏覽器連 POST 都不會送出。

### App Settings

只列名稱，值請在 Azure Portal 或 `az functionapp config appsettings list` 查看。

| 名稱 | 用途 | 來源 |
| --- | --- | --- |
| `LINE_CHANNEL_ACCESS_TOKEN` | LINE Messaging API 推播與回覆 | `AGENTS.md`（待輪替） |
| `LINE_CHANNEL_SECRET` | 驗證 `X-Line-Signature` | `AGENTS.md`（待輪替） |
| `LINE_TO_IDS` | 推播目標，逗號分隔。空值＝不推播 | webhook 事件擷取的 groupId |
| `ALLOWED_ORIGINS` | 伺服器端 `Origin` 白名單 | `provision.ps1 -AllowedOrigins` |
| `MONTHLY_PUSH_CAP` | 每月推播上限（目前 150） | `provision.ps1 -MonthlyPushCap` |
| `AzureWebJobsStorage` | Functions 執行儲存體，同時供三張 Table 使用 | 建立 Function App 時自動設定 |
| `DEPLOYMENT_STORAGE_CONNECTION_STRING` | Flex Consumption 的部署套件容器 | 平台自動設定 |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | 遙測輸出 | `provision.ps1` |

---

## 儲存體帳戶：`stjenwainquiry`

| 項目 | 值 |
| --- | --- |
| SKU / 種類 | `Standard_LRS` / `StorageV2`（Hot） |
| 最低 TLS | TLS 1.2 |
| 僅允許 HTTPS | True |
| Blob 公開存取 | 停用 |

### 資料表

| 資料表 | 內容 |
| --- | --- |
| `inquiries` | 每一筆收到的諮詢（含 `Pushed` 欄位記錄是否送達 LINE）。推播失敗或額度用盡時的資料來源 |
| `throttle` | 節流狀態。`ip` / `phone` 分割區的 RowKey 是 SHA-256 雜湊，**不存原始 IP 與電話**；`quota` 分割區存每月推播計數 |
| `linesources` | webhook 看到的 LINE 來源（group / room / user ID），用來取得 `LINE_TO_IDS` |

### Blob 容器

三個都是平台建立的，不要手動改動：

| 容器 | 用途 |
| --- | --- |
| `app-package-jenwainquiryfunc-7298534` | Flex Consumption 的部署套件 |
| `azure-webjobs-hosts` | Functions 執行階段內部狀態 |
| `azure-webjobs-secrets` | function key 等金鑰 |

---

## 監控

### Application Insights：`jenwa-inquiry-ai`

| 項目 | 值 |
| --- | --- |
| App ID | `8e09d09f-0a39-4807-bf55-26f1befbf093` |
| 類型 | web（workspace-based，`ingestionMode: LogAnalytics`） |
| 工作區 | `jenwa-inquiry-logs` |
| 保留 | 90 天 |
| 公開擷取／查詢 | 皆啟用 |

採用 workspace-based 是必要的：無工作區的傳統 App Insights 元件已停止提供。

### Log Analytics：`jenwa-inquiry-logs`

| 項目 | 值 |
| --- | --- |
| SKU | `PerGB2018`（按擷取量計費） |
| 保留 | 30 天 |
| 每日擷取上限 | 無（`-1`） |

查詢記錄：

```powershell
az monitor app-insights query --app jenwa-inquiry-ai -g jenwa-rg -o table `
  --analytics-query "traces | where timestamp > ago(1h) | project timestamp, message | order by timestamp desc | take 30"

az monitor app-insights query --app jenwa-inquiry-ai -g jenwa-rg -o table `
  --analytics-query "requests | where timestamp > ago(1h) | project timestamp, name, resultCode | order by timestamp desc"
```

---

## 必須註冊的資源提供者

這個訂閱從未部署過這些資源類型，未註冊時每一個相關呼叫都會回 `SubscriptionNotFound`（而不是有意義的錯誤）。`provision.ps1` 會自動確認並註冊：

| 提供者 | 狀態 |
| --- | --- |
| `Microsoft.Web` | Registered |
| `Microsoft.Storage` | Registered |
| `microsoft.insights` | Registered |
| `Microsoft.OperationalInsights` | Registered |

---

## 費用模型

沒有任何固定月費的資源：

- **Function App（Flex Consumption FC1）**：按執行次數與記憶體用量計費；`alwaysReady` 為 0，閒置時不產生運算費用。
- **儲存體**：按容量與交易數計費。三張 Table 的資料量極小（節流狀態與諮詢紀錄）。
- **Log Analytics（PerGB2018）**：按擷取 GB 數計費，保留 30 天內含。目前未設每日上限 —— 若日後流量成長，可用 `az monitor log-analytics workspace update --workspace-capping` 設定上限以免超支。
- **Application Insights**：資料實際計入上述工作區。

## 本專案沒有使用的服務

避免誤會，明確記錄：無 Key Vault、無虛擬網路／私人端點、無自訂網域或 App Service 憑證、無 API Management、無 CDN／Front Door、無受控識別（憑證以連線字串與 app setting 形式存放）。網站前端本身也不在 Azure 上（`02_website/dist/server/index.js` 是 Cloudflare Workers 的靜態資產 shim）。

---

## 操作腳本

全部在 `03_AzureFunction/scripts/`，皆可重複執行：

| 腳本 | 作用 |
| --- | --- |
| `provision.ps1` | 註冊提供者、建立／沿用資源、寫入 app settings、同步平台 CORS |
| `deploy.ps1` | `dotnet publish` → zip → `az functionapp deployment source config-zip` |
| `set-line-webhook.ps1` | 設定並驗證 LINE webhook 端點 |
| `_common.ps1` | 共用工具，並集中處理 Windows 上的 az／PowerShell 陷阱 |

細節與 LINE 端設定見 [03_AzureFunction/README.md](03_AzureFunction/README.md)。

---

## 待改善

1. **Function App 尚未強制 HTTPS。** `httpsOnly` 為 `False`，實測 `http://jenwa-inquiry-func.azurewebsites.net/api/inquiry` 確實會被服務（回應 400，不是轉址）。表單會送出姓名與電話，這些個資不應有機會走明文傳輸。修正：

   ```powershell
   az functionapp update -g jenwa-rg -n jenwa-inquiry-func --set httpsOnly=true
   ```

   網站一律以 `https://` 呼叫端點，開啟後不影響現有行為。

2. **LINE 憑證仍明文存在 `AGENTS.md` 並已 commit 進 git。** 確認端點穩定後請到 LINE Developers Console 重新產生，再用
   `provision.ps1 -ChannelAccessToken ... -ChannelSecret ...` 寫入 App Settings。長期可改用 Key Vault 參考。

3. **.NET 9 的支援到 2026-11-10。** 屆時需同步升級 `Jenwa.Inquiry.csproj` 的 `TargetFramework` 與 `provision.ps1` 的 `--runtime-version`（Japan East 的 Flex Consumption 已支援 `dotnet-isolated` 10）。

4. **正式網域尚未加入白名單。** 網站上線時務必用 `provision.ps1 -AllowedOrigins "https://<網域>,<開發埠…>"`，否則線上站台會在 CORS preflight 就失敗。

5. **Log Analytics 未設每日擷取上限。** 目前流量極低不成問題，值得在正式上線前設一個上限作為費用保險。
