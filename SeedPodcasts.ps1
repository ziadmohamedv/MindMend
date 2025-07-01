# PowerShell Script to Seed Podcasts via API
param(
    [string]$Action = "seed"
)

$ApiBase = "http://localhost:5000/api/Seed"

function Invoke-SeedPodcasts {
    Write-Host "🎧 Starting podcast seeding..." -ForegroundColor Yellow
    Write-Host "📡 Calling API: $ApiBase/podcasts" -ForegroundColor Cyan
    
    try {
        $response = Invoke-RestMethod -Uri "$ApiBase/podcasts" -Method Post -TimeoutSec 300
        Write-Host "✅ $($response.message)" -ForegroundColor Green
        Write-Host "📊 Total podcasts: $($response.totalPodcasts)" -ForegroundColor Blue
        return $true
    }
    catch {
        Write-Host "❌ Error seeding podcasts:" -ForegroundColor Red
        Write-Host $_.Exception.Message -ForegroundColor Red
        return $false
    }
}

function Get-PodcastsCount {
    try {
        $response = Invoke-RestMethod -Uri "$ApiBase/podcasts/count" -Method Get
        Write-Host "📊 Current podcasts in database: $($response.totalPodcasts)" -ForegroundColor Blue
        return $response.totalPodcasts
    }
    catch {
        Write-Host "❌ Error getting podcast count:" -ForegroundColor Red
        Write-Host $_.Exception.Message -ForegroundColor Red
        return $null
    }
}

function Clear-Podcasts {
    Write-Host "🗑️ Clearing all podcasts..." -ForegroundColor Yellow
    
    try {
        $response = Invoke-RestMethod -Uri "$ApiBase/podcasts" -Method Delete
        Write-Host "✅ $($response.message)" -ForegroundColor Green
        return $true
    }
    catch {
        Write-Host "❌ Error clearing podcasts:" -ForegroundColor Red
        Write-Host $_.Exception.Message -ForegroundColor Red
        return $false
    }
}

function Invoke-RefreshPodcasts {
    Write-Host "🔄 Refreshing podcasts..." -ForegroundColor Yellow
    
    try {
        $response = Invoke-RestMethod -Uri "$ApiBase/podcasts/refresh" -Method Post -TimeoutSec 300
        Write-Host "✅ $($response.message)" -ForegroundColor Green
        Write-Host "📊 Total podcasts: $($response.totalPodcasts)" -ForegroundColor Blue
        return $true
    }
    catch {
        Write-Host "❌ Error refreshing podcasts:" -ForegroundColor Red
        Write-Host $_.Exception.Message -ForegroundColor Red
        return $false
    }
}

# Main execution
switch ($Action.ToLower()) {
    "seed" {
        Write-Host "📚 Starting podcast seeding process..." -ForegroundColor Magenta
        Write-Host "📊 Current state:" -ForegroundColor Cyan
        Get-PodcastsCount
        Write-Host ""
        
        if (Invoke-SeedPodcasts) {
            Write-Host ""
            Write-Host "📊 Final state:" -ForegroundColor Cyan
            Get-PodcastsCount
        } else {
            Write-Host ""
            Write-Host "❌ Seeding failed. Check if the API is running and try again." -ForegroundColor Red
        }
    }
    "count" {
        Get-PodcastsCount
    }
    "clear" {
        Clear-Podcasts
    }
    "refresh" {
        Invoke-RefreshPodcasts
    }
    default {
        Write-Host "Usage: .\SeedPodcasts.ps1 [-Action <seed|count|clear|refresh>]" -ForegroundColor Yellow
        Write-Host "  seed    - Seed podcasts from podcast.json (default)" -ForegroundColor White
        Write-Host "  count   - Get current podcast count" -ForegroundColor White
        Write-Host "  clear   - Clear all podcasts" -ForegroundColor White
        Write-Host "  refresh - Clear and re-seed podcasts" -ForegroundColor White
    }
}
