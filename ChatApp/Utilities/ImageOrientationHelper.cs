using System;
using System.IO;
using SkiaSharp;

public static class ImageHelper
{
    public static byte[] FixImageOrientation(byte[] imageData)
    {
        try
        {
            using (var input = new MemoryStream(imageData))
            {
                using (var codec = SKCodec.Create(input))
                {
                    var orientation = codec.EncodedOrigin;

                    if (orientation == SKEncodedOrigin.TopLeft)
                        return imageData;

                    var degrees = GetRotationDegrees(orientation);
                    using (var original = SKBitmap.Decode(codec))
                    {
                        int newWidth = (degrees % 180 == 0) ? original.Width : original.Height;
                        int newHeight = (degrees % 180 == 0) ? original.Height : original.Width;

                        using (var rotated = new SKBitmap(newWidth, newHeight))
                        {
                            using (var canvas = new SKCanvas(rotated))
                            {
                                canvas.Translate(newWidth / 2, newHeight / 2);
                                canvas.RotateDegrees(degrees);
                                canvas.Translate(-original.Width / 2, -original.Height / 2);
                                canvas.DrawBitmap(original, 0, 0);
                            }

                            using (var output = new MemoryStream())
                            {
                                rotated.Encode(output, SKEncodedImageFormat.Jpeg, 100);
                                return output.ToArray();
                            }
                        }
                    }
                }
            }
        }
        catch
        {
            return imageData;
        }
    }

    private static float GetRotationDegrees(SKEncodedOrigin orientation)
    {
        return orientation switch
        {
            SKEncodedOrigin.RightTop => 90,
            SKEncodedOrigin.BottomRight => 180,
            SKEncodedOrigin.LeftBottom => 270,
            SKEncodedOrigin.LeftTop => 90,
            SKEncodedOrigin.RightBottom => 270,
            _ => 0,
        };
    }
}
