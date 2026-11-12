$ErrorActionPreference = 'Stop'
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $csc -PathType Leaf)) {
    throw 'This build requires the 64-bit .NET Framework 4.x C# compiler.'
}
$out = Join-Path $PSScriptRoot 'AI-Creation-Workbench.exe'
$sources = 'Launcher.cs', 'Workbench.cs', 'Settings.cs' | ForEach-Object { Join-Path $PSScriptRoot $_ }
& $csc /nologo /target:winexe /optimize+ /codepage:65001 /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll "/out:$out" @sources
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Write-Output "Built: $out"
