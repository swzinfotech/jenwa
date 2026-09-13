<#
.SYNOPSIS
  Publishes Jenwa.Inquiry and zip-deploys it to the function app.

.DESCRIPTION
  Uses `dotnet publish` + `az functionapp deployment source config-zip`, so Azure Functions
  Core Tools is not required. The zip must have host.json at its root, which is why the
  contents of the publish folder are archived rather than the folder itself.

.EXAMPLE
  ./deploy.ps1
  ./deploy.ps1 -FunctionAppName jenwa-inquiry-func2
#>
[CmdletBinding()]
param(
  [string] $ResourceGroup   = 'jenwa-rg',
  [string] $FunctionAppName = 'jenwa-inquiry-func'
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '_common.ps1')

$projectDir = Join-Path (Split-Path -Parent $PSScriptRoot) 'src\Jenwa.Inquiry'
$publishDir = Join-Path $env:TEMP "jenwa-inquiry-publish-$(Get-Date -Format yyyyMMddHHmmss)"
$zipPath    = "$publishDir.zip"

Write-Host "Publishing $projectDir..."
& dotnet publish $projectDir -c Release -o $publishDir --nologo
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed.' }

if (-not (Test-Path (Join-Path $publishDir 'host.json'))) { throw "host.json is missing from $publishDir." }
# local.settings.json holds the LINE credentials for local runs and must never be uploaded.
Remove-Item (Join-Path $publishDir 'local.settings.json') -ErrorAction SilentlyContinue

Write-Host "Packing $zipPath..."
New-DeploymentZip -SourceDir $publishDir -ZipPath $zipPath

Write-Host "Deploying to $FunctionAppName..."
Invoke-Az @('functionapp', 'deployment', 'source', 'config-zip', '-g', $ResourceGroup, '-n', $FunctionAppName, '--src', $zipPath, '-o', 'none') | Out-Null

$hostName = Get-FunctionAppHostName -ResourceGroup $ResourceGroup -Name $FunctionAppName
Remove-Item $publishDir -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item $zipPath -Force -ErrorAction SilentlyContinue

Write-Host ''
Write-Host 'Deployed.' -ForegroundColor Green
Get-AzLines @('functionapp', 'function', 'list', '-g', $ResourceGroup, '-n', $FunctionAppName, '--query', '[].name', '-o', 'tsv') -AllowFailure
Write-Host "Inquiry API  : https://$hostName/api/inquiry"
Write-Host "LINE webhook : https://$hostName/api/line/webhook"
