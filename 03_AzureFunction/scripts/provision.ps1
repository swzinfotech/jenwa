<#
.SYNOPSIS
  Creates the Azure resources for the Easy Go inquiry endpoint and writes its app settings.

.DESCRIPTION
  Idempotent: re-running it reuses whatever already exists. Names must be globally unique,
  so the storage account and function app get a suffix when the preferred name is taken.

  The LINE credentials are read from AGENTS.md when not passed in. They belong in App
  Settings only - AGENTS.md is the repo's current source of record, but the values there
  are committed to git and should be rotated once this endpoint is confirmed working.

.EXAMPLE
  ./provision.ps1
  ./provision.ps1 -FunctionAppName jenwa-inquiry-func2 -AllowedOrigins "https://easygo.tw"
#>
[CmdletBinding()]
param(
  [string] $ResourceGroup    = 'jenwa-rg',
  [string] $Location         = 'japaneast',
  [string] $FunctionAppName  = 'jenwa-inquiry-func',
  [string] $StorageAccount   = 'stjenwainquiry',
  [string] $AppInsightsName  = 'jenwa-inquiry-ai',
  [string] $WorkspaceName    = 'jenwa-inquiry-logs',
  # 4190 is `npm run dev` (02_website/serve.mjs); 5500 is the VS Code Live Server extension.
  # Both host names are listed because the browser sends whichever one is in the address bar.
  [string] $AllowedOrigins   = 'http://127.0.0.1:4190,http://localhost:4190,http://127.0.0.1:5500,http://localhost:5500',
  [int]    $MonthlyPushCap   = 150,
  [string] $ChannelAccessToken,
  [string] $ChannelSecret,
  [string] $ToIds            = ''
)

$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot '_common.ps1')

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

# --- LINE credentials -------------------------------------------------------
if (-not $ChannelAccessToken -or -not $ChannelSecret) {
  $agents = Join-Path $repoRoot 'AGENTS.md'
  if (-not (Test-Path $agents)) { throw "Pass -ChannelAccessToken and -ChannelSecret, or keep AGENTS.md at $agents." }
  $text = Get-Content $agents -Raw
  if (-not $ChannelSecret) {
    $match = [regex]::Match($text, '(?im)^\s*Channel\s*secret\s*:\s*(\S+)')
    if (-not $match.Success) { throw 'Could not find the channel secret in AGENTS.md; pass -ChannelSecret.' }
    $ChannelSecret = $match.Groups[1].Value
  }
  if (-not $ChannelAccessToken) {
    # The token is on the line(s) after its label and contains + / = characters.
    $match = [regex]::Match($text, '(?ims)^\s*Chann+el\s*access\s*token\s*:\s*(?<token>[A-Za-z0-9+/=\s]{100,})')
    if (-not $match.Success) { throw 'Could not find the channel access token in AGENTS.md; pass -ChannelAccessToken.' }
    $ChannelAccessToken = ($match.Groups['token'].Value -replace '\s', '')
  }
  Write-Host 'Read the LINE credentials from AGENTS.md.' -ForegroundColor DarkYellow
}

$account = Invoke-Az @('account', 'show', '--query', '{name:name,id:id}', '-o', 'tsv')
Write-Host "Subscription: $account"

# --- Resource providers -----------------------------------------------------
# A subscription that has never hosted these resource types reports "SubscriptionNotFound"
# for every call against them until the provider is registered.
foreach ($namespace in @('Microsoft.Storage', 'Microsoft.Web', 'microsoft.insights', 'Microsoft.OperationalInsights')) {
  $state = (Invoke-Az @('provider', 'show', '-n', $namespace, '--query', 'registrationState', '-o', 'tsv') | Out-String).Trim()
  if ($state -ne 'Registered') {
    Write-Host "Registering resource provider $namespace (state: $state)..."
    Invoke-Az @('provider', 'register', '-n', $namespace, '--wait', '-o', 'none') | Out-Null
  }
}
Write-Host 'Resource providers registered.' -ForegroundColor Green

# --- Resource group ---------------------------------------------------------
Invoke-Az @('group', 'create', '-n', $ResourceGroup, '-l', $Location, '-o', 'none') | Out-Null
Write-Host "Resource group $ResourceGroup ready." -ForegroundColor Green

# --- Storage account (also holds the throttle/inquiry/source tables) --------
$existingStorage = Get-AzLines @('storage', 'account', 'list', '-g', $ResourceGroup, '--query', '[].name', '-o', 'tsv') |
  Where-Object { $_.StartsWith($StorageAccount) }
if ($existingStorage) {
  $StorageAccount = @($existingStorage)[0]
  Write-Host "Reusing storage account $StorageAccount." -ForegroundColor Green
} else {
  $candidate = $StorageAccount
  for ($i = 0; $i -lt 10; $i++) {
    $available = Invoke-Az @('storage', 'account', 'check-name', '-n', $candidate, '--query', 'nameAvailable', '-o', 'tsv')
    if ($available -eq 'true') { break }
    $candidate = "$StorageAccount$(Get-Random -Minimum 100 -Maximum 999)"
  }
  $StorageAccount = $candidate
  Write-Host "Creating storage account $StorageAccount..."
  Invoke-Az @('storage', 'account', 'create', '-n', $StorageAccount, '-g', $ResourceGroup, '-l', $Location,
    '--sku', 'Standard_LRS', '--kind', 'StorageV2', '--min-tls-version', 'TLS1_2',
    '--allow-blob-public-access', 'false', '-o', 'none') | Out-Null
  Write-Host "Storage account $StorageAccount created." -ForegroundColor Green
}

# --- Application Insights ---------------------------------------------------
Invoke-Az @('extension', 'add', '-n', 'application-insights', '--only-show-errors', '-o', 'none') -AllowFailure | Out-Null
$insightsKey = Invoke-Az @('monitor', 'app-insights', 'component', 'show', '-g', $ResourceGroup, '-a', $AppInsightsName,
  '--query', 'connectionString', '-o', 'tsv') -AllowFailure
if ($LASTEXITCODE -ne 0 -or -not $insightsKey) {
  # Classic (workspace-less) components are retired, so a Log Analytics workspace comes first.
  Write-Host "Creating Log Analytics workspace $WorkspaceName..."
  $workspaceId = (Invoke-Az @('monitor', 'log-analytics', 'workspace', 'create', '-g', $ResourceGroup, '-n', $WorkspaceName,
    '-l', $Location, '--query', 'id', '-o', 'tsv') | Out-String).Trim()
  Write-Host "Creating Application Insights $AppInsightsName..."
  $insightsKey = Invoke-Az @('monitor', 'app-insights', 'component', 'create', '-g', $ResourceGroup, '-a', $AppInsightsName,
    '-l', $Location, '--kind', 'web', '--application-type', 'web', '--workspace', $workspaceId,
    '--query', 'connectionString', '-o', 'tsv')
}
$insightsKey = ($insightsKey | Out-String).Trim()

# --- Function app (Flex Consumption, .NET 9 isolated) -----------------------
if (Get-FunctionApp -ResourceGroup $ResourceGroup -Name $FunctionAppName) {
  Write-Host "Reusing function app $FunctionAppName." -ForegroundColor Green
} else {
  # The name is part of the *.azurewebsites.net host name, so it must be globally unique.
  $candidate = $FunctionAppName
  for ($i = 0; $i -lt 10; $i++) {
    $available = (Get-AzLines @('functionapp', 'list', '--query', '[].name', '-o', 'tsv') -AllowFailure) -notcontains $candidate
    if ($available) { break }
    $candidate = "$FunctionAppName$(Get-Random -Minimum 100 -Maximum 999)"
  }
  $FunctionAppName = $candidate
  Write-Host "Creating function app $FunctionAppName (Flex Consumption, dotnet-isolated 9)..."
  Invoke-Az @('functionapp', 'create', '-g', $ResourceGroup, '-n', $FunctionAppName,
    '--storage-account', $StorageAccount, '--flexconsumption-location', $Location,
    '--runtime', 'dotnet-isolated', '--runtime-version', '9', '--instance-memory', '2048',
    '--app-insights', $AppInsightsName, '-o', 'none') | Out-Null
  Write-Host "Function app $FunctionAppName created." -ForegroundColor Green
}

# --- App settings -----------------------------------------------------------
Write-Host 'Writing app settings...'
if (-not $ToIds) {
  # LINE_TO_IDS is obtained by hand from a webhook event, so a re-run must never blank it.
  $existing = (Invoke-Az @('functionapp', 'config', 'appsettings', 'list', '-g', $ResourceGroup,
    '-n', $FunctionAppName, '-o', 'json') -AllowFailure | Out-String)
  if ($existing.Trim()) {
    $ToIds = (($existing | ConvertFrom-Json) | Where-Object { $_.name -eq 'LINE_TO_IDS' }).value
  }
  if ($ToIds) { Write-Host "Keeping the existing LINE_TO_IDS ($($ToIds.Length) chars)." }
}
$settings = @(
  "LINE_CHANNEL_ACCESS_TOKEN=$ChannelAccessToken",
  "LINE_CHANNEL_SECRET=$ChannelSecret",
  "LINE_TO_IDS=$ToIds",
  "ALLOWED_ORIGINS=$AllowedOrigins",
  "MONTHLY_PUSH_CAP=$MonthlyPushCap"
)
if ($insightsKey) { $settings += "APPLICATIONINSIGHTS_CONNECTION_STRING=$insightsKey" }
Invoke-Az (@('functionapp', 'config', 'appsettings', 'set', '-g', $ResourceGroup, '-n', $FunctionAppName, '-o', 'none', '--settings') + $settings) | Out-Null

# --- Platform CORS ----------------------------------------------------------
# The Functions host answers the preflight OPTIONS itself and never invokes the function, so the
# browser-facing allow list has to live here. It is driven from the same -AllowedOrigins value as
# the ALLOWED_ORIGINS app setting so the two cannot drift apart.
Write-Host 'Syncing platform CORS...'
$wanted = @($AllowedOrigins -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_ })
$current = @(Get-AzLines @('functionapp', 'cors', 'show', '-g', $ResourceGroup, '-n', $FunctionAppName,
  '--query', 'allowedOrigins', '-o', 'tsv') -AllowFailure)
foreach ($origin in $current) {
  if ($wanted -notcontains $origin) {
    Invoke-Az @('functionapp', 'cors', 'remove', '-g', $ResourceGroup, '-n', $FunctionAppName, '--allowed-origins', $origin, '-o', 'none') | Out-Null
  }
}
foreach ($origin in $wanted) {
  if ($current -notcontains $origin) {
    Invoke-Az @('functionapp', 'cors', 'add', '-g', $ResourceGroup, '-n', $FunctionAppName, '--allowed-origins', $origin, '-o', 'none') | Out-Null
  }
}
Write-Host "Platform CORS: $($wanted -join ', ')" -ForegroundColor Green

$hostName = Get-FunctionAppHostName -ResourceGroup $ResourceGroup -Name $FunctionAppName

Write-Host ''
Write-Host '=== Provisioned ===' -ForegroundColor Cyan
Write-Host "Resource group : $ResourceGroup"
Write-Host "Storage        : $StorageAccount"
Write-Host "Function app   : $FunctionAppName"
Write-Host "Inquiry API    : https://$hostName/api/inquiry"
Write-Host "LINE webhook   : https://$hostName/api/line/webhook"
if (-not $ToIds) {
  Write-Host ''
  Write-Host 'LINE_TO_IDS is still empty, so nothing can be pushed yet.' -ForegroundColor Yellow
  Write-Host 'Deploy, set the webhook, invite the bot to the staff group, then set it:' -ForegroundColor Yellow
  Write-Host "  az functionapp config appsettings set -g $ResourceGroup -n $FunctionAppName --settings LINE_TO_IDS=<groupId>" -ForegroundColor Yellow
}
