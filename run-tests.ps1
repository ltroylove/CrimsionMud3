# PowerShell script to run different categories of tests
param(
    [string]$Category = "all",
    [switch]$Verbose
)

$projectDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $projectDir

Write-Host "C3Mud Test Runner - TDD Red Phase" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan

$verbosity = if ($Verbose) { "detailed" } else { "normal" }

switch ($Category.ToLower()) {
    "tcp" {
        Write-Host "Running TCP Server Tests..." -ForegroundColor Yellow
        dotnet test --filter "FullyQualifiedName~TcpServerTests" --verbosity $verbosity
    }
    "telnet" {
        Write-Host "Running Telnet Protocol Tests..." -ForegroundColor Yellow
        dotnet test --filter "FullyQualifiedName~TelnetProtocolTests" --verbosity $verbosity
    }
    "performance" {
        Write-Host "Running Performance Tests..." -ForegroundColor Yellow
        dotnet test --filter "FullyQualifiedName~PerformanceTests" --verbosity $verbosity
    }
    "connection" {
        Write-Host "Running Connection Management Tests..." -ForegroundColor Yellow
        dotnet test --filter "FullyQualifiedName~ConnectionManagementTests" --verbosity $verbosity
    }
    "descriptor" {
        Write-Host "Running Connection Descriptor Tests..." -ForegroundColor Yellow
        dotnet test --filter "FullyQualifiedName~ConnectionDescriptorTests" --verbosity $verbosity
    }
    "integration" {
        Write-Host "Running Integration Tests..." -ForegroundColor Yellow
        dotnet test --filter "FullyQualifiedName~NetworkingIntegrationTests" --verbosity $verbosity
    }
    "unit" {
        Write-Host "Running All Unit Tests..." -ForegroundColor Yellow
        dotnet test --filter "FullyQualifiedName~C3Mud.Tests.Networking" --verbosity $verbosity
    }
    "all" {
        Write-Host "Running All Tests..." -ForegroundColor Yellow
        dotnet test --verbosity $verbosity
    }
    default {
        Write-Host "Unknown category: $Category" -ForegroundColor Red
        Write-Host "Available categories: tcp, telnet, performance, connection, descriptor, integration, unit, all" -ForegroundColor White
        exit 1
    }
}

Write-Host ""
Write-Host "TDD Red Phase Status: All tests should be FAILING (NotImplementedException)" -ForegroundColor Green
Write-Host "This is expected behavior - we need to implement the actual classes next!" -ForegroundColor Green