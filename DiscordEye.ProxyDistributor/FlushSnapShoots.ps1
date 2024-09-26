$file1 = "bin/Debug/net8.0/ProxyStateSnapshot.json"
$file2 = "bin/Debug/net8.0/ProxyHeartbeatSnapshot.json"

if (Test-Path $file1) {
    Clear-Content $file1
    Write-Host "Файл $file1 очищен."
} else {
    Write-Host "Файл $file1 не найден."
}

if (Test-Path $file2) {
    Clear-Content $file2
    Write-Host "Файл $file2 очищен."
} else {
    Write-Host "Файл $file2 не найден."
}
