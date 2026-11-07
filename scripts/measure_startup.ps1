# Script to launch server and measure exact startup time
$logPath = "d:\Lập trình web\OmnichannelBeautySolution\server_log.txt"
if (Test-Path $logPath) { Remove-Item $logPath -Force }

$sw = [System.Diagnostics.Stopwatch]::StartNew()
$psi = New-Object System.Diagnostics.ProcessStartInfo
$psi.FileName = "dotnet"
$psi.Arguments = "run --project Omnichannel.Web/Omnichannel.Web.csproj --launch-profile http"
$psi.WorkingDirectory = "d:\Lập trình web\OmnichannelBeautySolution"
$psi.RedirectStandardOutput = $true
$psi.RedirectStandardError = $true
$psi.UseShellExecute = $false
$psi.CreateNoWindow = $true

$proc = New-Object System.Diagnostics.Process
$proc.StartInfo = $psi

# Stream stdout/stderr to server_log.txt
$writer = [System.IO.StreamWriter]::new($logPath, $false, [System.Text.Encoding]::UTF8)
$writer.AutoFlush = $true

$outEvent = Register-ObjectEvent -InputObject $proc -EventName "OutputDataReceived" -Action {
    if ($EventArgs.Data) { $writer.WriteLine($EventArgs.Data) }
}
$errEvent = Register-ObjectEvent -InputObject $proc -EventName "ErrorDataReceived" -Action {
    if ($EventArgs.Data) { $writer.WriteLine("[ERR] " + $EventArgs.Data) }
}

$proc.Start() | Out-Null
$proc.BeginOutputReadLine()
$proc.BeginErrorReadLine()

$ready = $false
$client = New-Object System.Net.Http.HttpClient
$client.Timeout = [TimeSpan]::FromSeconds(2)

for ($i = 0; $i -lt 60; $i++) {
    Start-Sleep -Milliseconds 500
    try {
        $resp = $client.GetAsync("http://localhost:5116/").Result
        if ($resp -and [int]$resp.StatusCode -eq 200) {
            $ready = $true
            break
        }
    } catch {
        # still booting
    }
}

$sw.Stop()
$elapsed = $sw.ElapsedMilliseconds

Write-Host "Server Process ID: $($proc.Id)"
Write-Host "Startup Status: $(if ($ready) {'SUCCESS'} else {'FAILED'})"
Write-Host "Startup Time (ms): $elapsed"
