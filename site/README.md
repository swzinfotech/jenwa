# 易立構網站

依 `../design.md`、`../sitemap.md` 製作的繁體中文 HTML 網站。視覺參考 [無印良品の家](https://www.muji.net/ie/) 的留白、住宅攝影與簡潔編排，使用易立構自己的 Logo、配色與文案。

## 開啟

直接開啟 `index.html` 即可瀏覽，也可在本目錄執行：

```sh
npm run dev
```

預覽網址：`http://127.0.0.1:4173`。不需要安裝依賴。

## 修改與建置

- `build.mjs`：七個頁面的內容與共用 HTML 樣板。
- `styles.css`：桌面與手機版樣式。
- `app.js`：手機選單、案例篩選、諮詢單下載。
- `assets/`：Logo 與本機照片。

修改樣板後執行 `npm run build`，重新產生本目錄 HTML 與 `dist/client/` 靜態發佈檔案，並檢查站內連結、錨點與資產。請勿只修改產生的 HTML，否則下次建置會覆寫。

`dist/server/index.js` 提供使用 `ASSETS` 綁定的 Cloudflare Worker ESM 入口；目前依需求將網站保留在 `site/`，尚未設定線上託管。

## 上線前需補齊

目前為設計提案，住宅照片與案例均標示為情境示意。品牌歷史、專利文件、技術規格、實際案例、正式報價、公司聯絡資訊尚待提供，未虛構證號、性能或實績。

首頁與場勘頁表單只產生本機 `.txt` 諮詢單，不傳送資料，也不代表完成預約。正式收件需另行串接後端、設定收件流程及適用的個資告知內容。

## 圖片來源

- `assets/logo.png`：使用者提供的 `../images/logo.png`。
- `assets/house.jpg`：[Unsplash 住宅照片](https://images.unsplash.com/photo-1600596542815-ffad4c1539a9)。
- `assets/interior.jpg`：[Unsplash 室內照片](https://images.unsplash.com/photo-1600210492486-724fe5c67fb0)。

照片已存放本機，瀏覽網站不依賴外部圖片服務。
