using System;
using System.IO;
using System.Threading.Tasks;
#if ANDROID
using Android.Graphics;
#endif

#if IOS || MACCATALYST
using UIKit;
using Foundation;
using CoreGraphics;
#endif

#if WINDOWS
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
#endif

namespace ChatApp.Utilities
{
    public static class CompressExtensions
    {
        /// <summary>
        /// Compress an image (byte[]) by resizing and re-encoding to JPEG.
        /// Cross-platform conditional implementations for MAUI.
        /// Defaults aim for very strong compression while keeping reasonable resolution.
        /// </summary>
        /// <param name="input">Input image bytes (any common image format)</param>
        /// <param name="maxWidth">Max width after resize (preserve aspect ratio). Default 1024.</param>
        /// <param name="maxHeight">Max height after resize. Default 1024.</param>
        /// <param name="jpegQuality">JPEG quality 1-100 (higher = better quality & bigger size). Default 30.</param>
        /// <returns>Compressed image bytes (JPEG)</returns>
        public static Task<byte[]> CompressImageAsync(
            byte[] input,
            int maxWidth = 1024,
            int maxHeight = 1024,
            int jpegQuality = 30
        )
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));
            if (maxWidth <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxWidth));
            if (maxHeight <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxHeight));
            if (jpegQuality < 1 || jpegQuality > 100)
                throw new ArgumentOutOfRangeException(nameof(jpegQuality));

#if ANDROID
            return Task.FromResult(CompressAndroid(input, maxWidth, maxHeight, jpegQuality));
#elif IOS || MACCATALYST
            return Task.FromResult(CompressIos(input, maxWidth, maxHeight, jpegQuality));
#elif WINDOWS
            return Task.FromResult(CompressWindows(input, maxWidth, maxHeight, jpegQuality));
#else
            // Fallback: try to return input or a minimally re-encoded version (no platform APIs available)
            return Task.FromResult(input);
#endif
        }

#if ANDROID
        static byte[] CompressAndroid(byte[] input, int maxW, int maxH, int quality)
        {
            Bitmap src = null;
            Bitmap scaled = null;
            try
            {
                src = BitmapFactory.DecodeByteArray(input, 0, input.Length);
                if (src == null)
                    return input;

                var (newW, newH) = CalculateDimensions(src.Width, src.Height, maxW, maxH);

                // If no resize needed, skip scaling
                if (newW == src.Width && newH == src.Height)
                {
                    using var ms2 = new MemoryStream();
                    src.Compress(Bitmap.CompressFormat.Jpeg, quality, ms2);
                    return ms2.ToArray();
                }

                scaled = Bitmap.CreateBitmap(newW, newH, Bitmap.Config.Argb8888);

                using (var canvas = new Canvas(scaled))
                {
                    var paint = new Android.Graphics.Paint(PaintFlags.FilterBitmap);
                    canvas.DrawBitmap(
                        src,
                        null,
                        new Android.Graphics.Rect(0, 0, newW, newH),
                        paint
                    );
                }

                using var ms = new MemoryStream();
                scaled.Compress(Bitmap.CompressFormat.Jpeg, quality, ms);

                return ms.ToArray();
            }
            finally
            {
                // Clean up safely
                src?.Recycle();
                src?.Dispose();
                scaled?.Recycle();
                scaled?.Dispose();
            }
        }
#endif

#if IOS || MACCATALYST
        static byte[] CompressIos(byte[] input, int maxW, int maxH, int quality)
        {
            using var data = NSData.FromArray(input);
            using var image = UIImage.LoadFromData(data);
            if (image == null)
                return input;

            var originalSize = image.Size;
            var (newWf, newHf) = CalculateDimensionsFloat(
                (float)originalSize.Width,
                (float)originalSize.Height,
                maxW,
                maxH
            );
            var newSize = new CoreGraphics.CGSize(newWf, newHf);

            UIGraphics.BeginImageContextWithOptions(newSize, false, 1.0f);
            try
            {
                image.Draw(new CoreGraphics.CGRect(0, 0, newSize.Width, newSize.Height));
                using var resized = UIGraphics.GetImageFromCurrentImageContext();
                using var jpeg = resized.AsJPEG((nfloat)quality / 100.0);
                return jpeg.ToArray();
            }
            finally
            {
                UIGraphics.EndImageContext();
            }
        }
#endif

#if WINDOWS
        static byte[] CompressWindows(byte[] input, int maxW, int maxH, int quality)
        {
            using var msIn = new MemoryStream(input);
            using var src = Image.FromStream(msIn);
            var (newW, newH) = CalculateDimensions(src.Width, src.Height, maxW, maxH);

            using var dest = new Bitmap(newW, newH);
            using (var g = Graphics.FromImage(dest))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.DrawImage(src, 0, 0, newW, newH);
            }

            using var msOut = new MemoryStream();
            var encoder = GetEncoder(ImageFormat.Jpeg);
            var encParams = new EncoderParameters(1);
            encParams.Param[0] = new EncoderParameter(Encoder.Quality, (long)quality);
            dest.Save(msOut, encoder, encParams);
            return msOut.ToArray();
        }

        static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            var codecs = ImageCodecInfo.GetImageDecoders();
            foreach (var codec in codecs)
                if (codec.FormatID == format.Guid)
                    return codec;
            return ImageCodecInfo.GetImageDecoders()[0];
        }
#endif

        // Compute new integer dimensions preserving aspect ratio
        static (int width, int height) CalculateDimensions(
            int originalW,
            int originalH,
            int maxW,
            int maxH
        )
        {
            if (originalW <= maxW && originalH <= maxH)
                return (originalW, originalH);

            double ratio = Math.Min((double)maxW / originalW, (double)maxH / originalH);
            int newW = Math.Max(1, (int)Math.Round(originalW * ratio));
            int newH = Math.Max(1, (int)Math.Round(originalH * ratio));
            return (newW, newH);
        }

        // Float variant for iOS sizing using CGSizes
        static (float width, float height) CalculateDimensionsFloat(
            float originalW,
            float originalH,
            int maxW,
            int maxH
        )
        {
            if (originalW <= maxW && originalH <= maxH)
                return (originalW, originalH);

            var ratio = Math.Min((float)maxW / originalW, (float)maxH / originalH);
            var newW = Math.Max(1f, (float)Math.Round(originalW * ratio));
            var newH = Math.Max(1f, (float)Math.Round(originalH * ratio));
            return (newW, newH);
        }
    }
}
