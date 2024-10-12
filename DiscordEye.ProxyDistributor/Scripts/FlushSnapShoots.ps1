$file1 = "../bin/Debug/net8.0/ProxyStateSnapshot.json"
$file2 = "../bin/Debug/net8.0/ProxyHeartbeatSnapshot.json"

if (Test-Path $file1) {
    Clear-Content $file1
    Write-Host "File $file1 flushed."
} else {
    Write-Host "File $file1 not flushed."
}

if (Test-Path $file2) {
    Clear-Content $file2
    Write-Host "File $file2 flushed."
} else {
    Write-Host "File $file2 not flushed."
}
