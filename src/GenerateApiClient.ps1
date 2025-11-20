# gen-api.ps1

# Configuration
$apiUrl = "https://localhost:4001/openapi/v1.json"
$outputFile = "./../Starter.Web/open-api-schema.json"
$outputDir = "./src/api"
$apiName = "api.ts"

Write-Host "Downloading OpenAPI schema..." -ForegroundColor Green

try {
    # # Ignore SSL certificate validation for self-signed certificates
    # [System.Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }
    
    # Download the OpenAPI JSON file
    Invoke-WebRequest -Uri $apiUrl -OutFile $outputFile -UseBasicParsing
    
    Write-Host "Downloaded successfully to $outputFile" -ForegroundColor Green
    
    # Check if file exists and has content
    if ((Test-Path $outputFile) -and (Get-Item $outputFile).Length -gt 0) {
        Write-Host "Generating TypeScript API client..." -ForegroundColor Green
        
        # Generate TypeScript API from local file
        npx swagger-typescript-api generate -p $outputFile -o $outputDir --n $apiName
        
        Write-Host "API client generated successfully!" -ForegroundColor Green
        
        # Clean up the temporary file
        Remove-Item $outputFile
        Write-Host "Cleaned up temporary file" -ForegroundColor Green
        
    }
    else {
        Write-Host "Downloaded file is empty or doesn't exist" -ForegroundColor Red
        exit 1
    }
    
}
catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host "Done! Check $outputDir for your generated files." -ForegroundColor Cyan