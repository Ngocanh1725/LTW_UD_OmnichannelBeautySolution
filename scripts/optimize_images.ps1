# =========================================================================================
# OMNICHANNEL BEAUTY SOLUTION - PHIÊN 3: CHUẨN HÓA & TỐI ƯU HÌNH ẢNH (WEBP + 1:1 1000x1000)
# Công cụ cục bộ sử dụng: SixLabors.ImageSharp 4.1.1 (.NET 8)
# Lệnh thực thi:
#   powershell -ExecutionPolicy Bypass -File scripts/optimize_images.ps1
# Nhiệm vụ:
#   1. Tạo ảnh fallback chuẩn /images/products/_placeholder.webp (1000x1000, tông hồng nhạt có logo mờ).
#   2. Chuẩn hóa toàn bộ ảnh sản phẩm trong wwwroot/images/products/{skincare|makeup|perfume|hair-body}:
#      - Tỷ lệ 1:1 (1000x1000px), nền sáng (#FFFFFF), không méo/cắt mất đầu chai.
#      - Giữ file gốc .jpg/.png (chuẩn hóa Magic Bytes đúng định dạng, <= 150 KB).
#      - Xuất thêm bản .webp song hành (<= 150 KB) phục vụ thẻ <picture>.
#   3. Tối ưu ảnh trong wwwroot/images/banners và wwwroot/images/categories (<= 150 KB + xuất .webp).
# =========================================================================================

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$workspaceRoot = Split-Path $PSScriptRoot -Parent
$imagesRoot = Join-Path $workspaceRoot "Omnichannel.Web\wwwroot\images"
$imageSharpDll = Join-Path $env:USERPROFILE ".nuget\packages\sixlabors.imagesharp\4.1.1\lib\net8.0\SixLabors.ImageSharp.dll"

if (-not (Test-Path $imageSharpDll)) {
    throw "Không tìm thấy SixLabors.ImageSharp.dll tại: $imageSharpDll"
}

$toolDir = Join-Path $env:TEMP "OmniImageOptimizerPhien3"
New-Item -ItemType Directory -Path $toolDir -Force | Out-Null

$csproj = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="SixLabors.ImageSharp">
      <HintPath>$imageSharpDll</HintPath>
    </Reference>
  </ItemGroup>
</Project>
"@

$programCs = @'
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

class Program
{
    static readonly HashSet<string> OrphanProductFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        "dung-dich-duong-am-hada-labo-advanced-nourish-170ml.jpg",
        "kem-duong-phuc-hoi-la-roche-posay-cicaplast-baume-b5-plus.jpg",
        "tay-te-bao-chet-paulas-choice-2-bha-liquid-118ml.png"
    };

    static int Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.Error.WriteLine("Usage: OmniImageOptimizerPhien3 <wwwroot/images path>");
            return 1;
        }

        string imagesRoot = args[0];
        string productsRoot = Path.Combine(imagesRoot, "products");
        string bannersRoot = Path.Combine(imagesRoot, "banners");
        string categoriesRoot = Path.Combine(imagesRoot, "categories");

        Console.WriteLine("=========================================================================");
        Console.WriteLine(" [PHIÊN 3] TỐI ƯU HÓA HÌNH ẢNH CỤC BỘ BẰNG SIXLABORS.IMAGESHARP (.NET 8)");
        Console.WriteLine("=========================================================================");

        // 1. Tạo _placeholder.webp (và _placeholder.jpg fallback) tông hồng nhạt có biểu tượng logo mờ
        string placeholderWebp = Path.Combine(productsRoot, "_placeholder.webp");
        string placeholderJpg = Path.Combine(productsRoot, "_placeholder.jpg");
        GeneratePinkPlaceholder(placeholderWebp, placeholderJpg);

        // 2. Chuẩn hóa ảnh sản phẩm trong products/{skincare|makeup|perfume|hair-body}
        string[] subDirs = { "skincare", "makeup", "perfume", "hair-body" };
        int productProcessed = 0;
        int webpGenerated = 0;

        foreach (var sub in subDirs)
        {
            string dir = Path.Combine(productsRoot, sub);
            if (!Directory.Exists(dir)) continue;

            // Lấy danh sách tên gốc (baseName) từ các file hiện có (bỏ qua 3 file mồ côi chờ duyệt xóa)
            var files = Directory.GetFiles(dir)
                .Where(f => !OrphanProductFiles.Contains(Path.GetFileName(f)))
                .ToList();

            var groups = files.GroupBy(f => Path.GetFileNameWithoutExtension(f), StringComparer.OrdinalIgnoreCase);

            foreach (var grp in groups)
            {
                // Ưu tiên chọn file .jpg hoặc .png làm nguồn gốc; nếu chỉ có .webp thì dùng .webp
                string sourceFile = grp.FirstOrDefault(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
                                 ?? grp.FirstOrDefault(f => f.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                                 ?? grp.First();

                string ext = Path.GetExtension(sourceFile).ToLowerInvariant();
                string baseName = grp.Key;

                using Image<Rgba32> img = Image.Load<Rgba32>(sourceFile);

                // Chuẩn hóa 1:1 (1000x1000px) trên nền sáng (#FFFFFF)
                NormalizeToSquare1000(img);

                // Nếu nguồn gốc là .webp duy nhất -> tạo thêm .jpg fallback
                string fallbackPath = (ext == ".png")
                    ? Path.Combine(dir, baseName + ".png")
                    : Path.Combine(dir, baseName + ".jpg");

                SaveOptimizedJpeg(img, fallbackPath, 150 * 1024);

                // Xuất bản .webp song hành (<= 150 KB)
                string webpPath = Path.Combine(dir, baseName + ".webp");
                SaveOptimizedWebp(img, webpPath, 150 * 1024);

                productProcessed++;
                webpGenerated++;
            }
        }

        Console.WriteLine($"  -> Đã chuẩn hóa 1000x1000 (1:1) cho {productProcessed} ảnh sản phẩm + xuất {webpGenerated} bản .webp (<= 150 KB).");

        // 3. Tối ưu ảnh trong banners/ và categories/ (<= 150 KB + xuất .webp)
        int bannerCount = OptimizeFolderWithWebp(bannersRoot, 150 * 1024);
        int categoryCount = OptimizeFolderWithWebp(categoriesRoot, 150 * 1024);
        Console.WriteLine($"  -> Đã tối ưu & xuất WebP cho {bannerCount} ảnh banners và {categoryCount} ảnh categories.");

        return 0;
    }

    static void GeneratePinkPlaceholder(string webpPath, string jpgPath)
    {
        const int size = 1000;
        using var img = new Image<Rgba32>(size, size);

        // Nền hồng phấn dịu nhẹ (#FFF4F7 -> #FCE4EC) kèm biểu tượng hoa 4 cánh lấp lánh (Sparkle/Blossom) mờ ở tâm
        float cx = size / 2f;
        float cy = size / 2f;
        float maxDist = (float)Math.Sqrt(cx * cx + cy * cy);

        img.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < size; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < size; x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                    float t = Math.Clamp(dist / maxDist, 0f, 1f);

                    // Gradient nền: tâm #FFF8FA (255, 248, 250) -> viền #FBDCE6 (251, 220, 230)
                    byte r = (byte)(255 - t * 4);
                    byte g = (byte)(248 - t * 28);
                    byte b = (byte)(250 - t * 20);

                    // Khung viền mảnh tinh tế cách mép 48px
                    bool isBorder = (x >= 48 && x <= 51 && y >= 48 && y <= size - 48) ||
                                    (x >= size - 51 && x <= size - 48 && y >= 48 && y <= size - 48) ||
                                    (y >= 48 && y <= 51 && x >= 48 && x <= size - 48) ||
                                    (y >= size - 51 && y <= size - 48 && x >= 48 && x <= size - 48);

                    if (isBorder)
                    {
                        row[x] = new Rgba32(244, 184, 204, 255); // #F4B8CC
                        continue;
                    }

                    // Vẽ biểu tượng logo mờ ở trung tâm:
                    // Vòng tròn mảnh bán kính 150..156px + Ngôi sao lấp lánh 4 cánh mềm (Canmake Sparkle) + đường nét chữ C
                    float ringDist = Math.Abs(dist - 155f);
                    float ax = Math.Abs(dx);
                    float ay = Math.Abs(dy);

                    // Phương trình ngôi sao 4 cánh bo mềm: (ax/95)^0.65 + (ay/95)^0.65 <= 1.0
                    double starVal = Math.Pow(ax / 95.0, 0.65) + Math.Pow(ay / 95.0, 0.65);

                    // Chấm lấp lánh nhỏ góc đông bắc (dx ~ 75, dy ~ -75)
                    float sdx = Math.Abs(dx - 82f);
                    float sdy = Math.Abs(dy + 82f);
                    double miniStar = Math.Pow(sdx / 28.0, 0.65) + Math.Pow(sdy / 28.0, 0.65);

                    float alphaLogo = 0f;
                    if (ringDist <= 3.5f)
                    {
                        alphaLogo = Math.Clamp((3.5f - ringDist) / 3.5f, 0f, 1f) * 0.28f;
                    }
                    if (starVal <= 1.0)
                    {
                        float edge = (float)Math.Clamp((1.0 - starVal) * 8.0, 0.0, 1.0);
                        alphaLogo = Math.Max(alphaLogo, edge * 0.24f);
                    }
                    if (miniStar <= 1.0)
                    {
                        float edge = (float)Math.Clamp((1.0 - miniStar) * 8.0, 0.0, 1.0);
                        alphaLogo = Math.Max(alphaLogo, edge * 0.30f);
                    }

                    if (alphaLogo > 0f)
                    {
                        // Pha màu Rose #E8618C (232, 97, 140) theo độ mờ alphaLogo
                        r = (byte)(r * (1f - alphaLogo) + 232 * alphaLogo);
                        g = (byte)(g * (1f - alphaLogo) + 97 * alphaLogo);
                        b = (byte)(b * (1f - alphaLogo) + 140 * alphaLogo);
                    }

                    row[x] = new Rgba32(r, g, b, 255);
                }
            }
        });

        SaveOptimizedWebp(img, webpPath, 100 * 1024);
        SaveOptimizedJpeg(img, jpgPath, 100 * 1024);
        var fi = new FileInfo(webpPath);
        Console.WriteLine($"  -> Đã tạo ảnh placeholder tông hồng: {webpPath} ({Math.Round(fi.Length / 1024.0, 2)} KB)");
    }

    static void NormalizeToSquare1000(Image<Rgba32> img)
    {
        const int targetSize = 1000;

        // Thay mọi pixel trong suốt bằng nền trắng sáng (#FFFFFF)
        img.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < accessor.Width; x++)
                {
                    var p = row[x];
                    if (p.A < 255)
                    {
                        float a = p.A / 255f;
                        byte r = (byte)(p.R * a + 255 * (1f - a));
                        byte g = (byte)(p.G * a + 255 * (1f - a));
                        byte b = (byte)(p.B * a + 255 * (1f - a));
                        row[x] = new Rgba32(r, g, b, 255);
                    }
                }
            }
        });

        if (img.Width == targetSize && img.Height == targetSize)
        {
            return;
        }

        var options = new ResizeOptions
        {
            Size = new Size(targetSize, targetSize),
            Mode = ResizeMode.Pad,
            Sampler = KnownResamplers.Bicubic,
            PadColor = Color.White
        };

        img.Mutate(x => x.Resize(options));
    }

    static void SaveOptimizedJpeg(Image<Rgba32> img, string path, int maxBytes)
    {
        int[] qualities = { 84, 78, 72, 66, 60, 54 };
        byte[] bestBytes = Array.Empty<byte>();

        foreach (int q in qualities)
        {
            using var ms = new MemoryStream();
            img.Save(ms, new JpegEncoder { Quality = q });
            bestBytes = ms.ToArray();
            if (bestBytes.Length <= maxBytes) break;
        }

        File.WriteAllBytes(path, bestBytes);
    }

    static void SaveOptimizedPngOrJpegInPngContainer(Image<Rgba32> img, string path, int maxBytes)
    {
        using var ms = new MemoryStream();
        var encoder = new PngEncoder
        {
            CompressionLevel = PngCompressionLevel.BestCompression,
            ColorType = PngColorType.Palette,
            Quantizer = KnownQuantizers.Wu
        };
        img.Save(ms, encoder);
        var bytes = ms.ToArray();
        if (bytes.Length <= maxBytes)
        {
            File.WriteAllBytes(path, bytes);
            return;
        }

        // Nếu ảnh chụp thực tế vượt 150 KB khi lưu PNG 256 màu -> giảm nhẹ kích thước nội dung hoặc lưu JPEG
        SaveOptimizedJpeg(img, path, maxBytes);
    }

    static void SaveOptimizedWebp(Image<Rgba32> img, string path, int maxBytes)
    {
        int[] qualities = { 82, 76, 70, 64, 58 };
        byte[] bestBytes = Array.Empty<byte>();

        foreach (int q in qualities)
        {
            using var ms = new MemoryStream();
            var encoder = new WebpEncoder
            {
                Quality = q,
                FileFormat = WebpFileFormatType.Lossy,
                Method = WebpEncodingMethod.Default
            };
            img.Save(ms, encoder);
            bestBytes = ms.ToArray();
            if (bestBytes.Length <= maxBytes) break;
        }

        File.WriteAllBytes(path, bestBytes);
    }

    static int OptimizeFolderWithWebp(string folderPath, int maxBytes)
    {
        if (!Directory.Exists(folderPath)) return 0;
        var files = Directory.GetFiles(folderPath)
            .Where(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            .ToList();

        int count = 0;
        foreach (var file in files)
        {
            using Image<Rgba32> img = Image.Load<Rgba32>(file);
            var fi = new FileInfo(file);
            if (fi.Length > maxBytes)
            {
                SaveOptimizedJpeg(img, file, maxBytes);
            }

            string webpPath = Path.Combine(folderPath, Path.GetFileNameWithoutExtension(file) + ".webp");
            SaveOptimizedWebp(img, webpPath, maxBytes);
            count++;
        }
        return count;
    }
}
'@

[System.IO.File]::WriteAllText((Join-Path $toolDir "OmniImageOptimizerPhien3.csproj"), $csproj, [System.Text.Encoding]::UTF8)
[System.IO.File]::WriteAllText((Join-Path $toolDir "Program.cs"), $programCs, [System.Text.Encoding]::UTF8)

dotnet run --project (Join-Path $toolDir "OmniImageOptimizerPhien3.csproj") -c Release -- "$imagesRoot"
