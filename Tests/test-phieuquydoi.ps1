# PowerShell script to test PhieuQuyDoi APIs
# Usage: Open PowerShell, cd to this folder and run: .\test-phieuquydoi.ps1
# Adjust $baseUrl and payload values below to match your local dev server and real data.

# If your dev server uses self-signed cert, uncomment the line below to skip cert validation for this session
# [System.Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }

$baseUrl = 'http://localhost:5203'   # <-- adjusted to the local server URL observed when running the app

function Get-Json {
    param([string]$endpoint)
    $url = "$baseUrl$endpoint"
    try {
        return Invoke-RestMethod -Method Get -Uri $url -ContentType 'application/json'
    } catch {
        Write-Host "GET $url failed:" -ForegroundColor Red
        $_ | Format-List * -Force
        return $null
    }
}

function Post-Json {
    param(
        [string]$endpoint,
        [hashtable]$payload
    )
    $url = "$baseUrl$endpoint"
    $json = $payload | ConvertTo-Json -Depth 6
    Write-Host "POST $url" -ForegroundColor Cyan
    Write-Host $json

    try {
        $resp = Invoke-RestMethod -Method Post -Uri $url -Body $json -ContentType 'application/json'
        Write-Host "Response:" -ForegroundColor Green
        $resp | ConvertTo-Json -Depth 6
    } catch {
        Write-Host "Request failed:" -ForegroundColor Red
        $_ | Format-List * -Force
    }
    Write-Host "`n-------------------------------`n"
}

# 1) Detailed conversion by MaLo (chi tiết theo lô)
# Try to auto-discover a valid sealed lot (ChuaTachLe) from the API; fallback to hard-coded example if none found
$chua = Get-Json -endpoint '/api/ThuocView/ChuaTachLe'
if ($chua -and $chua.Count -gt 0) {
    $first = $chua[0]
    $maLoGoc = $first.MaLo
    $maThuocSample = $first.MaThuoc
    Write-Host "Using discovered MaLoGoc: $maLoGoc (MaThuoc: $maThuocSample)" -ForegroundColor Cyan
} else {
    $maLoGoc = 'LO25110611220901'
    $maThuocSample = 'T005'
    Write-Host "No ChuaTachLe results; falling back to example MaLoGoc: $maLoGoc" -ForegroundColor Yellow
}

$payload1 = @{
    MaLoGoc = $maLoGoc
    SoLuongGoc = 1
    UnitsPerPackage = 10
    # do not pass MaNV so the service can pick an existing NhanVien
    GhiChu = 'Test tách lẻ chi tiết (auto-discovered)'
}
Post-Json -endpoint '/api/PhieuQuyDoi/Create' -payload $payload1


# 2) Quick convert by MaThuoc (1 đơn vị)
# Replace MaThuoc with a real code from your THUOC table (e.g. T005)
# 2) Quick convert by MaThuoc (1 đơn vị) — prefer discovered MaThuoc from ChuaTachLe
# QuickByMa (single unit) has been removed; use QuickByMaQuantity or QuickByName instead.

# 3) Quick convert by MaThuoc with quantity (có thể tiêu nhiều lô theo HSD gần nhất)
$payload3 = @{
    MaThuoc = $maThuocSample
    SoLuongGoc = 5
    UnitsPerPackage = 10
}
Post-Json -endpoint '/api/PhieuQuyDoi/QuickByMaQuantity' -payload $payload3

Write-Host "Done. If responses indicate success, check your TON_KHO, PHIEU_QUY_DOI and CT_PHIEU_QUY_DOI tables to verify changes." -ForegroundColor Yellow

# End of script
