# Mind-Mend Book Database Seeder
# PowerShell script to seed books into the database

param(
    [string]$BaseUrl = "https://localhost:7140",
    [switch]$Refresh,
    [switch]$Count
)

function Test-ApiConnection {
    param([string]$Url)
    
    try {
        $response = Invoke-RestMethod -Uri "$Url/api/Seed/books/count" -Method Get -SkipCertificateCheck -ErrorAction Stop
        return $true
    }
    catch {
        return $false
    }
}

function Get-BookCount {
    param([string]$Url)
    
    try {
        $response = Invoke-RestMethod -Uri "$Url/api/Seed/books/count" -Method Get -SkipCertificateCheck
        Write-Host "📊 Current books in database: $($response.totalBooks)" -ForegroundColor Green
    }
    catch {
        Write-Host "❌ Could not get book count: $($_.Exception.Message)" -ForegroundColor Red
    }
}

function Invoke-SeedBooks {
    param([string]$Url)
    
    try {
        Write-Host "🌱 Seeding books from book.json..." -ForegroundColor Yellow
        $response = Invoke-RestMethod -Uri "$Url/api/Seed/books" -Method Post -SkipCertificateCheck
        Write-Host "✅ $($response.message)" -ForegroundColor Green
        Write-Host "📈 Total books after seeding: $($response.totalBooks)" -ForegroundColor Green
    }
    catch {
        Write-Host "❌ Seeding failed: $($_.Exception.Message)" -ForegroundColor Red
    }
}

function Invoke-RefreshBooks {
    param([string]$Url)
    
    try {
        Write-Host "🔄 Refreshing book database..." -ForegroundColor Yellow
        $response = Invoke-RestMethod -Uri "$Url/api/Seed/books/refresh" -Method Post -SkipCertificateCheck
        Write-Host "✅ $($response.message)" -ForegroundColor Green
        Write-Host "📈 Total books after refresh: $($response.totalBooks)" -ForegroundColor Green
    }
    catch {
        Write-Host "❌ Refresh failed: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# Main script
Write-Host "Mind-Mend Book Database Seeder" -ForegroundColor Cyan
Write-Host "=" * 50 -ForegroundColor Cyan

# Test API connection
Write-Host "🔍 Testing API connection at $BaseUrl..." -ForegroundColor Yellow
if (-not (Test-ApiConnection -Url $BaseUrl)) {
    Write-Host "❌ Could not connect to API at $BaseUrl" -ForegroundColor Red
    Write-Host "   Make sure the server is running with 'dotnet run'" -ForegroundColor Yellow
    exit 1
}

Write-Host "✅ API connection successful!" -ForegroundColor Green

# Execute based on parameters
if ($Count) {
    Get-BookCount -Url $BaseUrl
}
elseif ($Refresh) {
    $confirmation = Read-Host "⚠️ This will delete all existing books. Continue? (yes/no)"
    if ($confirmation -eq "yes") {
        Invoke-RefreshBooks -Url $BaseUrl
    }
    else {
        Write-Host "❌ Operation cancelled." -ForegroundColor Red
    }
}
else {
    # Default: seed books
    Get-BookCount -Url $BaseUrl
    Invoke-SeedBooks -Url $BaseUrl
}

Write-Host "`n✅ Script completed!" -ForegroundColor Green
