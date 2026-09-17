#Requires -Version 5.1
<#
.SYNOPSIS
    Install OpenAI Codex CLI
.DESCRIPTION
    Installs the OpenAI Codex CLI via npm.
    Requires Node.js to be installed first.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Write-Host "Installing OpenAI Codex CLI..." -ForegroundColor Cyan

# Verify Node.js is available
try {
    $nodeVersion = node --version 2>&1
    Write-Host "  Node.js found: $nodeVersion" -ForegroundColor Gray
} catch {
    Write-Error "Node.js is required to install Codex CLI. Please install Node.js first."
    exit 1
}

# Install via npm
try {
    Write-Host "  Running: npm install -g @openai/codex" -ForegroundColor Gray
    npm install -g @openai/codex 2>&1 | ForEach-Object { Write-Host "  $_" -ForegroundColor Gray }
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "npm install failed with exit code $LASTEXITCODE"
        exit 1
    }
    
    Write-Host "  ✓ Codex CLI installed successfully." -ForegroundColor Green
    exit 0
} catch {
    Write-Error "Failed to install Codex CLI: $_"
    exit 1
}
