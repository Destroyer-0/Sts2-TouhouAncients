# Relic Localization Validation Scripts
# Run from: Sts2-TouhouAncients workspace root
# Usage: Copy the relevant section into your PowerShell terminal

$zhsPath = "TouhouAncients\localization\zhs\relics.json"
$engPath = "TouhouAncients\localization\eng\relics.json"

# ============================================================
# 1. JSON SYNTAX VALIDATION
# ============================================================
# Validates that both files are valid JSON
Write-Host "=== JSON Syntax Validation ==="
try { $null = Get-Content $zhsPath -Raw | ConvertFrom-Json; Write-Host "[PASS] zhs/relics.json - valid JSON" } catch { Write-Host "[FAIL] zhs/relics.json - $($_.Exception.Message)" }
try { $null = Get-Content $engPath -Raw | ConvertFrom-Json; Write-Host "[PASS] eng/relics.json - valid JSON" } catch { Write-Host "[FAIL] eng/relics.json - $($_.Exception.Message)" }

# ============================================================
# 2. DUPLICATE KEY DETECTION
# ============================================================
# Finds any duplicate JSON keys within each file
Write-Host "`n=== Duplicate Key Detection ==="
foreach ($file in @($zhsPath, $engPath)) {
    $lines = Get-Content $file
    $keyLines = $lines | Select-String '"TOUHOUANCIENTS-[^"]+"\s*:' | ForEach-Object { $_.Line.Trim() }
    $dupCheck = @{}
    $dupes = @()
    foreach ($l in $keyLines) {
        if ($l -match '"([^"]+)"') {
            $k = $matches[1]
            if ($dupCheck[$k]) { $dupes += $k }
            else { $dupCheck[$k] = $true }
        }
    }
    if ($dupes) {
        Write-Host "[WARN] $file has duplicate keys:"
        $dupes | ForEach-Object { Write-Host "  $_" }
    } else { Write-Host "[PASS] $file - no duplicate keys" }
}

# ============================================================
# 3. MISSING KEY DETECTION (zhs vs eng)
# ============================================================
# Finds relic entries present in zhs but missing from eng (or vice versa)
Write-Host "`n=== Missing Key Detection ==="
$zhs = Get-Content $zhsPath -Raw | ConvertFrom-Json
$eng = Get-Content $engPath -Raw | ConvertFrom-Json

# Extract base relic keys by stripping known suffixes
$suffixPatterns = @(
    '\.title$', '\.description$', '\.eventDescription$', '\.flavor$',
    '\.selectionScreenPrompt$', '\.filterKeywords$',
    '\.additionalRestSiteHealText$', '\.forgetTitle$', '\.forgetNothing$', '\.forget$',
    '\.hungry$', '\.mushroom$', '\.extra$', '\.infoText$'
)
function Get-BaseKeys($obj) {
    $obj.PSObject.Properties.Name | ForEach-Object {
        $n = $_
        foreach ($s in $suffixPatterns) { $n = $n -replace $s, '' }
        $n
    } | Sort-Object -Unique
}
$zhsBase = Get-BaseKeys $zhs
$engBase = Get-BaseKeys $eng
Write-Host "zhs base keys: $($zhsBase.Count)"
Write-Host "eng base keys: $($engBase.Count)"

$onlyZhs = $zhsBase | Where-Object { $_ -notin $engBase }
$onlyEng = $engBase | Where-Object { $_ -notin $zhsBase }
if ($onlyZhs) { Write-Host "[MISSING] In eng:"; $onlyZhs | ForEach-Object { Write-Host "  $_" } }
if ($onlyEng) { Write-Host "[ORPHAN] In eng but not zhs:"; $onlyEng | ForEach-Object { Write-Host "  $_" } }
if (-not $onlyZhs -and -not $onlyEng) { Write-Host "[PASS] All keys present in both files" }

# ============================================================
# 4. REQUIRED SUB-KEYS CHECK
# ============================================================
# Every relic should have at minimum .title and .description
Write-Host "`n=== Required Sub-Keys Check ==="
foreach ($baseKey in $zhsBase) {
    $subs = $zhs.PSObject.Properties.Name | Where-Object { $_ -like "$baseKey.*" }
    if (-not ($subs -match '\.title$')) { Write-Host "[WARN] $baseKey missing .title" }
    if (-not ($subs -match '\.description$')) { Write-Host "[WARN] $baseKey missing .description" }
}
Write-Host "[DONE] Sub-key check complete"

# ============================================================
# 5. DYNAMIC VARIABLE CONSISTENCY (zhs variables in eng)
# ============================================================
# Ensures all {VarName} variables from zhs .description appear in eng .description
Write-Host "`n=== Variable Consistency ==="
$mismatches = 0
$checked = 0
$zhs.PSObject.Properties | Where-Object Name -match '\.description$' | ForEach-Object {
    $k = $_.Name
    $checked++
    if ($eng.$k) {
        # Extract simple single-line variables (ignores nested braces like cond:...)
        $zv = [regex]::Matches($_.Value, '\{([A-Za-z][A-Za-z0-9_.]*)(?::|$|\})') | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique
        $ev = [regex]::Matches($eng.$k, '\{([A-Za-z][A-Za-z0-9_.]*)(?::|$|\})') | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique
        $missing = $zv | Where-Object { $_ -notin $ev }
        if ($missing) {
            $mismatches++
            Write-Host "  $k : zhs vars missing from eng -> $($missing -join ', ')"
        }
    }
}
Write-Host "Checked $checked descriptions, $mismatches with variable mismatches"
if ($mismatches -eq 0) { Write-Host "[PASS] All variables consistent" }
