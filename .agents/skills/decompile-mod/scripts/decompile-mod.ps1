#Requires -Version 5.1
<#
.SYNOPSIS
  固定 ilspycmd 包装。agent 只准原样调用，不准改参数、不改本脚本。
.DESCRIPTION
  仅解包第三方 mod DLL；输出与日志只写 %TEMP%\ta-decompile\。内置 120s 单步超时。
.PARAMETER Dll
  用户给出的 DLL 绝对路径。
.PARAMETER OutName
  输出短名（字母/数字/./_/-），决定 %TEMP%\ta-decompile\<OutName>\。
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$Dll,
    [Parameter(Mandatory = $true)][string]$OutName
)

$ErrorActionPreference = 'Stop'
$TimeoutSeconds = 120

function Fail([int]$code, [string]$message) {
    Write-Output "FAIL exit=$code $message"
    exit $code
}

if ($OutName -notmatch '^[A-Za-z0-9._-]+$') { Fail 2 'OutName 只允许字母/数字/./_/-' }
if (-not (Test-Path -LiteralPath $Dll -PathType Leaf)) { Fail 3 "DLL 不存在：$Dll" }

$dllPath = (Resolve-Path -LiteralPath $Dll).Path
if ((Split-Path -Leaf $dllPath) -ieq 'sts2.dll' -or $dllPath -match '\\data_sts2[^\\]*\\') {
    Fail 4 '拒绝解包游戏本体程序集；本体只查 .agents/local.md 的 sts2-src'
}

$tool = (Get-Command ilspycmd -ErrorAction SilentlyContinue).Source
if (-not $tool) {
    $candidate = Join-Path $env:USERPROFILE '.dotnet\tools\ilspycmd.exe'
    if (Test-Path -LiteralPath $candidate) { $tool = $candidate }
}
if (-not $tool) { Fail 5 '找不到 ilspycmd；不要自行安装或换工具，直接报告用户' }

$tempRoot = $env:TEMP
if ([string]::IsNullOrWhiteSpace($tempRoot)) { $tempRoot = [System.IO.Path]::GetTempPath() }
$outRoot = Join-Path (Join-Path $tempRoot 'ta-decompile') $OutName
$srcDir = Join-Path $outRoot 'src'
$log = Join-Path $outRoot 'ilspycmd.log'

# 已存在的输出目录不碰：删除/覆盖不在本脚本权限内
if (Test-Path -LiteralPath $outRoot) {
    if (Get-ChildItem -LiteralPath $outRoot -Force | Select-Object -First 1) {
        Fail 6 "输出目录已存在且非空：$outRoot（不要删除/覆盖，报告用户决定）"
    }
} else {
    New-Item -ItemType Directory -Path $outRoot -Force | Out-Null
}
New-Item -ItemType Directory -Path $srcDir -Force | Out-Null

# -p：一型一文件、按命名空间分目录，比单文件 .decompiled.cs 好检索
$argList = @('-p', "`"$dllPath`"", '-o', "`"$srcDir`"", '--disable-updatecheck')

# 用 ProcessStartInfo 直接起进程：Start-Process -PassThru 拿不到 ExitCode
$psi = New-Object System.Diagnostics.ProcessStartInfo
$psi.FileName = $tool
$psi.Arguments = ($argList -join ' ')
$psi.UseShellExecute = $false
$psi.CreateNoWindow = $true
$psi.RedirectStandardOutput = $true
$psi.RedirectStandardError = $true
try { $proc = [System.Diagnostics.Process]::Start($psi) } catch { Fail 5 "无法启动 ilspycmd：$($_.Exception.Message)" }
$outTask = $proc.StandardOutput.ReadToEndAsync()
$errTask = $proc.StandardError.ReadToEndAsync()

if (-not $proc.WaitForExit($TimeoutSeconds * 1000)) {
    try { $proc.Kill() } catch { }
    try { [void]$proc.WaitForExit(5000) } catch { }
    # 超时也要留下已有输出，否则报告里没有日志可看
    $partial = ''
    if ($outTask.Wait(2000)) { $partial += $outTask.Result }
    if ($errTask.Wait(2000)) { $partial += $errTask.Result }
    [System.IO.File]::WriteAllText($log, $partial, (New-Object System.Text.UTF8Encoding($false)))
    Write-Output "TIMEOUT after ${TimeoutSeconds}s pid=$($proc.Id)（已强制结束，不要重跑同一步）"
    Write-Output '---- ilspycmd.log (tail 50) ----'
    Get-Content -LiteralPath $log -Tail 50 -ErrorAction SilentlyContinue
    exit 124
}

$proc.WaitForExit()
$out = $outTask.Result
$err = $errTask.Result
[System.IO.File]::WriteAllText($log, ($out + $err), (New-Object System.Text.UTF8Encoding($false)))
$code = $proc.ExitCode

$count = (Get-ChildItem -LiteralPath $srcDir -Filter *.cs -Recurse -File | Measure-Object).Count
Write-Output "exit=$code files=$count outdir=$srcDir"
Write-Output '---- ilspycmd.log (tail 50) ----'
if (Test-Path -LiteralPath $log) { Get-Content -LiteralPath $log -Tail 50 -ErrorAction SilentlyContinue }

if ($code -ne 0) { Write-Output "FAIL exit=$code ilspycmd 非零退出"; exit $code }
if ($count -eq 0) { Write-Output 'FAIL exit=7 没有产出 .cs 文件'; exit 7 }
Write-Output 'OK'
