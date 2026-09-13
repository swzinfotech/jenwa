# 易立構｜純文字需求版

本版先依客戶文字需求完成，未參考 design1.png 或 design1-m.png。採建築顧問型編排：霧白留白、深墨色、低飽和產品照片、黑體層級、細線分隔。無花草裝飾背景。

## 使用

直接開啟 index.html，或執行 `npm run dev`，瀏覽 http://127.0.0.1:4191。
修改 build.mjs（內容）、styles.css（樣式）、app.js（互動），再執行 `npm run build`。
七頁 HTML 與 dist/client 同步產生，dist/server/index.js 為 Worker ESM 入口。此任務交付本機目錄，未部署。

## Sitemap

- 首頁：Banner、精選範例、比較表、產品優勢、購買流程、聯絡表單。
- product.html：結構分析、生產流程、優勢、差異、模型與預算參考。
- cases.html：用途篩選及三個空間提案。
- contact.html：場勘準備與諮詢單下載。
- about.html：品牌理念、大事紀、專利與聯絡資訊區。
- faq.html：分組問答與索引。
- comparison.html：部位比較、新聞與影片資料區。

尚未提供的品牌紀錄、專利、公司聯絡方式、正式報價、新聞影片，均標示待確認，未虛構。表單只下載本機諮詢單，不送出、不代表完成預約。

## 圖片來源

assets/logo.png：images/logo.png。
house.jpg：images/official/ff290817c3-E1524548250233.jpg。
interior.jpg：images/official/fdbeddc4b0-E1594891390522.jpg。
studio.jpg：images/official/e436f3a609-E1518415328872.JPG。
lounge.jpg：images/official/420df6447f-E1518415328895.jpg。
照片使用 CSS 降低飽和度；結構圖為程式繪製的概念 SVG，非施工圖。
