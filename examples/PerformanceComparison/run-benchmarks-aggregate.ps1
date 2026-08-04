$ErrorActionPreference = 'Stop'

$project = '.\examples\PerformanceComparison\PerformanceComparison.csproj'
$runs = 5
$rows = @()

$dotnetCommand = Get-Command dotnet -ErrorAction SilentlyContinue
$dotnetExe = $null
if ($dotnetCommand)
{
    $dotnetExe = $dotnetCommand.Source
}
if (-not $dotnetExe)
{
    $fallbackDotnet = 'C:\Program Files\dotnet\dotnet.exe'
    if (-not (Test-Path $fallbackDotnet))
    {
        throw "dotnet executable not found in PATH or at '$fallbackDotnet'."
    }

    $dotnetExe = $fallbackDotnet
}

function Get-Median([double[]]$values)
{
    if ($values.Count -eq 0) { return 0.0 }

    $sorted = $values | Sort-Object
    $n = $sorted.Count
    if ($n % 2 -eq 1)
    {
        return [double]$sorted[[int]($n / 2)]
    }

    return ([double]$sorted[($n / 2) - 1] + [double]$sorted[$n / 2]) / 2.0
}

for ($r = 1; $r -le $runs; $r++)
{
    Write-Host "Running pass $r/$runs"
    $output = & $dotnetExe run --project $project -c Release
    foreach ($line in $output)
    {
        if ($line -notlike 'RESULT|*')
        {
            continue
        }

        $parts = $line.Split('|')
        if ($parts.Count -lt 7)
        {
            continue
        }

        $rows += [pscustomobject]@{
            Run = $r
            Name = $parts[1]
            Iterations = [int]$parts[2]
            PayloadSize = [int]$parts[3]
            ElapsedMs = [double]$parts[4]
            Throughput = [double]$parts[5]
            BytesPerOp = [double]$parts[6]
        }
    }
}

$aggregated = $rows |
    Group-Object Name |
    ForEach-Object {
        $g = $_.Group
        $elapsed = @($g | ForEach-Object { [double]$_.ElapsedMs })
        $throughput = @($g | ForEach-Object { [double]$_.Throughput })
        $bytes = @($g | ForEach-Object { [double]$_.BytesPerOp })

        [pscustomobject]@{
            Name = $_.Name
            Samples = $g.Count
            Iterations = ($g | Select-Object -First 1).Iterations
            PayloadSize = ($g | Select-Object -First 1).PayloadSize
            ElapsedMs_Avg = [Math]::Round(($elapsed | Measure-Object -Average).Average, 6)
            ElapsedMs_Median = [Math]::Round((Get-Median $elapsed), 6)
            Throughput_Avg = [Math]::Round(($throughput | Measure-Object -Average).Average, 6)
            Throughput_Median = [Math]::Round((Get-Median $throughput), 6)
            BytesPerOp_Avg = [Math]::Round(($bytes | Measure-Object -Average).Average, 6)
            BytesPerOp_Median = [Math]::Round((Get-Median $bytes), 6)
        }
    } |
    Sort-Object Name

$byName = @{}
foreach ($item in $aggregated)
{
    $byName[$item.Name] = $item
}

function Add-Comparison([string]$group, [string]$label, [string]$baseline, [string]$contender)
{
    if (-not $byName.ContainsKey($baseline) -or -not $byName.ContainsKey($contender))
    {
        return $null
    }

    $b = $byName[$baseline]
    $c = $byName[$contender]

    $throughputDelta = if ($b.Throughput_Avg -eq 0) { 0.0 } else { (($c.Throughput_Avg - $b.Throughput_Avg) / $b.Throughput_Avg) * 100.0 }
    $timeReduction = if ($b.ElapsedMs_Avg -eq 0) { 0.0 } else { (($b.ElapsedMs_Avg - $c.ElapsedMs_Avg) / $b.ElapsedMs_Avg) * 100.0 }
    $allocReduction = if ($b.BytesPerOp_Avg -eq 0) { 0.0 } else { (($b.BytesPerOp_Avg - $c.BytesPerOp_Avg) / $b.BytesPerOp_Avg) * 100.0 }

    return [pscustomobject]@{
        Group = $group
        Label = $label
        Baseline = $baseline
        Contender = $contender
        ThroughputDeltaPct = [Math]::Round($throughputDelta, 3)
        TimeReductionPct = [Math]::Round($timeReduction, 3)
        AllocationReductionPct = [Math]::Round($allocReduction, 3)
    }
}

$comparisons = @()

$comparisons += Add-Comparison 'Text mode' 'Text small parse DOM -> visit' 'Text small object parse DOM' 'Text small object visit no DOM'
$comparisons += Add-Comparison 'Text mode' 'Text repeated parse DOM -> visit' 'Text repeated object parse DOM' 'Text repeated object visit no DOM'
$comparisons += Add-Comparison 'Text mode' 'Text wide parse DOM -> selective 1' 'Text wide root parse DOM' 'Text wide root selective property read'
$comparisons += Add-Comparison 'Text mode' 'Text wide parse DOM -> selective 8' 'Text wide root parse DOM' 'Text wide root selective 8 properties (one pass)'
$comparisons += Add-Comparison 'Text mode' 'Text repeated sync parse DOM -> async parse' 'Text repeated object parse DOM' 'Text async parse from stream'
$comparisons += Add-Comparison 'Text mode' 'Text repeated sync serialize -> async write' 'Text repeated object serialize compact' 'Text async write to StringWriter'
$comparisons += Add-Comparison 'Text mode' 'Text async write StringWriter -> MemoryStream' 'Text async write to StringWriter' 'Text async write to MemoryStream UTF8'

$comparisons += Add-Comparison 'Binary mode' 'Binary small DOM -> visit' 'Small object deserialize DOM' 'Small object visit no DOM'
$comparisons += Add-Comparison 'Binary mode' 'Binary repeated table DOM -> visit' 'Repeated strings deserialize DOM with string table' 'Repeated strings visit no DOM with string table'
$comparisons += Add-Comparison 'Binary mode' 'Binary repeated no-table DOM -> visit' 'Repeated strings deserialize DOM without string table' 'Repeated strings visit no DOM without string table'
$comparisons += Add-Comparison 'Binary mode' 'Binary packed DOM -> visit' 'Packed numeric array deserialize DOM' 'Packed numeric array visit no DOM'
$comparisons += Add-Comparison 'Binary mode' 'Binary large payload DOM -> visit' 'Large binary payload deserialize DOM' 'Large binary payload visit no DOM'
$comparisons += Add-Comparison 'Binary mode' 'Binary wide DOM -> selective 1' 'Wide root object deserialize DOM' 'Wide root object selective property read'
$comparisons += Add-Comparison 'Binary mode' 'Binary wide DOM -> selective 8' 'Wide root object deserialize DOM' 'Wide root object selective 8 properties (one pass)'
$comparisons += Add-Comparison 'Binary mode' 'Binary repeated sync read DOM -> async read memory' 'Repeated strings deserialize DOM with string table' 'Async read from ReadOnlyMemory to DOM'
$comparisons += Add-Comparison 'Binary mode' 'Binary repeated sync read DOM -> async read stream' 'Repeated strings deserialize DOM with string table' 'Async read from stream to DOM'
$comparisons += Add-Comparison 'Binary mode' 'Binary repeated sync serialize -> async write' 'Repeated strings serialize with string table' 'Async stream write serialize to MemoryStream'

$comparisons += Add-Comparison 'Text vs Binary' 'Small serialize: Text vs Binary' 'Text small object serialize' 'Small object serialize'
$comparisons += Add-Comparison 'Text vs Binary' 'Small parse DOM: Text vs Binary' 'Text small object parse DOM' 'Small object deserialize DOM'
$comparisons += Add-Comparison 'Text vs Binary' 'Small visit no DOM: Text vs Binary' 'Text small object visit no DOM' 'Small object visit no DOM'
$comparisons += Add-Comparison 'Text vs Binary' 'Repeated serialize: Text compact vs Binary with table' 'Text repeated object serialize compact' 'Repeated strings serialize with string table'
$comparisons += Add-Comparison 'Text vs Binary' 'Repeated serialize: Text compact vs Binary without table' 'Text repeated object serialize compact' 'Repeated strings serialize without string table'
$comparisons += Add-Comparison 'Text vs Binary' 'Repeated parse DOM: Text vs Binary (with table)' 'Text repeated object parse DOM' 'Repeated strings deserialize DOM with string table'
$comparisons += Add-Comparison 'Text vs Binary' 'Repeated parse DOM: Text vs Binary (without table)' 'Text repeated object parse DOM' 'Repeated strings deserialize DOM without string table'
$comparisons += Add-Comparison 'Text vs Binary' 'Repeated visit no DOM: Text vs Binary (with table)' 'Text repeated object visit no DOM' 'Repeated strings visit no DOM with string table'
$comparisons += Add-Comparison 'Text vs Binary' 'Repeated visit no DOM: Text vs Binary (without table)' 'Text repeated object visit no DOM' 'Repeated strings visit no DOM without string table'
$comparisons += Add-Comparison 'Text vs Binary' 'Wide selective 1: Text vs Binary' 'Text wide root selective property read' 'Wide root object selective property read'
$comparisons += Add-Comparison 'Text vs Binary' 'Wide selective 8: Text vs Binary' 'Text wide root selective 8 properties (one pass)' 'Wide root object selective 8 properties (one pass)'
$comparisons += Add-Comparison 'Text vs Binary' 'Async write stream parity: Text vs Binary' 'Text async write to MemoryStream UTF8' 'Async stream write serialize to MemoryStream'
$comparisons += Add-Comparison 'Text vs Binary' 'Async parse/read stream parity: Text vs Binary' 'Text async parse from stream' 'Async read from stream to DOM'

$comparisons += Add-Comparison 'CLR' 'CLR serialize: generated vs reflection' 'CLR reflection serialize' 'CLR generated serialize'
$comparisons += Add-Comparison 'CLR' 'CLR deserialize: generated vs reflection' 'CLR reflection deserialize' 'CLR generated deserialize'
$comparisons += Add-Comparison 'CLR' 'CLR attributed serialize: generated vs reflection' 'CLR attributed reflection serialize' 'CLR attributed generated serialize'
$comparisons += Add-Comparison 'CLR' 'CLR attributed deserialize missing defaults: generated vs reflection' 'CLR attributed reflection deserialize missing defaults' 'CLR attributed generated deserialize missing defaults'
$comparisons += Add-Comparison 'CLR' 'CLR advanced mapper/default serialize: generated vs reflection' 'CLR advanced mapper/default reflection serialize' 'CLR advanced mapper/default generated serialize'
$comparisons += Add-Comparison 'CLR' 'CLR advanced mapper/default deserialize: generated vs reflection' 'CLR advanced mapper/default reflection deserialize' 'CLR advanced mapper/default generated deserialize'
$comparisons += Add-Comparison 'CLR' 'CLR advanced preprocessor deserialize: generated vs reflection' 'CLR advanced preprocess reflection deserialize' 'CLR advanced preprocess generated deserialize'
$comparisons += Add-Comparison 'CLR' 'CLR advanced external-ref fixed deserialize: generated vs reflection' 'CLR advanced external-ref fixed reflection deserialize' 'CLR advanced external-ref fixed generated deserialize'
$comparisons += Add-Comparison 'CLR' 'CLR advanced external-ref fixed serialize: generated vs reflection' 'CLR advanced external-ref fixed reflection serialize' 'CLR advanced external-ref fixed generated serialize'
$comparisons += Add-Comparison 'CLR' 'CLR advanced polymorphic serialize: generated vs reflection' 'CLR advanced polymorphic reflection serialize' 'CLR advanced polymorphic generated serialize'
$comparisons += Add-Comparison 'CLR' 'CLR advanced polymorphic deserialize: generated vs reflection' 'CLR advanced polymorphic reflection deserialize' 'CLR advanced polymorphic generated deserialize'

$comparisons = $comparisons | Where-Object { $_ -ne $null }

$outDir = '.\examples\PerformanceComparison'
$aggPath = Join-Path $outDir 'benchmark-aggregate.json'
$cmpPath = Join-Path $outDir 'benchmark-comparisons.json'

$aggregated | ConvertTo-Json -Depth 4 | Set-Content -Path $aggPath -Encoding UTF8
$comparisons | ConvertTo-Json -Depth 4 | Set-Content -Path $cmpPath -Encoding UTF8

Write-Host "Saved aggregate to $aggPath"
Write-Host "Saved comparisons to $cmpPath"

Write-Host ""
Write-Host "Text vs Binary deltas (avg):"
$comparisons |
    Where-Object { $_.Group -eq 'Text vs Binary' } |
    Select-Object Label, ThroughputDeltaPct, TimeReductionPct, AllocationReductionPct |
    Format-Table -AutoSize |
    Out-String |
    Write-Host
