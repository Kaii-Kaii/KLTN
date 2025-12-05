Test PhieuQuyDoi APIs

Files:
- test-phieuquydoi.ps1: PowerShell script to call the three endpoints you added:
  1. POST /api/PhieuQuyDoi/Create            (detailed, by MaLo)
  2. POST /api/PhieuQuyDoi/QuickByName       (quick convert by TenThuoc, 1 unit)
  3. POST /api/PhieuQuyDoi/QuickByNameQuantity (quick convert by TenThuoc + quantity)

How to run
1. Start your backend in Debug (run the BE_QLTiemThuoc project). Note the URL (http://localhost:5000 or https://localhost:5001). Update $baseUrl in the script if needed.

2. Open PowerShell as Administrator (if you need to bypass certificate validation). If your dev certificate is self-signed and you get cert errors, you can uncomment the validation bypass in the script:
   [System.Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }

3. In PowerShell, cd to the Tests folder and run the script:
   cd BE_QLTiemThuoc\Tests
   .\test-phieuquydoi.ps1

Adjust payload values before running:
- For the detailed endpoint, replace `MaLoGoc` with an actual `MaLo` from your `TON_KHO` table (see sample screenshot in your attachments).
- For QuickByName endpoints, replace `TenThuoc` with the real medicine name stored in your `THUOC` table. If you added `UnitsPerPackage` to `THUOC`, the script will use that when you omit `UnitsPerPackage` in the payload.

Notes:
- The script prints request JSON and the server response in JSON form.
- After a successful test, verify DB changes manually in tables `TON_KHO`, `PHIEU_QUY_DOI`, and `CT_PHIEU_QUY_DOI`.
- If you want, I can also produce an equivalent curl file or a small C# console tester using HttpClient.
