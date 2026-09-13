[folder]
# 02_website: front-end web
# 03_AzureFunction: back-end function
# Azure.md: Azure 資源清單（訂閱、資源、設定、費用、待改善）

[LINE bot info]
bot basic ID: @515gjwug
Channel ID: 2011572957
Channel secret: cc3245dbe384e70ce12b9b873f5c8cec
Channnel access token: 
xjEV780rZbedi14pUfHquj+ei0V+XNI19Ih3K5g+Z8qYx4mqmTLhFWJEfA988UKmXi0LjM+murbTbiewzf+YFO1+6qPpiLWVsyQya+SyeFGYc4Xv44BhukzvkbSOrMfZUviljvg9qWvIDG2IVSrZSgdB04t89/1O/w1cDnyilFU=

# 上面兩組憑證已明文 commit 進 git，建議輪替後只放 Azure App Settings；詳見 03_AzureFunction/README.md「待辦」。
# 執行環境的值以 Function App jenwa-inquiry-func 的 App Settings 為準。

[deployed]
Azure: jenwa-rg / japaneast / jenwa-inquiry-func (Flex Consumption, dotnet-isolated 9)
Inquiry API: https://jenwa-inquiry-func.azurewebsites.net/api/inquiry
LINE webhook: https://jenwa-inquiry-func.azurewebsites.net/api/line/webhook
webhook 已啟用（active），推播目標群組 LINE_TO_IDS 已設定，端對端實測可送達。
資源與設定明細見 Azure.md。