$folderPath = Join-Path $PSScriptRoot "\logs"

if (Test-Path $folderPath) {
    Get-ChildItem -Path $folderPath -File | Remove-Item -Force

    Write-Host "All files from the folder have been deleted."
} else {
    Write-Host "Folder does not exist: $folderPath"
}
