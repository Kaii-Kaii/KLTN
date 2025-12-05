# Test PayOS API với signature
$baseUrl = "https://localhost:7167"

Write-Host "=== Test PayOS API với Signature ===" -ForegroundColor Green

# 1. Test cấu hình PayOS
Write-Host "`n1. Kiểm tra cấu hình PayOS..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/SimplePayment/Test" -Method GET -SkipCertificateCheck
    Write-Host "✓ Cấu hình PayOS:" -ForegroundColor Green
    $response | ConvertTo-Json -Depth 3 | Write-Host
} catch {
    Write-Host "✗ Lỗi kiểm tra cấu hình: $($_.Exception.Message)" -ForegroundColor Red
}

# 2. Test tạo payment với signature
Write-Host "`n2. Test tạo payment link với signature..." -ForegroundColor Yellow
$paymentRequest = @{
    Amount = 50000
    Description = "Test thanh toán với signature"
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/SimplePayment/Create" -Method POST -Body $paymentRequest -ContentType "application/json" -SkipCertificateCheck
    Write-Host "✓ Tạo payment thành công:" -ForegroundColor Green
    $response | ConvertTo-Json -Depth 3 | Write-Host
    
    if ($response.data -and $response.data.PaymentUrl) {
        Write-Host "`n📎 Payment URL: $($response.data.PaymentUrl)" -ForegroundColor Cyan
        Write-Host "📎 Order Code: $($response.data.OrderCode)" -ForegroundColor Cyan
    }
} catch {
    Write-Host "✗ Lỗi tạo payment: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $errorDetails = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($errorDetails)
        $errorContent = $reader.ReadToEnd()
        Write-Host "Chi tiết lỗi: $errorContent" -ForegroundColor Red
    }
}

# 3. Test với số tiền khác
Write-Host "`n3. Test với số tiền 100,000 VND..." -ForegroundColor Yellow
$paymentRequest2 = @{
    Amount = 100000
    Description = "Test thanh toán 100K"
} | ConvertTo-Json

try {
    $response2 = Invoke-RestMethod -Uri "$baseUrl/api/SimplePayment/Create" -Method POST -Body $paymentRequest2 -ContentType "application/json" -SkipCertificateCheck
    Write-Host "✓ Tạo payment 100K thành công:" -ForegroundColor Green
    $response2 | ConvertTo-Json -Depth 3 | Write-Host
} catch {
    Write-Host "✗ Lỗi tạo payment 100K: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n=== Kết thúc test ===" -ForegroundColor Green