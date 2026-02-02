# Stop any process using port 5278
$port = 5278
$processId = (Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue).OwningProcess
if ($processId) {
    Write-Host "Stopping process $processId using port $port..." -ForegroundColor Yellow
    Stop-Process -Id $processId -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 1
}

# Start the application
Write-Host "Starting NotesApp.API..." -ForegroundColor Green
dotnet run
