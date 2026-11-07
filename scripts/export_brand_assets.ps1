# ==============================================================================
# CANMAKE BEAUTY - BRAND ASSETS GENERATOR & EXPORTER (PHIÊN 5)
# Phương án được chọn: Phương án 1 - Blooming Heart Monogram (C x M) & 4-Point Sparkle
# Sản xuất trọn bộ SVG thuần vector (chữ đã chuyển thành <path>, có <title>/<desc> a11y)
# và xuất toàn bộ PNG / Multi-size ICO / OpenGraph 1200x630 vào wwwroot/images/branding/
# ==============================================================================

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$ErrorActionPreference = "Stop"

$scriptDir = $PSScriptRoot
if ([string]::IsNullOrEmpty($scriptDir)) {
    $scriptDir = Join-Path (Get-Location).Path "scripts"
}
$workspaceRoot = Split-Path -Parent $scriptDir
$brandingDir = Join-Path $workspaceRoot "Omnichannel.Web\wwwroot\images\branding"
$wwwrootDir  = Join-Path $workspaceRoot "Omnichannel.Web\wwwroot"
$artifactDir = "C:\Users\Admin\.gemini\antigravity\brain\6d105088-d2f0-49ed-88bf-4e334c5932d7"

if (-not (Test-Path $brandingDir)) {
    New-Item -ItemType Directory -Path $brandingDir -Force | Out-Null
}

$toolDir = Join-Path $env:TEMP "CanmakeBrandAssetExporter"
if (-not (Test-Path $toolDir)) {
    New-Item -ItemType Directory -Path $toolDir -Force | Out-Null
}

$csproj = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="SkiaSharp" Version="2.88.8" />
  </ItemGroup>
</Project>
"@

$programCs = @'
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using SkiaSharp;

class Program
{
    // Official Canmake Beauty Color Palette (WCAG 2.1 AA/AAA Verified)
    static readonly SKColor PrimaryRose = SKColor.Parse("#E8618C");
    static readonly SKColor DeepBerry   = SKColor.Parse("#B8336A");
    static readonly SKColor Blush       = SKColor.Parse("#F7C6D4");
    static readonly SKColor PetalCream  = SKColor.Parse("#FFF4F7");
    static readonly SKColor Ink         = SKColor.Parse("#2B1B24");
    static readonly SKColor White       = SKColors.White;

    static void Main(string[] args)
    {
        string brandingDir = args[0];
        string wwwrootDir  = args[1];
        string artifactDir = args.Length > 2 ? args[2] : "";

        Console.WriteLine("=== 1. SINH 5 FILE SVG CHUAN VECTOR (TEXT-TO-PATH + A11Y TITLE/DESC) ===");
        WriteSvgFiles(brandingDir);

        Console.WriteLine("=== 2. XUAT CAC FILE PNG, FAVICON.ICO (16/32/48), MASKABLE & OG-IMAGE ===");
        ExportRasterAssets(brandingDir, wwwrootDir, artifactDir);

        Console.WriteLine("=== HOAN TAT XUAT BO NHAN DIEN THUONG HIEU CANMAKE BEAUTY ===");
    }

    // Converts a text string using a typeface into an SVG <path d="..."/> string
    // so the SVG has 0 external font dependency and renders identically everywhere.
    static string TextToSvgPathData(string text, SKTypeface typeface, float textSize, float startX, float baselineY, float letterSpacing = 0f)
    {
        using var paint = new SKPaint
        {
            IsAntialias = true,
            Typeface = typeface,
            TextSize = textSize
        };

        using var combinedPath = new SKPath();
        float curX = startX;
        foreach (char ch in text)
        {
            string s = ch.ToString();
            if (ch != ' ')
            {
                using var glyphPath = paint.GetTextPath(s, curX, baselineY);
                combinedPath.AddPath(glyphPath);
            }
            curX += paint.MeasureText(s) + letterSpacing;
        }

        return SkPathToSvgData(combinedPath);
    }

    static float MeasureSpacedTextWidth(string text, SKTypeface typeface, float textSize, float letterSpacing = 0f)
    {
        using var paint = new SKPaint { IsAntialias = true, Typeface = typeface, TextSize = textSize };
        float total = 0f;
        for (int i = 0; i < text.Length; i++)
        {
            total += paint.MeasureText(text[i].ToString());
            if (i < text.Length - 1) total += letterSpacing;
        }
        return total;
    }

    static string SkPathToSvgData(SKPath path)
    {
        var sb = new StringBuilder();
        var ic = CultureInfo.InvariantCulture;
        using var iter = path.CreateRawIterator();
        var pts = new SKPoint[4];
        SKPathVerb verb;
        while ((verb = iter.Next(pts)) != SKPathVerb.Done)
        {
            switch (verb)
            {
                case SKPathVerb.Move:
                    sb.AppendFormat(ic, "M{0:0.##} {1:0.##}", pts[0].X, pts[0].Y);
                    break;
                case SKPathVerb.Line:
                    sb.AppendFormat(ic, "L{0:0.##} {1:0.##}", pts[1].X, pts[1].Y);
                    break;
                case SKPathVerb.Quad:
                    sb.AppendFormat(ic, "Q{0:0.##} {1:0.##} {2:0.##} {3:0.##}", pts[1].X, pts[1].Y, pts[2].X, pts[2].Y);
                    break;
                case SKPathVerb.Cubic:
                    sb.AppendFormat(ic, "C{0:0.##} {1:0.##} {2:0.##} {3:0.##} {4:0.##} {5:0.##}", pts[1].X, pts[1].Y, pts[2].X, pts[2].Y, pts[3].X, pts[3].Y);
                    break;
                case SKPathVerb.Conic:
                    sb.AppendFormat(ic, "Q{0:0.##} {1:0.##} {2:0.##} {3:0.##}", pts[1].X, pts[1].Y, pts[2].X, pts[2].Y);
                    break;
                case SKPathVerb.Close:
                    sb.Append("Z");
                    break;
            }
        }
        return sb.ToString();
    }

    static void WriteSvgFiles(string brandingDir)
    {
        using var tfBold = SKTypeface.FromFamilyName("Segoe UI", SKFontStyleWeight.ExtraBold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);
        using var tfLight = SKTypeface.FromFamilyName("Segoe UI", SKFontStyleWeight.SemiBold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);

        // 1. canmake-logo-icon.svg (viewBox 0 0 100 100)
        string iconSvg = @"<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 100 100"" role=""img"" aria-labelledby=""canmakeIconTitle canmakeIconDesc"">
  <title id=""canmakeIconTitle"">CANMAKE BEAUTY Icon</title>
  <desc id=""canmakeIconDesc"">Biểu tượng Blooming Heart Monogram chữ C ôm chữ M tạo hình trái tim và cánh hoa kèm ngôi sao 4 cánh của CANMAKE BEAUTY.</desc>
  <defs>
    <linearGradient id=""cmBgGrad"" x1=""10%"" y1=""10%"" x2=""90%"" y2=""90%"">
      <stop offset=""0%"" stop-color=""#FFF4F7"" />
      <stop offset=""100%"" stop-color=""#F7C6D4"" />
    </linearGradient>
    <linearGradient id=""cmRoseGrad"" x1=""15%"" y1=""15%"" x2=""85%"" y2=""85%"">
      <stop offset=""0%"" stop-color=""#E8618C"" />
      <stop offset=""100%"" stop-color=""#B8336A"" />
    </linearGradient>
  </defs>
  <rect x=""4"" y=""4"" width=""92"" height=""92"" rx=""26"" fill=""url(#cmBgGrad)"" stroke=""#F7C6D4"" stroke-width=""1.5"" />
  <path d=""M74 30 C62 14 34 14 23 32 C12 50 22 76 50 86 C68 80 79 68 81 56"" fill=""none"" stroke=""url(#cmRoseGrad)"" stroke-width=""10.5"" stroke-linecap=""round"" stroke-linejoin=""round"" />
  <path d=""M35 62 L35 42 Q35 34 42 40 L50 49 L58 40 Q65 34 65 42 L65 62"" fill=""none"" stroke=""#B8336A"" stroke-width=""7.5"" stroke-linecap=""round"" stroke-linejoin=""round"" />
  <path d=""M77 15.5 Q79.5 22.5 86.5 25 Q79.5 27.5 77 34.5 Q74.5 27.5 67.5 25 Q74.5 22.5 77 15.5 Z"" fill=""#E8618C"" />
</svg>";
        File.WriteAllText(Path.Combine(brandingDir, "canmake-logo-icon.svg"), iconSvg, Encoding.UTF8);

        // Prepare vector path data for Horizontal Lockup (viewBox 0 0 420 100)
        string horizCanmakePath = TextToSvgPathData("CANMAKE", tfBold, 44f, 114f, 56f, 2.0f);
        string horizBeautyPath  = TextToSvgPathData("BEAUTY", tfLight, 18.5f, 116f, 84f, 9.5f);

        // 2. canmake-logo-horizontal.svg
        string horizontalSvg = $@"<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 420 100"" role=""img"" aria-labelledby=""canmakeHorizTitle canmakeHorizDesc"">
  <title id=""canmakeHorizTitle"">CANMAKE BEAUTY — Logo Ngang Chính Thức</title>
  <desc id=""canmakeHorizDesc"">Logo ngang CANMAKE BEAUTY gồm biểu tượng Blooming Heart Monogram C x M tông hồng và chữ CANMAKE BEAUTY.</desc>
  <defs>
    <linearGradient id=""cmHBgGrad"" x1=""10%"" y1=""10%"" x2=""90%"" y2=""90%"">
      <stop offset=""0%"" stop-color=""#FFF4F7"" />
      <stop offset=""100%"" stop-color=""#F7C6D4"" />
    </linearGradient>
    <linearGradient id=""cmHRoseGrad"" x1=""15%"" y1=""15%"" x2=""85%"" y2=""85%"">
      <stop offset=""0%"" stop-color=""#E8618C"" />
      <stop offset=""100%"" stop-color=""#B8336A"" />
    </linearGradient>
  </defs>
  <g id=""canmake-emblem"">
    <rect x=""4"" y=""4"" width=""92"" height=""92"" rx=""26"" fill=""url(#cmHBgGrad)"" stroke=""#F7C6D4"" stroke-width=""1.5"" />
    <path d=""M74 30 C62 14 34 14 23 32 C12 50 22 76 50 86 C68 80 79 68 81 56"" fill=""none"" stroke=""url(#cmHRoseGrad)"" stroke-width=""10.5"" stroke-linecap=""round"" stroke-linejoin=""round"" />
    <path d=""M35 62 L35 42 Q35 34 42 40 L50 49 L58 40 Q65 34 65 42 L65 62"" fill=""none"" stroke=""#B8336A"" stroke-width=""7.5"" stroke-linecap=""round"" stroke-linejoin=""round"" />
    <path d=""M77 15.5 Q79.5 22.5 86.5 25 Q79.5 27.5 77 34.5 Q74.5 27.5 67.5 25 Q74.5 22.5 77 15.5 Z"" fill=""#E8618C"" />
  </g>
  <g id=""canmake-wordmark"">
    <path d=""{horizCanmakePath}"" fill=""#2B1B24"" />
    <path d=""{horizBeautyPath}"" fill=""#B8336A"" />
  </g>
</svg>";
        File.WriteAllText(Path.Combine(brandingDir, "canmake-logo-horizontal.svg"), horizontalSvg, Encoding.UTF8);

        // 3. canmake-logo-white.svg (Dark Mode / Footer variant, viewBox 0 0 420 100)
        string whiteSvg = $@"<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 420 100"" role=""img"" aria-labelledby=""canmakeWhiteTitle canmakeWhiteDesc"">
  <title id=""canmakeWhiteTitle"">CANMAKE BEAUTY — Logo Phiên Bản Nền Tối (Dark Mode)</title>
  <desc id=""canmakeWhiteDesc"">Logo CANMAKE BEAUTY phiên bản sáng màu dùng trên nền tối (Footer, Admin Sidebar, Dark Mode).</desc>
  <defs>
    <linearGradient id=""cmWBgGrad"" x1=""10%"" y1=""10%"" x2=""90%"" y2=""90%"">
      <stop offset=""0%"" stop-color=""#E8618C"" />
      <stop offset=""100%"" stop-color=""#B8336A"" />
    </linearGradient>
  </defs>
  <g id=""canmake-emblem-dark"">
    <rect x=""4"" y=""4"" width=""92"" height=""92"" rx=""26"" fill=""url(#cmWBgGrad)"" />
    <path d=""M74 30 C62 14 34 14 23 32 C12 50 22 76 50 86 C68 80 79 68 81 56"" fill=""none"" stroke=""#FFFFFF"" stroke-width=""10.5"" stroke-linecap=""round"" stroke-linejoin=""round"" />
    <path d=""M35 62 L35 42 Q35 34 42 40 L50 49 L58 40 Q65 34 65 42 L65 62"" fill=""none"" stroke=""#F7C6D4"" stroke-width=""7.5"" stroke-linecap=""round"" stroke-linejoin=""round"" />
    <path d=""M77 15.5 Q79.5 22.5 86.5 25 Q79.5 27.5 77 34.5 Q74.5 27.5 67.5 25 Q74.5 22.5 77 15.5 Z"" fill=""#FFFFFF"" />
  </g>
  <g id=""canmake-wordmark-white"">
    <path d=""{horizCanmakePath}"" fill=""#FFFFFF"" />
    <path d=""{horizBeautyPath}"" fill=""#F7C6D4"" />
  </g>
</svg>";
        File.WriteAllText(Path.Combine(brandingDir, "canmake-logo-white.svg"), whiteSvg, Encoding.UTF8);

        // 4. canmake-logo-mono.svg (1-color monochrome #2B1B24 for POS 80mm thermal receipts & shipping labels, viewBox 0 0 420 100)
        string monoSvg = $@"<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 420 100"" role=""img"" aria-labelledby=""canmakeMonoTitle canmakeMonoDesc"">
  <title id=""canmakeMonoTitle"">CANMAKE BEAUTY — Logo Đơn Sắc In Nhiệt (Monochrome)</title>
  <desc id=""canmakeMonoDesc"">Logo CANMAKE BEAUTY đơn sắc 1 màu độ tương phản cao chuyên dụng cho máy in nhiệt hóa đơn POS 80mm và nhãn vận đơn.</desc>
  <g id=""canmake-emblem-mono"">
    <rect x=""5"" y=""5"" width=""90"" height=""90"" rx=""25"" fill=""none"" stroke=""#2B1B24"" stroke-width=""4.5"" />
    <path d=""M74 30 C62 14 34 14 23 32 C12 50 22 76 50 86 C68 80 79 68 81 56"" fill=""none"" stroke=""#2B1B24"" stroke-width=""10.5"" stroke-linecap=""round"" stroke-linejoin=""round"" />
    <path d=""M35 62 L35 42 Q35 34 42 40 L50 49 L58 40 Q65 34 65 42 L65 62"" fill=""none"" stroke=""#2B1B24"" stroke-width=""7.5"" stroke-linecap=""round"" stroke-linejoin=""round"" />
    <path d=""M77 15.5 Q79.5 22.5 86.5 25 Q79.5 27.5 77 34.5 Q74.5 27.5 67.5 25 Q74.5 22.5 77 15.5 Z"" fill=""#2B1B24"" />
  </g>
  <g id=""canmake-wordmark-mono"">
    <path d=""{horizCanmakePath}"" fill=""#2B1B24"" />
    <path d=""{horizBeautyPath}"" fill=""#2B1B24"" />
  </g>
</svg>";
        File.WriteAllText(Path.Combine(brandingDir, "canmake-logo-mono.svg"), monoSvg, Encoding.UTF8);

        // 5. canmake-logo-stacked.svg (Icon top, wordmark bottom centered, viewBox 0 0 280 220)
        float wCanmake = MeasureSpacedTextWidth("CANMAKE", tfBold, 38f, 2.0f);
        float wBeauty  = MeasureSpacedTextWidth("BEAUTY", tfLight, 16f, 8.5f);
        string stackedCanmakePath = TextToSvgPathData("CANMAKE", tfBold, 38f, (280f - wCanmake) / 2f, 168f, 2.0f);
        string stackedBeautyPath  = TextToSvgPathData("BEAUTY", tfLight, 16f, (280f - wBeauty) / 2f, 200f, 8.5f);

        string stackedSvg = $@"<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 280 220"" role=""img"" aria-labelledby=""canmakeStackedTitle canmakeStackedDesc"">
  <title id=""canmakeStackedTitle"">CANMAKE BEAUTY — Logo Dọc (Stacked Lockup)</title>
  <desc id=""canmakeStackedDesc"">Logo bố cục dọc của CANMAKE BEAUTY với biểu tượng Blooming Heart Monogram phía trên và chữ CANMAKE BEAUTY căn giữa phía dưới.</desc>
  <defs>
    <linearGradient id=""cmSBgGrad"" x1=""10%"" y1=""10%"" x2=""90%"" y2=""90%"">
      <stop offset=""0%"" stop-color=""#FFF4F7"" />
      <stop offset=""100%"" stop-color=""#F7C6D4"" />
    </linearGradient>
    <linearGradient id=""cmSRoseGrad"" x1=""15%"" y1=""15%"" x2=""85%"" y2=""85%"">
      <stop offset=""0%"" stop-color=""#E8618C"" />
      <stop offset=""100%"" stop-color=""#B8336A"" />
    </linearGradient>
  </defs>
  <g transform=""translate(82, 10) scale(1.16)"">
    <rect x=""4"" y=""4"" width=""92"" height=""92"" rx=""26"" fill=""url(#cmSBgGrad)"" stroke=""#F7C6D4"" stroke-width=""1.5"" />
    <path d=""M74 30 C62 14 34 14 23 32 C12 50 22 76 50 86 C68 80 79 68 81 56"" fill=""none"" stroke=""url(#cmSRoseGrad)"" stroke-width=""10.5"" stroke-linecap=""round"" stroke-linejoin=""round"" />
    <path d=""M35 62 L35 42 Q35 34 42 40 L50 49 L58 40 Q65 34 65 42 L65 62"" fill=""none"" stroke=""#B8336A"" stroke-width=""7.5"" stroke-linecap=""round"" stroke-linejoin=""round"" />
    <path d=""M77 15.5 Q79.5 22.5 86.5 25 Q79.5 27.5 77 34.5 Q74.5 27.5 67.5 25 Q74.5 22.5 77 15.5 Z"" fill=""#E8618C"" />
  </g>
  <path d=""{stackedCanmakePath}"" fill=""#2B1B24"" />
  <path d=""{stackedBeautyPath}"" fill=""#B8336A"" />
</svg>";
        File.WriteAllText(Path.Combine(brandingDir, "canmake-logo-stacked.svg"), stackedSvg, Encoding.UTF8);
    }

    static void DrawBloomingHeartEmblem(SKCanvas canvas, float x, float y, float size, bool isDarkBg = false, bool isMono = false, bool circularClip = false)
    {
        canvas.Save();
        canvas.Translate(x, y);
        float s = size / 100f;
        canvas.Scale(s, s);

        using var bgPaint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
            Shader = isMono ? null : SKShader.CreateLinearGradient(
                new SKPoint(10, 10), new SKPoint(90, 90),
                isDarkBg ? new[] { PrimaryRose, DeepBerry } : new[] { PetalCream, Blush },
                null, SKShaderTileMode.Clamp),
            Color = isMono ? White : PetalCream
        };

        if (circularClip)
        {
            canvas.DrawCircle(50, 50, 48, bgPaint);
        }
        else
        {
            canvas.DrawRoundRect(new SKRoundRect(new SKRect(4, 4, 96, 96), 26, 26), bgPaint);
        }

        if (isMono)
        {
            using var borderP = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 4.5f, Color = Ink };
            canvas.DrawRoundRect(new SKRoundRect(new SKRect(5, 5, 95, 95), 25, 25), borderP);
        }

        // Outer C petal-heart curve
        using var cPath = new SKPath();
        cPath.MoveTo(74, 30);
        cPath.CubicTo(62, 14, 34, 14, 23, 32);
        cPath.CubicTo(12, 50, 22, 76, 50, 86);
        cPath.CubicTo(68, 80, 79, 68, 81, 56);

        using var cPaint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 10.5f,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round,
            Shader = (isMono || isDarkBg) ? null : SKShader.CreateLinearGradient(
                new SKPoint(20, 20), new SKPoint(80, 85),
                new[] { PrimaryRose, DeepBerry }, null, SKShaderTileMode.Clamp),
            Color = isMono ? Ink : (isDarkBg ? White : PrimaryRose)
        };
        canvas.DrawPath(cPath, cPaint);

        // Inner M heart-petals nestled inside the C
        using var mPath = new SKPath();
        mPath.MoveTo(35, 62);
        mPath.LineTo(35, 42);
        mPath.QuadTo(35, 34, 42, 40);
        mPath.LineTo(50, 49);
        mPath.LineTo(58, 40);
        mPath.QuadTo(65, 34, 65, 42);
        mPath.LineTo(65, 62);

        using var mPaint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 7.5f,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round,
            Color = isMono ? Ink : (isDarkBg ? Blush : DeepBerry)
        };
        canvas.DrawPath(mPath, mPaint);

        // 4-point sparkle at top-right opening of C
        using var spPath = new SKPath();
        float cx = 77f, cy = 25f, r = 9.5f, inner = r * 0.26f;
        spPath.MoveTo(cx, cy - r);
        spPath.QuadTo(cx + inner, cy - inner, cx + r, cy);
        spPath.QuadTo(cx + inner, cy + inner, cx, cy + r);
        spPath.QuadTo(cx - inner, cy + inner, cx - r, cy);
        spPath.QuadTo(cx - inner, cy - inner, cx, cy - r);
        spPath.Close();
        using var spPaint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
            Color = isMono ? Ink : (isDarkBg ? White : PrimaryRose)
        };
        canvas.DrawPath(spPath, spPaint);

        canvas.Restore();
    }

    static byte[] RenderIconPngBytes(int size, bool circular = false, bool maskableSafeZone = false)
    {
        using var surface = SKSurface.Create(new SKImageInfo(size, size, SKColorType.Rgba8888, SKAlphaType.Premul));
        var c = surface.Canvas;
        c.Clear(SKColors.Transparent);

        if (maskableSafeZone)
        {
            // Full bleed gradient background with emblem inside the central 76% (< 80% safe zone)
            using var bgPaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                Shader = SKShader.CreateLinearGradient(
                    new SKPoint(0, 0), new SKPoint(size, size),
                    new[] { PetalCream, Blush }, null, SKShaderTileMode.Clamp)
            };
            c.DrawRect(0, 0, size, size, bgPaint);
            float innerSize = size * 0.74f;
            float offset = (size - innerSize) / 2f;
            DrawBloomingHeartEmblem(c, offset, offset, innerSize, false, false, false);
        }
        else
        {
            DrawBloomingHeartEmblem(c, 0, 0, size, false, false, circular);
        }

        using var img = surface.Snapshot();
        using var data = img.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    // Writes a valid multi-resolution Windows .ico file containing 16x16, 32x32, and 48x48 PNG frames
    static void WriteMultiSizeIco(string icoPath, int[] sizes)
    {
        var frames = new List<byte[]>();
        foreach (int sz in sizes)
        {
            frames.Add(RenderIconPngBytes(sz));
        }

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        // ICONDIR header (6 bytes)
        bw.Write((ushort)0);             // Reserved
        bw.Write((ushort)1);             // Type: 1 = ICO
        bw.Write((ushort)frames.Count);  // Count

        int dataOffset = 6 + frames.Count * 16;
        for (int i = 0; i < frames.Count; i++)
        {
            int sz = sizes[i];
            byte[] png = frames[i];
            bw.Write((byte)(sz >= 256 ? 0 : sz)); // Width
            bw.Write((byte)(sz >= 256 ? 0 : sz)); // Height
            bw.Write((byte)0);                    // ColorCount
            bw.Write((byte)0);                    // Reserved
            bw.Write((ushort)1);                  // Planes
            bw.Write((ushort)32);                 // BitCount
            bw.Write((uint)png.Length);           // BytesInRes
            bw.Write((uint)dataOffset);           // ImageOffset
            dataOffset += png.Length;
        }

        for (int i = 0; i < frames.Count; i++)
        {
            bw.Write(frames[i]);
        }

        File.WriteAllBytes(icoPath, ms.ToArray());
    }

    static void ExportOgImage1200x630(string outPath)
    {
        int w = 1200, h = 630;
        using var surface = SKSurface.Create(new SKImageInfo(w, h, SKColorType.Rgba8888, SKAlphaType.Premul));
        var c = surface.Canvas;

        // Soft Rose-to-Cream diagonal luxury background
        using var bgPaint = new SKPaint
        {
            IsAntialias = true,
            Shader = SKShader.CreateLinearGradient(
                new SKPoint(0, 0), new SKPoint(w, h),
                new[] { PetalCream, SKColor.Parse("#FDE4EC"), Blush },
                new[] { 0f, 0.55f, 1f }, SKShaderTileMode.Clamp)
        };
        c.DrawRect(0, 0, w, h, bgPaint);

        // Decorative soft glowing circles
        using var glow1 = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(232, 97, 140, 22),
            Style = SKPaintStyle.Fill
        };
        c.DrawCircle(1060, 120, 240, glow1);
        c.DrawCircle(140, 540, 210, glow1);

        // White luxury inner frame card
        var cardRect = new SKRoundRect(new SKRect(64, 56, w - 64, h - 56), 36, 36);
        using var cardFill = new SKPaint { IsAntialias = true, Color = new SKColor(255, 255, 255, 235) };
        using var cardStroke = new SKPaint { IsAntialias = true, Color = PrimaryRose, Style = SKPaintStyle.Stroke, StrokeWidth = 2.5f };
        c.DrawRoundRect(cardRect, cardFill);
        c.DrawRoundRect(cardRect, cardStroke);

        // Draw Blooming Heart Emblem on left-center
        DrawBloomingHeartEmblem(c, 130, 165, 260, false, false, false);

        using var tfBold = SKTypeface.FromFamilyName("Segoe UI", SKFontStyleWeight.ExtraBold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);
        using var tfSemi = SKTypeface.FromFamilyName("Segoe UI", SKFontStyleWeight.SemiBold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);
        using var tfReg  = SKTypeface.FromFamilyName("Segoe UI", SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);

        // Badge pill at top right
        var pillRect = new SKRoundRect(new SKRect(435, 160, 895, 204), 22, 22);
        using var pillFill = new SKPaint { IsAntialias = true, Color = PetalCream };
        using var pillBorder = new SKPaint { IsAntialias = true, Color = Blush, Style = SKPaintStyle.Stroke, StrokeWidth = 1.5f };
        c.DrawRoundRect(pillRect, pillFill);
        c.DrawRoundRect(pillRect, pillBorder);

        using var pPillTxt = new SKPaint { IsAntialias = true, Typeface = tfBold, TextSize = 15.5f, Color = DeepBerry };
        c.DrawText("HỆ THỐNG MỸ PHẨM CHÍNH HÃNG ĐA KÊNH O2O", 472, 188, pPillTxt);
        using (var spP = new SKPaint { IsAntialias = true, Color = PrimaryRose, Style = SKPaintStyle.Fill })
        {
            c.DrawCircle(454, 182, 4.5f, spP);
            c.DrawCircle(874, 182, 4.5f, spP);
        }

        // Brand Name "CANMAKE" + "B E A U T Y"
        using var pBrand = new SKPaint { IsAntialias = true, Typeface = tfBold, TextSize = 86, Color = Ink };
        c.DrawText("CANMAKE", 432, 300, pBrand);

        using var pSubBrand = new SKPaint { IsAntialias = true, Typeface = tfSemi, TextSize = 30, Color = DeepBerry };
        c.DrawText("B E A U T Y", 438, 346, pSubBrand);

        // Slogan & USPs
        using var pSlogan = new SKPaint { IsAntialias = true, Typeface = tfBold, TextSize = 23, Color = PrimaryRose };
        c.DrawText("Bạn Có Thể Tự Tay Kiến Tạo Vẻ Đẹp Của Riêng Mình", 435, 405, pSlogan);

        using var pDesc = new SKPaint { IsAntialias = true, Typeface = tfReg, TextSize = 18.5f, Color = SKColor.Parse("#4A3540") };
        c.DrawText("• Cam kết 100% chính hãng — Minh bạch hạn dùng theo lô FEFO", 435, 448, pDesc);
        c.DrawText("• Trợ lý Da liễu AI & Thử màu son ảo (Virtual Try-On) + Click & Collect 1H", 435, 480, pDesc);

        using var img = surface.Snapshot();
        using var data = img.Encode(SKEncodedImageFormat.Png, 95);
        File.WriteAllBytes(outPath, data.ToArray());
    }

    static void ExportLegacyCompatLogos(string brandingDir)
    {
        // logo-circle.png (512x512 circular icon)
        File.WriteAllBytes(Path.Combine(brandingDir, "logo-circle.png"), RenderIconPngBytes(512, true, false));

        // logo-icon.jpg (512x512 square icon on PetalCream)
        using (var surface = SKSurface.Create(new SKImageInfo(512, 512, SKColorType.Rgba8888, SKAlphaType.Premul)))
        {
            var c = surface.Canvas;
            c.Clear(PetalCream);
            DrawBloomingHeartEmblem(c, 26, 26, 460, false, false, false);
            using var img = surface.Snapshot();
            using var data = img.Encode(SKEncodedImageFormat.Jpeg, 92);
            File.WriteAllBytes(Path.Combine(brandingDir, "logo-icon.jpg"), data.ToArray());
        }

        // logo-full.jpg (840x220 horizontal logo on White)
        using (var surface = SKSurface.Create(new SKImageInfo(840, 220, SKColorType.Rgba8888, SKAlphaType.Premul)))
        {
            var c = surface.Canvas;
            c.Clear(White);
            DrawBloomingHeartEmblem(c, 28, 20, 180, false, false, false);
            using var tfBold = SKTypeface.FromFamilyName("Segoe UI", SKFontStyleWeight.ExtraBold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);
            using var tfSemi = SKTypeface.FromFamilyName("Segoe UI", SKFontStyleWeight.SemiBold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);
            using var pMain = new SKPaint { IsAntialias = true, Typeface = tfBold, TextSize = 84, Color = Ink };
            using var pSub  = new SKPaint { IsAntialias = true, Typeface = tfSemi, TextSize = 34, Color = DeepBerry };
            c.DrawText("CANMAKE", 236, 122, pMain);
            c.DrawText("B E A U T Y", 240, 176, pSub);
            using var img = surface.Snapshot();
            using var data = img.Encode(SKEncodedImageFormat.Jpeg, 92);
            File.WriteAllBytes(Path.Combine(brandingDir, "logo-full.jpg"), data.ToArray());
        }
    }

    static void ExportFinalShowcaseBoard(string outPath)
    {
        int w = 1280, h = 760;
        using var surface = SKSurface.Create(new SKImageInfo(w, h, SKColorType.Rgba8888, SKAlphaType.Premul));
        var c = surface.Canvas;
        c.Clear(PetalCream);

        using var tfBold = SKTypeface.FromFamilyName("Segoe UI", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);
        using var tfExtra = SKTypeface.FromFamilyName("Segoe UI", SKFontStyleWeight.ExtraBold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);
        using var tfSemi = SKTypeface.FromFamilyName("Segoe UI", SKFontStyleWeight.SemiBold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);
        using var tfReg = SKTypeface.FromFamilyName("Segoe UI", SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);

        using var headerPaint = new SKPaint { IsAntialias = true, Color = Ink };
        c.DrawRect(0, 0, w, 76, headerPaint);
        using var hTitle = new SKPaint { IsAntialias = true, Typeface = tfBold, TextSize = 22, Color = White };
        c.DrawText("BO NHAN DIEN LOGO CANMAKE BEAUTY CHINH THUC (5 BIEN THE SVG + FAVICON + OG)", 32, 47, hTitle);

        using var cardWhite = new SKPaint { IsAntialias = true, Color = White };
        using var cardDark  = new SKPaint { IsAntialias = true, Color = Ink };
        using var cardBorder = new SKPaint { IsAntialias = true, Color = Blush, Style = SKPaintStyle.Stroke, StrokeWidth = 2 };
        using var labelP = new SKPaint { IsAntialias = true, Typeface = tfBold, TextSize = 13, Color = DeepBerry };
        using var labelDarkP = new SKPaint { IsAntialias = true, Typeface = tfBold, TextSize = 13, Color = Blush };

        // 1. canmake-logo-horizontal.svg preview
        var r1 = new SKRoundRect(new SKRect(32, 100, 624, 300), 18, 18);
        c.DrawRoundRect(r1, cardWhite);
        c.DrawRoundRect(r1, cardBorder);
        c.DrawText("1. canmake-logo-horizontal.svg (Header Storefront)", 52, 130, labelP);
        DrawBloomingHeartEmblem(c, 64, 152, 118, false, false);
        using (var pM = new SKPaint { IsAntialias = true, Typeface = tfExtra, TextSize = 54, Color = Ink })
        using (var pS = new SKPaint { IsAntialias = true, Typeface = tfSemi, TextSize = 23, Color = DeepBerry })
        {
            c.DrawText("CANMAKE", 204, 218, pM);
            c.DrawText("B E A U T Y", 208, 254, pS);
        }

        // 2. canmake-logo-white.svg preview
        var r2 = new SKRoundRect(new SKRect(32, 320, 624, 520), 18, 18);
        c.DrawRoundRect(r2, cardDark);
        c.DrawText("2. canmake-logo-white.svg (Dark Mode / Footer / Admin Sidebar)", 52, 350, labelDarkP);
        DrawBloomingHeartEmblem(c, 64, 372, 118, true, false);
        using (var pM = new SKPaint { IsAntialias = true, Typeface = tfExtra, TextSize = 54, Color = White })
        using (var pS = new SKPaint { IsAntialias = true, Typeface = tfSemi, TextSize = 23, Color = Blush })
        {
            c.DrawText("CANMAKE", 204, 438, pM);
            c.DrawText("B E A U T Y", 208, 474, pS);
        }

        // 3. canmake-logo-mono.svg preview
        var r3 = new SKRoundRect(new SKRect(32, 540, 624, 730), 18, 18);
        c.DrawRoundRect(r3, cardWhite);
        c.DrawRoundRect(r3, cardBorder);
        c.DrawText("3. canmake-logo-mono.svg (In nhiet POS 80mm & Nhan van chuyen)", 52, 570, labelP);
        DrawBloomingHeartEmblem(c, 64, 590, 112, false, true);
        using (var pM = new SKPaint { IsAntialias = true, Typeface = tfExtra, TextSize = 52, Color = Ink })
        using (var pS = new SKPaint { IsAntialias = true, Typeface = tfSemi, TextSize = 22, Color = Ink })
        {
            c.DrawText("CANMAKE", 200, 654, pM);
            c.DrawText("B E A U T Y", 204, 688, pS);
        }

        // 4. canmake-logo-stacked.svg preview
        var r4 = new SKRoundRect(new SKRect(652, 100, 1248, 425), 18, 18);
        c.DrawRoundRect(r4, cardWhite);
        c.DrawRoundRect(r4, cardBorder);
        c.DrawText("4. canmake-logo-stacked.svg (Trang Dang nhap / Dang ky / Hoa don)", 674, 130, labelP);
        DrawBloomingHeartEmblem(c, 880, 152, 140, false, false);
        using (var pM = new SKPaint { IsAntialias = true, Typeface = tfExtra, TextSize = 48, Color = Ink, TextAlign = SKTextAlign.Center })
        using (var pS = new SKPaint { IsAntialias = true, Typeface = tfSemi, TextSize = 21, Color = DeepBerry, TextAlign = SKTextAlign.Center })
        {
            c.DrawText("CANMAKE", 950, 348, pM);
            c.DrawText("B E A U T Y", 950, 384, pS);
        }

        // 5. canmake-logo-icon.svg + Favicons + Maskable preview
        var r5 = new SKRoundRect(new SKRect(652, 445, 1248, 730), 18, 18);
        c.DrawRoundRect(r5, cardWhite);
        c.DrawRoundRect(r5, cardBorder);
        c.DrawText("5. canmake-logo-icon.svg + Favicon (512 / 192 / 180 / 32 / 16) + Maskable", 674, 475, labelP);

        DrawBloomingHeartEmblem(c, 684, 510, 110, false, false);
        // Maskable 80% safe-zone preview
        using (var mBg = new SKPaint { IsAntialias = true, Color = Blush })
        {
            c.DrawRoundRect(new SKRoundRect(new SKRect(824, 510, 934, 620), 24, 24), mBg);
            DrawBloomingHeartEmblem(c, 839, 525, 80, false, false);
            using var safeCircle = new SKPaint { IsAntialias = true, Color = DeepBerry, Style = SKPaintStyle.Stroke, StrokeWidth = 1.5f, PathEffect = SKPathEffect.CreateDash(new[] { 4f, 4f }, 0) };
            c.DrawCircle(879, 565, 44, safeCircle);
        }
        DrawBloomingHeartEmblem(c, 964, 530, 72, false, false);
        DrawBloomingHeartEmblem(c, 1064, 552, 48, false, false);
        DrawBloomingHeartEmblem(c, 1136, 568, 32, false, false);
        DrawBloomingHeartEmblem(c, 1190, 584, 16, false, false);

        using var capP = new SKPaint { IsAntialias = true, Typeface = tfReg, TextSize = 12.5f, Color = Ink, TextAlign = SKTextAlign.Center };
        c.DrawText("Icon 512px", 739, 648, capP);
        c.DrawText("Maskable (80%)", 879, 648, capP);
        c.DrawText("Apple 180px", 1000, 648, capP);
        c.DrawText("48px", 1088, 648, capP);
        c.DrawText("32px", 1152, 648, capP);
        c.DrawText("16px", 1198, 648, capP);

        using var img = surface.Snapshot();
        using var data = img.Encode(SKEncodedImageFormat.Png, 100);
        File.WriteAllBytes(outPath, data.ToArray());
    }

    static void ExportRasterAssets(string brandingDir, string wwwrootDir, string artifactDir)
    {
        File.WriteAllBytes(Path.Combine(brandingDir, "favicon-32x32.png"), RenderIconPngBytes(32));
        File.WriteAllBytes(Path.Combine(brandingDir, "favicon-192x192.png"), RenderIconPngBytes(192));
        File.WriteAllBytes(Path.Combine(brandingDir, "favicon-512x512.png"), RenderIconPngBytes(512));
        File.WriteAllBytes(Path.Combine(brandingDir, "apple-touch-icon.png"), RenderIconPngBytes(180));
        File.WriteAllBytes(Path.Combine(brandingDir, "maskable-512.png"), RenderIconPngBytes(512, false, true));

        // Multi-size favicon.ico (16x16, 32x32, 48x48) in both brandingDir and wwwrootDir
        string brandingIco = Path.Combine(brandingDir, "favicon.ico");
        string rootIco     = Path.Combine(wwwrootDir, "favicon.ico");
        WriteMultiSizeIco(brandingIco, new[] { 16, 32, 48 });
        File.Copy(brandingIco, rootIco, true);

        // og-image-1200x630.png
        string ogPath = Path.Combine(brandingDir, "og-image-1200x630.png");
        ExportOgImage1200x630(ogPath);

        // Also update legacy filenames (logo-circle.png, logo-full.jpg, logo-icon.jpg)
        ExportLegacyCompatLogos(brandingDir);

        if (!string.IsNullOrEmpty(artifactDir) && Directory.Exists(artifactDir))
        {
            File.Copy(ogPath, Path.Combine(artifactDir, "og-image-1200x630.png"), true);
            ExportFinalShowcaseBoard(Path.Combine(artifactDir, "canmake_final_brand_assets_showcase.png"));
        }
    }
}
'@

[System.IO.File]::WriteAllText((Join-Path $toolDir "CanmakeBrandAssetExporter.csproj"), $csproj, [System.Text.Encoding]::UTF8)
[System.IO.File]::WriteAllText((Join-Path $toolDir "Program.cs"), $programCs, [System.Text.Encoding]::UTF8)

dotnet run --project (Join-Path $toolDir "CanmakeBrandAssetExporter.csproj") -c Release -- "$brandingDir" "$wwwrootDir" "$artifactDir"

Write-Host "`n=== DANH SACH FILE TRONG wwwroot/images/branding/ ===" -ForegroundColor Cyan
Get-ChildItem -Path $brandingDir -File | Sort-Object Name | Select-Object Name, Length, LastWriteTime | Format-Table -AutoSize
