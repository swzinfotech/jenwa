# 易立構｜參考圖版

依 images/design1.png、images/design1-m.png 的灰白建築視覺、產品卡片、線條圖示及手機圖文順序製作。保留易立構品牌與 sitemap.md 的七頁內容。

## 使用

直接開啟 index.html，或執行 `npm run dev`，瀏覽 http://127.0.0.1:4190。
修改 build.mjs、styles.css、reference.css、app.js，再執行 `npm run build`。
頁面從 build.mjs 產生，請勿只改輸出的 HTML。兩個版本各自獨立；本任務交付本機目錄，未部署。
表單送出的後端網址寫在 build.mjs 的 `inquiryApi`，輸出為每頁的 `<meta name="inquiry-api">`；要指向其他環境時用 `INQUIRY_API=... npm run build`。

## 頁面

index.html 首頁、product.html 產品介紹、cases.html 精選範例、contact.html 申請場勘、about.html 關於我們、faq.html 常見問題、comparison.html 組合屋比較。
品牌大事紀、專利、正式報價、公司電話地址、新聞與 YouTube 來源尚待提供。表單送出後由 `03_AzureFunction` 的 `/api/inquiry` 推播到 LINE 群組；「下載諮詢單」仍只在本機產生檔案，不傳送資料。

## 視覺與素材

- 主色為灰白、墨黑，字體優先使用 Noto Sans TC / PingFang TC / Microsoft JhengHei 系統黑體。
- 所有圖片均存放本機，不依賴外部圖床與字型服務。
- logo.png 來自 images/logo.png。
- house.jpg、interior.jpg、studio.jpg、lounge.jpg 分別來自 images/official/ff290817c3-E1524548250233.jpg、fdbeddc4b0-E1594891390522.jpg、e436f3a609-E1518415328872.JPG、420df6447f-E1518415328895.jpg。
- hero-courtyard.png 使用內建 imagegen，以官方 house.jpg 製作建築情境示意，並在首頁標示。非完工實景或施工圖。
- 主視覺提示詞及來源見 image-prompt.txt。

桌面採左側文案搭配寬幅建築照片，手機改為照片在前、標題與雙按鈕在後；手機導覽為深色展開選單，應用圖示採兩欄，流程採垂直編排。
