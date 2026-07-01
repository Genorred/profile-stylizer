using MyApp.Models;
using SkiaSharp;

public class StylizedCardService
{
    public byte[] Generate(User user)
    {
        const int width = 1200;
        const int height = 630;

        var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);

        using var surface = SKSurface.Create(info);
        var canvas = surface.Canvas;

        canvas.Clear(new SKColor(25, 28, 35));

        using var paintTitle = new SKPaint { Color = SKColors.White, IsAntialias = true };

        using var fontTitle = new SKFont { Size = 48 };

        canvas.DrawText(user.Name, 320, 90, SKTextAlign.Left, fontTitle, paintTitle);

        using var paintText = new SKPaint
        {
            Color = new SKColor(210, 210, 210),
            IsAntialias = true,
        };

        using var fontText = new SKFont { Size = 28 };

        canvas.DrawText(user.Email, 320, 150, SKTextAlign.Left, fontText, paintText);
        canvas.DrawText($"Role: {user.Role}", 320, 200, SKTextAlign.Left, fontText, paintText);
        canvas.DrawText(
            $"Telegram: @{user.TelegramUsername}",
            320,
            250,
            SKTextAlign.Left,
            fontText,
            paintText
        );

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);

        return data.ToArray();
    }
}
