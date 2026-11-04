$sw = [System.Diagnostics.Stopwatch]::StartNew()
$ready = $false
while ($sw.ElapsedMilliseconds -lt 40000) {
    try {
        $resp = Invoke-WebRequest -Uri "http://localhost:5116/" -UseBasicParsing -TimeoutSec 2
        if ($resp.StatusCode -eq 200) {
            $ready = $true
            break
        }
    } catch {
        Start-Sleep -Milliseconds 500
    }
}
$sw.Stop()
Write-Host "Server Ready: $ready"
Write-Host "Elapsed ms: $($sw.ElapsedMilliseconds)"
