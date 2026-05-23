using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ChatApp.Database;
using ChatApp.Models;
using Newtonsoft.Json;

namespace ChatApp.Utilities
{
    public static class ChatExtensions
    {
        public static bool screenIsOff = false;

        public static string StoryUserName = string.Empty;

        public static string charId = string.Empty;


        public static ImageSource? ConvertByteArrayToImageSource(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0)
            {
                return null;
            }
            var stream = new MemoryStream(imageData);
            return ImageSource.FromStream(() => stream);
        }

        public static byte[] CreateThumbnail(byte[] imageBytes, int width, int height)
        {
            if (imageBytes == null || imageBytes.Length == 0)
                throw new ArgumentException(
                    "Image data cannot be null or empty.",
                    nameof(imageBytes)
                );

            using var inputStream = new MemoryStream(imageBytes);
            var originalImage = Microsoft.Maui.Graphics.Platform.PlatformImage.FromStream(
                inputStream
            );

            if (originalImage == null)
                throw new Exception("Failed to decode image.");

            var resizedImage = originalImage.Resize(width, height, ResizeMode.Fit);

            if (resizedImage == null)
                throw new Exception("Failed to resize image.");

            using var outputStream = new MemoryStream();
            resizedImage.Save(outputStream, ImageFormat.Jpeg);

            return outputStream.ToArray();
        }

        public static string GenerateGroupId(string user1Id, string user2Id)
        {
            return string.Compare(user1Id, user2Id) < 0
                ? $"{user1Id}:{user2Id}"
                : $"{user2Id}:{user1Id}";
        }

        public static string DefaultBase64Image =
    "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";

        public static List<string> CharsList = new List<string>()
        {
            "Charc100__BrokenHeart__",
            "Charc101__Hug__",
            "Charc102__Heart__",
            "Charc103__Kiss__",
            "Charc104__Ring__",
            "Charc105__Calling__",
            "Charc106__Engine__",
            "Charc10__Dancing__",
            "Charc11__Dancing__",
            "Charc12__Dancing__",
            "Charc13__Walking__",
            "Charc14__Dancing__",
            "Charc15__Walking__",
            "Charc16__Dancing__",
            "Charc17__Dancing__",
            "Charc18__Dancing__",
            "Charc19__Dancing__",
            "Charc1__Running__",
            "Charc20__Walking__",
            "Charc21__Dancing__",
            "Charc22__Dancing__",
            "Charc23__Dancing__",
            "Charc24__Walking__",
            "Charc25__Walking__",
            "Charc26__Dancing__",
            "Charc27__Dancing__",
            "Charc28__Dancing__",
            "Charc29__Walking__",
            "Charc2__Dancing__",
            "Charc30__Walking__",
            "Charc31__Dancing__",
            "Charc32__Dancing__",
            "Charc33__Standing__",
            "Charc34__Dancing__",
            "Charc35__Dancing__",
            "Charc36__Dancing__",
            "Charc37__Dancing__",
            "Charc38__Dancing__",
            "Charc39__Dancing__",
            "Charc3__Dancing__",
            "Charc40__Angry__",
            "Charc41__Defeated__",
            "Charc42__Dying__",
            "Charc43__Jab__",
            "Charc44__Jogging__",
            "Charc45__Praying__",
            "Charc46__Swimming__",
            "Charc47__Taunt__",
            "Charc48__Angry__",
            "Charc49__Drunk__",
            "Charc4__Dancing__",
            "Charc50__Dying__",
            "Charc51__Falling__",
            "Charc52__Jump__",
            "Charc53__Nervously__",
            "Charc54__Swimming__",
            "Charc55__Falling__",
            "Charc56__Kiss__",
            "Charc57__Swimming___",
            "Charc58__Texting__",
            "Charc59__Dying__",
            "Charc5__Walking__",
            "Charc60__Falling__",
            "Charc61__Jab__",
            "Charc62__KnockedOut__",
            "Charc63__Praying__",
            "Charc64__RightTurn__",
            "Charc65__Standing__",
            "Charc66__Swimming__",
            "Charc67__Taunt__",
            "Charc68__Angry__",
            "Charc69__Drunk__",
            "Charc6__Dancing__",
            "Charc70__Dwarf__",
            "Charc71__Laying__",
            "Charc72__Idle__",
            "Charc73__Standing__",
            "Charc74__Texting__",
            "Charc75__Fax__",
            "Charc76__Angry__",
            "Charc77__Gesture__",
            "Charc78__Drunk__",
            "Charc79__Excited__",
            "Charc7__Dancing__",
            "Charc80__Stand__",
            "Charc81__Stand__",
            "Charc82__Walk__",
            "Charc83__Golf__",
            "Charc84__Side__",
            "Charc85__Dancing__",
            "Charc86__Villain__",
            "Charc87__Jogging__",
            "Charc88__Girl__",
            "Charc89__Girl__",
            "Charc8__Sick__",
            "Charc90__Run__",
            "Charc91__Swimming__",
            "Charc92__Fax__",
            "Charc93__Walk__",
            "Charc94__Walk__",
            "Charc95__Rose__",
            "Charc96__Heart__",
            "Charc97__Hug__",
            "Charc98__Hug__",
            "Charc99__Heart__",
            "Charc9__Dancing__",
        };
    }
}
