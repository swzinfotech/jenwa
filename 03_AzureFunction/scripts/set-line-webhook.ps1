<#
.SYNOPSIS
  Points the LINE channel's webhook at the deployed function and verifies it.

.DESCRIPTION
  Run this after deploy.ps1. It sets the endpoint, asks LINE to call it, and prints the result.
  Three things it cannot do, because LINE exposes no API for them:
    * turn ON "Use webhook" (Developers Console, Messaging API tab) - without it the endpoint
      is registered but inactive, and no event is ever delivered
    * allow the bot to join groups and multi-person chats (Official Account Manager, response
      settings) - otherwise it cannot be invited to the staff group
    * turn the auto-reply message OFF - otherwise auto-replies compete with the webhook

.EXAMPLE
  ./set-line-webhook.ps1
  ./set-line-webhook.ps1 -Endpoint https://jenwa-inquiry-func.azurewebsites.net/api/line/webhook
#>
[CmdletBinding()]
param(
  [string] $ResourceGroup   = 'jenwa-rg',
  [string] $FunctionAppName = 'jenwa-inquiry-func',
  [string] $Endpoint,
  [string] $ChannelAccessToken
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '_common.ps1')
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

if (-not $Endpoint) {
  $Endpoint = "https://$(Get-FunctionAppHostName -ResourceGroup $ResourceGroup -Name $FunctionAppName)/api/line/webhook"
}

if (-not $ChannelAccessToken) {
  $appSettings = (Invoke-Az @('functionapp', 'config', 'appsettings', 'list', '-g', $ResourceGroup, '-n', $FunctionAppName, '-o', 'json') | Out-String) | ConvertFrom-Json
  $ChannelAccessToken = ($appSettings | Where-Object { $_.name -eq 'LINE_CHANNEL_ACCESS_TOKEN' }).value
  if (-not $ChannelAccessToken) { throw 'LINE_CHANNEL_ACCESS_TOKEN is not set on the function app; pass -ChannelAccessToken.' }
}

$headers = @{ Authorization = "Bearer $ChannelAccessToken" }

Write-Host "Setting the webhook endpoint to $Endpoint..."
Invoke-RestMethod -Method Put -Uri 'https://api.line.me/v2/bot/channel/webhook/endpoint' `
  -Headers $headers -ContentType 'application/json' `
  -Body (@{ endpoint = $Endpoint } | ConvertTo-Json -Compress) | Out-Null
Write-Host 'Endpoint set.' -ForegroundColor Green

$current = Invoke-RestMethod -Method Get -Uri 'https://api.line.me/v2/bot/channel/webhook/endpoint' -Headers $headers
Write-Host "Registered: $($current.endpoint) (active: $($current.active))"
if (-not $current.active) {
  # There is no API for this flag; it is the "Use webhook" toggle in the console.
  Write-Warning 'The webhook is registered but NOT active: LINE will not deliver events yet.'
  Write-Warning 'Turn on "Use webhook" in the LINE Developers Console (Messaging API tab) to activate it.'
}

Write-Host 'Asking LINE to call the endpoint...'
try {
  $test = Invoke-RestMethod -Method Post -Uri 'https://api.line.me/v2/bot/channel/webhook/test' `
    -Headers $headers -ContentType 'application/json' -Body (@{ endpoint = $Endpoint } | ConvertTo-Json -Compress)
  Write-Host "Result: $($test.success) / HTTP $($test.statusCode) / $($test.detail)" -ForegroundColor Green
} catch {
  Write-Warning "The verification call failed: $($_.Exception.Message)"
  Write-Warning 'Check that the deployment finished and that LINE_CHANNEL_SECRET matches the channel.'
}

Write-Host ''
Write-Host 'Next, to get the push target:' -ForegroundColor Cyan
Write-Host '  1. LINE Developers Console, Messaging API tab: turn ON "Use webhook".'
Write-Host '  2. LINE Official Account Manager, response settings: allow joining groups and chats,'
Write-Host '     and turn the auto-reply message OFF.'
Write-Host '  3. Create the staff LINE group and invite @515gjwug.'
Write-Host '  4. The bot answers the join with the group id (or send "id" in the group to ask again).'
Write-Host "  5. az functionapp config appsettings set -g $ResourceGroup -n $FunctionAppName --settings LINE_TO_IDS=<groupId>"
