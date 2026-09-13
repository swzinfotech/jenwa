# Shared helpers for the provisioning and deployment scripts.
#
# Two Windows-specific traps are handled here:
#   * Windows PowerShell turns a native command's redirected stderr into an ErrorRecord, which
#     makes az's harmless warnings fatal under ErrorActionPreference Stop - so stderr is never
#     redirected, and success is judged by $LASTEXITCODE alone.
#   * az.cmd runs through cmd.exe, which mangles a --query containing ? { } - so every filter
#     is applied in PowerShell over a flat list instead.

function Invoke-Az {
  param([Parameter(Mandatory)][string[]] $Arguments, [switch] $AllowFailure)
  # az writes progress notes to stderr. Windows PowerShell turns those into ErrorRecords as soon
  # as the stream is captured or piped, so failure is judged by the exit code alone.
  $previous = $ErrorActionPreference
  $ErrorActionPreference = 'Continue'
  try { $output = & az @Arguments } finally { $ErrorActionPreference = $previous }
  if ($LASTEXITCODE -ne 0 -and -not $AllowFailure) {
    throw "az $($Arguments -join ' ') failed:`n$output"
  }
  return $output
}

function Get-AzLines {
  param([Parameter(Mandatory)][string[]] $Arguments, [switch] $AllowFailure)
  $raw = Invoke-Az -Arguments $Arguments -AllowFailure:$AllowFailure
  return ($raw | Out-String) -split "`r?`n" | Where-Object { $_.Trim() } | ForEach-Object { $_.Trim() }
}

# `az functionapp show` returns empty fields for Flex Consumption apps, so the app is always
# looked up through the list endpoint.
function Get-FunctionApp {
  param([Parameter(Mandatory)][string] $ResourceGroup, [Parameter(Mandatory)][string] $Name)
  $rows = Get-AzLines @('functionapp', 'list', '-g', $ResourceGroup, '--query', '[].[name,defaultHostName,state]', '-o', 'tsv') -AllowFailure
  foreach ($row in $rows) {
    $parts = $row -split "`t"
    if ($parts[0] -eq $Name) {
      return [pscustomobject]@{ Name = $parts[0]; HostName = $parts[1]; State = $parts[2] }
    }
  }
  return $null
}

# Builds the deployment package by hand, because neither built-in option produces a zip the
# Linux-side platform can read:
#   * Compress-Archive -Path "dir\*" drops the worker's .azurefunctions directory entirely;
#   * ZipFile.CreateFromDirectory on .NET Framework writes entry names with backslashes, so the
#     directory is invisible to the platform and the package is rejected as invalid.
function New-DeploymentZip {
  param([Parameter(Mandatory)][string] $SourceDir, [Parameter(Mandatory)][string] $ZipPath)
  Add-Type -AssemblyName System.IO.Compression | Out-Null
  Add-Type -AssemblyName System.IO.Compression.FileSystem | Out-Null
  if (Test-Path $ZipPath) { Remove-Item $ZipPath -Force }

  $zip = [System.IO.Compression.ZipFile]::Open($ZipPath, [System.IO.Compression.ZipArchiveMode]::Create)
  # Entry names come from Resolve-Path -Relative rather than trimming a prefix: $env:TEMP is an
  # 8.3 short path while Get-ChildItem reports long paths, so prefix arithmetic cuts the names.
  Push-Location -LiteralPath $SourceDir
  try {
    foreach ($file in Get-ChildItem -Recurse -File -Force) {
      $entryName = (Resolve-Path -LiteralPath $file.FullName -Relative) -replace '^\.[\\/]', '' -replace '\\', '/'
      [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
        $zip, $file.FullName, $entryName, [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
    }
  } finally {
    Pop-Location
    $zip.Dispose()
  }
}

function Get-FunctionAppHostName {
  param([Parameter(Mandatory)][string] $ResourceGroup, [Parameter(Mandatory)][string] $Name)
  $app = Get-FunctionApp -ResourceGroup $ResourceGroup -Name $Name
  if (-not $app) { throw "Function app $Name was not found in $ResourceGroup." }
  return $app.HostName
}
