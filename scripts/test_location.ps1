$sw = [System.Diagnostics.Stopwatch]::StartNew()
$r = Invoke-WebRequest -Uri "http://localhost:5116/api/location/provinces" -UseBasicParsing
$sw.Stop()
Write-Host "Status: $($r.StatusCode)"
Write-Host "api/location/provinces Elapsed ms: $($sw.ElapsedMilliseconds)"
Write-Host "Data length: $($r.Content.Length) chars"
