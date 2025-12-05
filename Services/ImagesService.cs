using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace BE_QLTiemThuoc.Services
{
    // Single-file service for Images operations (no repository needed)
    public class ImagesService
    {
        private static readonly HttpClient _http = new HttpClient();
        private readonly IWebHostEnvironment _env;

        public ImagesService(IWebHostEnvironment env)
        {
            _env = env ?? throw new ArgumentNullException(nameof(env));
        }

        public string GetTempUploadFolder()
        {
            var beRoot = _env.ContentRootPath ?? Directory.GetCurrentDirectory();
            var candidate = Path.GetFullPath(Path.Combine(beRoot, "..", "TempImages"));
            if (!Directory.Exists(candidate)) Directory.CreateDirectory(candidate);
            return candidate;
        }

        public string GetImagesFolder()
        {
            var beRoot = _env.ContentRootPath ?? Directory.GetCurrentDirectory();
            var candidate = Path.GetFullPath(Path.Combine(beRoot, "..", "FE_QLTiemThuoc", "wwwroot", "assets_user", "img", "product"));
            if (!Directory.Exists(candidate))
            {
                try { Directory.CreateDirectory(candidate); } catch { }
            }
            return candidate;
        }

        public async Task<string[]> ListAsync()
        {
            var folder = GetImagesFolder();
            if (!Directory.Exists(folder)) return Array.Empty<string>();
            var files = Directory.GetFiles(folder)
                .Select(f => Path.GetFileName(f))
                .OrderBy(n => n)
                .ToArray();
            return files;
        }

        public async Task<string> UploadExternalAsync(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) throw new Exception("Missing url");
            var uri = new Uri(url);
            var fileName = Path.GetFileName(uri.LocalPath);
            if (string.IsNullOrWhiteSpace(fileName)) fileName = "imported-image" + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + ".jpg";

            var folder = GetImagesFolder();
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            var safeName = SanitizeFileName(fileName);
            var target = Path.Combine(folder, safeName);
            var baseName = Path.GetFileNameWithoutExtension(safeName);
            var ext = Path.GetExtension(safeName);
            int counter = 1;
            while (File.Exists(target))
            {
                var newName = baseName + "_" + counter + ext;
                target = Path.Combine(folder, newName);
                counter++;
            }

            var bytes = await _http.GetByteArrayAsync(url);
            await File.WriteAllBytesAsync(target, bytes);

            return Path.GetFileName(target);
        }

        public async Task<string> UploadFileAsync(IFormFile file)
        {
            if (file == null) throw new Exception("File missing");

            var folder = GetTempUploadFolder();
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            var safeName = SanitizeFileName(file.FileName ?? ("upload_" + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + ".dat"));
            var target = Path.Combine(folder, safeName);
            var baseName = Path.GetFileNameWithoutExtension(safeName);
            var ext = Path.GetExtension(safeName);
            int counter = 1;
            while (File.Exists(target))
            {
                var newName = baseName + "_" + counter + ext;
                target = Path.Combine(folder, newName);
                counter++;
            }

            using (var stream = File.Create(target))
            {
                await file.CopyToAsync(stream);
            }

            return Path.GetFileName(target);
        }

        public async Task<string> UploadToProductAsync(IFormFile file)
        {
            if (file == null) throw new Exception("File missing");

            var folder = GetImagesFolder();
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            var safeName = SanitizeFileName(file.FileName ?? ("upload_" + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + ".dat"));
            var target = Path.Combine(folder, safeName);
            var baseName = Path.GetFileNameWithoutExtension(safeName);
            var ext = Path.GetExtension(safeName);
            int counter = 1;
            while (File.Exists(target))
            {
                var newName = baseName + "_" + counter + ext;
                target = Path.Combine(folder, newName);
                counter++;
            }

            using (var stream = File.Create(target))
            {
                await file.CopyToAsync(stream);
            }

            return Path.GetFileName(target);
        }

        public (byte[] bytes, string contentType) GetTempFile(string? filename)
        {
            if (string.IsNullOrWhiteSpace(filename)) throw new FileNotFoundException();
            var folder = GetTempUploadFolder();
            var safe = SanitizeFileName(Path.GetFileName(filename));
            var path = Path.Combine(folder, safe);
            if (!File.Exists(path)) throw new FileNotFoundException();

            var ext = Path.GetExtension(path).ToLowerInvariant();
            var contentType = ext switch
            {
                ".png" => "image/png",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".webp" => "image/webp",
                ".gif" => "image/gif",
                ".avif" => "image/avif",
                _ => "application/octet-stream",
            };

            var bytes = System.IO.File.ReadAllBytes(path);
            return (bytes, contentType);
        }

        public async Task<string> FinalizeImageAsync(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) throw new Exception("fileName required");

            var tempFolder = GetTempUploadFolder();
            var tempPath = Path.Combine(tempFolder, fileName);
            if (!File.Exists(tempPath)) throw new Exception("File not found in temp folder");

            var finalFolder = GetImagesFolder();
            if (!Directory.Exists(finalFolder)) Directory.CreateDirectory(finalFolder);
            var finalPath = Path.Combine(finalFolder, fileName);

            var baseName = Path.GetFileNameWithoutExtension(fileName);
            var ext = Path.GetExtension(fileName);
            int counter = 1;
            while (File.Exists(finalPath))
            {
                var newName = baseName + "_" + counter + ext;
                finalPath = Path.Combine(finalFolder, newName);
                counter++;
            }

            File.Copy(tempPath, finalPath, overwrite: true);
            File.Delete(tempPath);

            return Path.GetFileName(finalPath);
        }

        public string SanitizeFileName(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
            return name.Replace(' ', '_');
        }
    }
}
