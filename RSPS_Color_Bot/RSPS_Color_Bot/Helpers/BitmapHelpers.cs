
using System.Drawing;

namespace RSPS_Color_Bot.Helpers
{
    public class BitmapHelpers
    {
        public Bitmap ReplaceColor(Bitmap bitmap, Color originalColor, Color replacementColor)
        {
            for (var y = 0; y < bitmap.Height; y++)
            {
                for (var x = 0; x < bitmap.Width; x++)
                {
                    //Check if color are similar within a tolerance
                    if (!AreColorsSimilar(originalColor, bitmap.GetPixel(x, y), 30))
                    {
                        //Replace bitmap pixel color
                        bitmap.SetPixel(x, y, replacementColor);
                    }
                }
            }
            return bitmap;
        }

        public Bitmap ReplaceColorAndEverythingElse(Bitmap bitmap, Color originalColor, Color replacementColor, Color finalColor)
        {
            for (var y = 0; y < bitmap.Height; y++)
            {
                for (var x = 0; x < bitmap.Width; x++)
                {
                    //Check if color are similar within a tolerance
                    if (!AreColorsSimilar(originalColor, bitmap.GetPixel(x, y), 3))
                    {
                        //Replace bitmap pixel color
                        bitmap.SetPixel(x, y, replacementColor);
                    }
                    else
                    {
                        bitmap.SetPixel(x, y, finalColor);
                    }
                }
            }
            return bitmap;
        }

        public Bitmap ReplaceColorReplace(Bitmap bitmap, Color replaceColor, Color replacementColor)
        {
            for (var y = 0; y < bitmap.Height; y++)
            {
                for (var x = 0; x < bitmap.Width; x++)
                {
                    if (AreColorsSimilar(replaceColor, bitmap.GetPixel(x, y), 30)) //30
                    {
                        bitmap.SetPixel(x, y, replacementColor);
                    }
                }
            }
            return bitmap;
        }

        public bool AreColorsSimilar(Color c1, Color c2, int tolerance)
        {
            return Math.Abs(c1.R - c2.R) < tolerance &&
                   Math.Abs(c1.G - c2.G) < tolerance &&
                   Math.Abs(c1.B - c2.B) < tolerance;
        }


    }
}
