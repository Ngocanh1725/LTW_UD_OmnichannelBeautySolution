$largeFiles = Get-ChildItem -Path "Omnichannel.Web/wwwroot" -Recurse -File | 
    Where-Object { $_.Length -gt 300KB } | 
    Select-Object Name, @{Name="SizeKB";Expression={[math]::Round($_.Length / 1024, 1)}}, @{Name="RelativePath";Expression={$_.FullName.Replace((Get-Location).Path, "")}} |
    Sort-Object SizeKB -Descending

Write-Host "Found $($largeFiles.Count) files > 300KB in wwwroot:"
$largeFiles | Format-Table -AutoSize
