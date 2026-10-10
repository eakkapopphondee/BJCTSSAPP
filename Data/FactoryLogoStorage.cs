using System.ComponentModel.DataAnnotations;
using SkiaSharp;

namespace thaibev.Data;

public sealed class FactoryLogoStorage(IWebHostEnvironment environment)
{
    public async Task<string?> SaveAsync(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null) return null;
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var expectedFormat = extension switch
        {
            ".jpg" or ".jpeg" => SKEncodedImageFormat.Jpeg,
            ".png" => SKEncodedImageFormat.Png,
            ".webp" => SKEncodedImageFormat.Webp,
            _ => (SKEncodedImageFormat?)null
        };
        if (expectedFormat is null || !file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            throw new ValidationException("Logo รองรับเฉพาะไฟล์ .jpg, .jpeg, .png และ .webp");
        if (file.Length <= 0 || file.Length > 5 * 1024 * 1024)
            throw new ValidationException("Logo ต้องเป็นไฟล์ภาพที่ไม่ว่างและมีขนาดไม่เกิน 5 MB");

        using var input = file.OpenReadStream();
        using var codec = SKCodec.Create(input);
        if (codec is null || codec.EncodedFormat != expectedFormat)
            throw new ValidationException("ไฟล์ Logo ไม่ใช่รูปภาพที่ถูกต้องหรือนามสกุลไม่ตรงกับชนิดภาพ");
        if (codec.Info.Width is < 1 or > 250 || codec.Info.Height is < 1 or > 250)
            throw new ValidationException("Logo ต้องมีความกว้างไม่เกิน 250 px และความสูงไม่เกิน 250 px");

        using var bitmap = new SKBitmap(new SKImageInfo(codec.Info.Width, codec.Info.Height));
        if (codec.GetPixels(bitmap.Info, bitmap.GetPixels()) != SKCodecResult.Success)
            throw new ValidationException("ไฟล์ภาพ Logo เสียหายหรือไม่สมบูรณ์");
        // Re-encode decoded pixels so only image content is published.
        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
        var directory = Path.Combine(environment.WebRootPath, "uploads", "factories");
        Directory.CreateDirectory(directory);
        var name = $"{Guid.NewGuid():N}.png";
        var fullPath = Path.Combine(directory, name);
        try
        {
            await File.WriteAllBytesAsync(fullPath, encoded.ToArray(), cancellationToken);
        }
        catch
        {
            File.Delete(fullPath);
            throw;
        }
        return $"uploads/factories/{name}";
    }

    public void DeleteNewUpload(string? path)
    {
        if (path is not null)
            File.Delete(Path.Combine(environment.WebRootPath, "uploads", "factories", Path.GetFileName(path)));
    }
}
