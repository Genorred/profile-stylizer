using MyApp.Models;
using SkiaSharp;

public class StylizedCardService
{
    private readonly HttpClient _http = new();

    private const int Width = 1200;
    private const int Height = 630;

    private const int LeftPadding = 120;
    private const int RightPadding = 40;

    private const int NameY = 90;
    private const int RowY = 150;

    private const int ImagesStartY = 380;

    private const int BioMaxLines = 3;
    private const float LineHeight = 34;

    public byte[] Generate(User user)
    {
        using var surface = SKSurface.Create(new SKImageInfo(Width, Height));
        var canvas = surface.Canvas;

        canvas.Clear(new SKColor(54, 23, 32));

        using var paintText = new SKPaint
        {
            Color = new SKColor(220, 220, 220),
            IsAntialias = true
        };

        using var paintBg = new SKPaint
        {
            Color = new SKColor(18, 20, 26),
            IsAntialias = true
        };

        using var fontName = new SKFont { Size = 48 };
        using var fontText = new SKFont { Size = 28 };

        //---------------------------------------------------
        // NAME
        //---------------------------------------------------

        canvas.DrawText(
            user.Name ?? "",
            LeftPadding,
            NameY,
            SKTextAlign.Left,
            fontName,
            paintText
        );

        //---------------------------------------------------
        // USERNAME + BIO INLINE
        //---------------------------------------------------

        float usernameX = LeftPadding;
        float rowY = RowY;

        var username = user.TelegramUsername ?? "";

        canvas.DrawText(
            username,
            usernameX,
            rowY,
            SKTextAlign.Left,
            fontText,
            paintText
        );

        float usernameWidth = fontText.MeasureText(username);
        float bioX = usernameX + usernameWidth + 40;

        var bioLines = Wrap(user.Bio ?? "", fontText, 500);

        if (bioLines.Count > BioMaxLines)
            bioLines = bioLines.GetRange(0, BioMaxLines);

        float bioHeight = bioLines.Count * LineHeight;

        float paddingTop = 16;
        float paddingBottom = 16;
        float paddingX = 20;

        var bgRect = new SKRect(
            bioX - paddingX,
            rowY - fontText.Size - paddingTop,
            Width - RightPadding,
            rowY + bioHeight + paddingBottom
        );

        canvas.DrawRoundRect(bgRect, 16, 16, paintBg);

        for (int i = 0; i < bioLines.Count; i++)
        {
            canvas.DrawText(
                bioLines[i],
                bioX,
                rowY + i * LineHeight,
                SKTextAlign.Left,
                fontText,
                paintText
            );
        }

        //---------------------------------------------------
        // IMAGES
        //---------------------------------------------------

        if (user.TelegramPictures != null && user.TelegramPictures.Count > 0)
        {
            int startX = 320;
            int size = 240;
            int gap = 20;

            for (int i = 0; i < Math.Min(3, user.TelegramPictures.Count); i++)
            {
                try
                {
                    var bytes = _http
                        .GetByteArrayAsync(user.TelegramPictures[i])
                        .GetAwaiter()
                        .GetResult();

                    using var bitmap = SKBitmap.Decode(bytes);
                    if (bitmap == null)
                        continue;

                    var dest = new SKRect(
                        startX + i * (size + gap),
                        ImagesStartY,
                        startX + i * (size + gap) + size,
                        ImagesStartY + size
                    );

                    canvas.DrawBitmap(
                        bitmap,
                        dest,
                        new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear)
                    );
                }
                catch { }
            }
        }

        //---------------------------------------------------
        // 🔥 GRAIN EFFECT (FIXED, NO UNSAFE)
        //---------------------------------------------------

        DrawGrain(canvas);

        canvas.Flush();

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);

        return data.ToArray();
    }

    //---------------------------------------------------
    // GRAIN (SAFE VERSION)
    //---------------------------------------------------

    private static void DrawGrain(SKCanvas canvas)
    {
        int w = Width;
        int h = Height;

        var noise = new SKBitmap(w, h, SKColorType.Gray8, SKAlphaType.Opaque);

        var rand = new Random();

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                byte value = (byte)rand.Next(0, 255);
                noise.SetPixel(x, y, new SKColor(value, value, value));
            }
        }

        using var paint = new SKPaint
        {
            BlendMode = SKBlendMode.SoftLight,
            Color = SKColors.White.WithAlpha(25),
            IsAntialias = false
        };

        canvas.DrawBitmap(
            noise,
            0,
            0,
            new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear),
            paint
        );
    }

    //---------------------------------------------------
    // WRAP
    //---------------------------------------------------

    private static List<string> Wrap(string text, SKFont font, float maxWidth)
    {
        var words = text.Split(' ');
        var lines = new List<string>();
        var current = "";

        foreach (var word in words)
        {
            var test = string.IsNullOrEmpty(current) ? word : $"{current} {word}";

            if (font.MeasureText(test) > maxWidth)
            {
                if (!string.IsNullOrEmpty(current))
                    lines.Add(current);

                current = word;
            }
            else
            {
                current = test;
            }
        }

        if (!string.IsNullOrEmpty(current))
            lines.Add(current);

        return lines;
    }
}
