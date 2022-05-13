using IronOcr;
using RSPS_Color_Bot.Helpers;
using System.Drawing;
using System.Runtime.InteropServices;

namespace RSPS_Color_Bot
{
    public class Execute
    {
        /// <summary>
        /// Region imports from windows -> we use this to handle any windows related handling, such as moving the mouse and getting window information
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="pwi"></param>
        /// <returns></returns>
        #region DllImports

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool GetWindowInfo(IntPtr hwnd, ref WINDOWINFO pwi);

        [DllImport("user32.dll")]
        static extern IntPtr GetWindowRect(IntPtr hWnd, ref Rect rect);

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        static extern int GetSystemMetrics(int smIndex);

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint SendInput(uint nInputs, ref INPUT pInputs, int cbSize);

        #endregion

        /// <summary>
        /// Global instance ocr so we don't need to make more than one
        /// </summary>
        public IronTesseract ocr { get; set; }

        /// <summary>
        /// Dimensions of the window
        /// </summary>
        public Dimensions dimensions { get; set; }
        /// <summary>
        /// Information about the window
        /// </summary>
        public WINDOWINFO windowInfo { get; set; }

        /// <summary>
        /// The bitmapMainScreen, or better to say: A "screenshot" of the entire client
        /// We use this to retrieve information we require (and often take sub-images of this bitmap)
        /// </summary>
        public Bitmap bitMapMainScreen { get; set; }

        /// <summary>
        /// Boolean to keep track of if we're fighting or not
        /// </summary>
        public bool isFighting = false;

        BitmapHelpers bitmapHelpers = new BitmapHelpers();

        /// <summary>
        /// List of items we want to look for -> use a color picker
        /// The first number is always 0 because we don't want to use alpha in our client (no use for)
        /// </summary>
        public static List<Color> ItemColorsToLookFor = new List<Color>
        {
            Color.FromArgb(0, 175, 135, 13),
            Color.FromArgb(0, 132, 133, 145),
            Color.FromArgb(0, 150, 134, 92),
            Color.FromArgb(0, 119, 17, 9),
            Color.FromArgb(0, 222, 218, 218),
        };

        /// <summary>
        /// List of items we're looking for (this will be checked in the top-left corner upon hovering)
        /// </summary>
        public static List<string> ItemNamesWhiteList = new List<string>
        {
            "small coin bag",
            "raw lobster",
            "raw shark",
            "death rune",
            "coins",
            "crystal key"
        };


        /// <summary>
        /// Currently not in use, but can be used to filter out items we don't want to pick up
        /// </summary>
        public static List<string> ItemNamesBlackList = new List<string>
        {
            "bones"
        };

        /// <summary>
        /// The color of the monster we want to click/attack
        /// </summary>
        public static Color MonsterColor = Color.FromArgb(0, 168, 128, 126);


        public struct Dimensions
        {
            public int width;
            public int height;
        }

        /// <summary>
        /// The main entry point of the code
        /// </summary>
        public void MainLoop()
        {
            ///Starting the loop right here!
            Console.WriteLine("STARTING");
            //We initialize fields/data here that only need to be initialized once
            this.Initialize();

            //While true loop, this will keep running indefinitly.
            while (true)
            {
                //First retrieve all the information about the window (aka screen/client) we want to retrieve data from
                GetAllWindowInfo();
                //We use the data from the info to take a screenshot/bitmap from the client.
                GetBitmapMainScreen();

                //If we're fighting: Only check if we're still fighting, and check for drops meanwhile
                if (isFighting)
                {
                    Console.WriteLine("Currently fighting, checking for drops");
                    CheckIfFighting();
                    CheckForDrops();
                }
                else //Otherwise check for a new fight!
                {
                    //Look for a new fight
                    CheckForPossibleFight();
                }
            }
        }

        public void CheckForPossibleFight()
        {

            //Loop over the X-coordinates for the bitmap
            for (int x = 0; x < this.bitMapMainScreen.Width; x++)
            {
                //Loop over the Y-coordinates for the bitmap
                for (int y = 0; y < this.bitMapMainScreen.Height; y++)
                {
                    //See if the current combination of x/y coordinates color (color of that pixel) matches the one we look for
                    if (bitmapHelpers.AreColorsSimilar(bitMapMainScreen.GetPixel(x, y), MonsterColor, 1))
                    {
                        Console.WriteLine("Found a monster!");
                        //Move the mouse to the position of the item
                        MoveMouseToLocation(x, y);
                        //Wait a bit, so we can get a new image once the mouse arrived
                        Thread.Sleep(50);
                        //Refresh the bitmap so we can check if we hover the correct item
                        GetBitmapMainScreen();

                        //See if we can extract the "attack" text from the bitmap
                        OcrResult result = GetItemTextWalk();

                        if (!string.IsNullOrEmpty(result.Text) && result.Text.ToLower().Contains("attack"))
                        {
                            Console.WriteLine("Time to attack a monster!");
                            //Click if we found the color + text says "attack"
                            LeftMouseClick(x, y);
                            //We're fighting!
                            this.isFighting = true;
                            //Wait a bit, since fighting will obviously not be done in a second, this can also be optimized by checking if we're fighting (for example if we mislicked)
                            Thread.Sleep(3000);
                            return;
                        }
                    }
                }
            }
        }

        public void LeftMouseClick(int x, int y)
        {
            //Setup input, we use this as our ACTUAL mouse 
            INPUT mouseDownInput = new INPUT();
            mouseDownInput.type = SendInputEventType.InputMouse;
            //the "65536" is used to translate the x/y coordinates to actual pixels on our screen.
            //Otherwise the coordinates will be incorrect and you'll misclick
            mouseDownInput.mkhi.mi.dx = ((x + windowInfo.rcClient.Left) * 65536) / GetSystemMetrics(0);
            mouseDownInput.mkhi.mi.dy = ((y + windowInfo.rcClient.Top) * 65536) / GetSystemMetrics(1);

            //Mouse button left down (part 1 of clicking)
            mouseDownInput.mkhi.mi.dwFlags = MouseEventFlags.MOUSEEVENTF_LEFTDOWN;
            SendInput(1, ref mouseDownInput, Marshal.SizeOf(new INPUT()));

            //Mouse button left up (part 2 of clicking)
            mouseDownInput.mkhi.mi.dwFlags = MouseEventFlags.MOUSEEVENTF_LEFTUP;
            SendInput(1, ref mouseDownInput, Marshal.SizeOf(new INPUT()));
        }

        public void MoveMouseToLocation(int x, int y)
        {
            INPUT mouseDownInput = new INPUT();
            mouseDownInput.type = SendInputEventType.InputMouse;
            mouseDownInput.mkhi.mi.dx = ((x + windowInfo.rcClient.Left) * 65536) / GetSystemMetrics(0);
            mouseDownInput.mkhi.mi.dy = ((y + windowInfo.rcClient.Top) * 65536) / GetSystemMetrics(1);

            mouseDownInput.mkhi.mi.dwFlags = MouseEventFlags.MOUSEEVENTF_MOVE | MouseEventFlags.MOUSEEVENTF_ABSOLUTE;
            SendInput(1, ref mouseDownInput, Marshal.SizeOf(new INPUT()));
        }

        /// <summary>
        /// Scanning the bitmap for any drops we're searching for!
        /// </summary>
        public void CheckForDrops()
        {
            //This is purely used to skip x coordinates if we found something, so we don't get stuck on the same item 
            bool currentlySkippingCoordinates = false;
            int currentlySkippedCoordinates = 0;

            //Same as item -> loop over bitmap
            for (int x = 0; x < this.bitMapMainScreen.Width; x++)
            {
                //If we found something we don't want, we skip this pixel since we ain't picking it up, and it'll still be laying there
                if (currentlySkippingCoordinates)
                {
                    currentlySkippedCoordinates += 1;
                    //We'll skip 20 X-pixels, this can also be adjusted ofcourse
                    if (currentlySkippedCoordinates == 20)
                    {
                        currentlySkippedCoordinates = 0;
                        currentlySkippingCoordinates = false;
                    }
                }

                //If we found something, why should we continue looking?
                bool stopLookingAtY = false;
                for (int y = 0; y < this.bitMapMainScreen.Height; y++)
                {
                    if (stopLookingAtY)
                    {
                        break;
                    }

                    //Loop over our item colors to search for in the bitmap
                    foreach (var itemColors in ItemColorsToLookFor)
                    {
                        //Check if the bitmap color is similar to the one we search for, if it is: click it!
                        if (bitmapHelpers.AreColorsSimilar(bitMapMainScreen.GetPixel(x, y), itemColors, 1))
                        {
                            //Console.WriteLine("Found something!");
                            //Move the mouse to the position of the item
                            MoveMouseToLocation(x, y);
                            //Wait a bit, so we can get a new image once the mouse arrived
                            Thread.Sleep(100);
                            //Refresh the bitmap so we can check if we hover the correct item
                            GetBitmapMainScreen();

                            //Yet again -> we want the text of the item we hover over, we do this by removing ANY data we don't want (and we only want the item text by using the item color)
                            //And then pass it to IronOCR so we can extract the 
                            OcrResult result = GetItemText();

                            if (!string.IsNullOrEmpty(result.Text))
                            {
                                //CheckIfHoveredItemIsNotInBlackList(result.Text) && 
                                if (CheckIfHoveredItemIsInWhiteList(result.Text))
                                {
                                    Console.WriteLine("About to click the following item: " + result.Text);
                                    //Get the item
                                    LeftClickOnPosition(x, y);
                                    Thread.Sleep(1000);
                                    return;
                                }
                                else
                                {
                                    //No need to look at this position, so we skip it for a little while
                                    stopLookingAtY = true;
                                    currentlySkippingCoordinates = true;
                                    break;
                                    //Skip 20x
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Check if the text from OCR is something we're looking for
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public bool CheckIfHoveredItemIsInWhiteList(string text)
        {
            foreach (var item in ItemNamesWhiteList)
            {
                if (text.ToLower().Equals(item))
                {
                    return true;
                }
            }

            return false;
        }

        public void LeftClickOnPosition(int x, int y)
        {
            INPUT mouseDownInput = new INPUT();
            mouseDownInput.type = SendInputEventType.InputMouse;
            mouseDownInput.mkhi.mi.dx = ((x + windowInfo.rcClient.Left) * 65536) / GetSystemMetrics(0);
            mouseDownInput.mkhi.mi.dy = ((y + windowInfo.rcClient.Top) * 65536) / GetSystemMetrics(1);

            //Left mouse button down
            mouseDownInput.mkhi.mi.dwFlags = MouseEventFlags.MOUSEEVENTF_LEFTDOWN;
            SendInput(1, ref mouseDownInput, Marshal.SizeOf(new INPUT()));

            Thread.Sleep(10);
            //Left mouse button up
            mouseDownInput.mkhi.mi.dwFlags = MouseEventFlags.MOUSEEVENTF_LEFTUP;
            SendInput(1, ref mouseDownInput, Marshal.SizeOf(new INPUT()));
        }


        /// <summary>
        /// Blacklist filter, currently not in use
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public bool CheckIfHoveredItemIsNotInBlackList(string text)
        {

            foreach (var item in ItemNamesBlackList)
            {
                if (text.Contains(item))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Gets the item text in the top left corner
        /// </summary>
        /// <returns></returns>
        public OcrResult GetItemText()
        {
            Rectangle cloneRect = new Rectangle(0, 0, 150, 30);
            Bitmap cloneBitmap = bitMapMainScreen.Clone(cloneRect, bitMapMainScreen.PixelFormat);
            //Isolate the orange text from everything else
            var zzz2 = this.bitmapHelpers.ReplaceColorAndEverythingElse(cloneBitmap, ColorDefinitions.itemText, ColorDefinitions.white, ColorDefinitions.black);

            //DEBUG
            zzz2 = ScaleBitmap(zzz2, 2, 2);
            //DEbug

            return this.BitmapToOcrResult(zzz2);
        }

        /// <summary>
        /// Get the text from the rectangle top-left corner (where for example "walk to" is usually displayed)
        /// </summary>
        /// <returns></returns>
        public OcrResult GetItemTextWalk()
        {
            Rectangle cloneRect = new Rectangle(0, 0, 150, 30);
            Bitmap cloneBitmap = bitMapMainScreen.Clone(cloneRect, bitMapMainScreen.PixelFormat);
            //Isolate the orange text from everything else
            var zzz2 = this.bitmapHelpers.ReplaceColor(cloneBitmap, ColorDefinitions.white, ColorDefinitions.black);
            return this.BitmapToOcrResult(zzz2);
        }

        /// <summary>
        /// Check if we're fightning by taking a part of the screen and see if there is any text (as that is our indicator for fightning)
        /// </summary>
        public void CheckIfFighting()
        {
            //Get part where fightning info is displayed
            Rectangle cloneRect = new Rectangle(0, 20, 150, 40);
            Bitmap cloneBitmap = this.bitMapMainScreen.Clone(cloneRect, this.bitMapMainScreen.PixelFormat);

            cloneBitmap = this.ScaleBitmap(cloneBitmap, 2, 2);

            //DEBUG -> use this if you want to see what the bitmap contains
            //System.IO.FileStream fs2 = System.IO.File.Create(@"D:\snapshot24.png");
            //cloneBitmap.Save(fs2, System.Drawing.Imaging.ImageFormat.Png);

            //fs2.Close();
            //DEBUG

            OcrResult retrievedText = BitmapToOcrResult(cloneBitmap);
            if (!string.IsNullOrEmpty(retrievedText.Text))
            {
                isFighting = true;
            }
            else
            {
                isFighting = false;
            }
        }

        /// <summary>
        /// Scale the bitmap up for OCR
        /// </summary>
        /// <param name="bitmap"></param>
        /// <param name="xScale"></param>
        /// <param name="yScale"></param>
        /// <returns></returns>
        public Bitmap ScaleBitmap(Bitmap bitmap, int xScale, int yScale)
        {
            Bitmap resized = new Bitmap(bitmap, new Size(bitmap.Width * xScale, bitmap.Height * yScale));
            return resized;
        }

        /// <summary>
        /// Uses a bitmap and retrieves any text extracted from it!
        /// </summary>
        /// <param name="bitmap"></param>
        /// <returns></returns>
        public OcrResult BitmapToOcrResult(Bitmap bitmap)
        {
            using (var Input = new OcrInput())
            {
                Input.AddImage(bitmap);
                OcrResult? result = ocr.Read(Input);
                return result;
            }
        }

        /// <summary>
        /// Refreshes a bitmap of the entire screen (to take sub-items from)
        /// </summary>
        public void GetBitmapMainScreen()
        {
            //We use the dimensions/location of the client to take a "screenshot"
            Bitmap bitMapMainScreen = new Bitmap(dimensions.width, dimensions.height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            //Copy from the dimensions
            Graphics.FromImage(bitMapMainScreen).CopyFromScreen(windowInfo.rcClient.Left, windowInfo.rcClient.Top, 0, 0, new Size(dimensions.width, dimensions.height), CopyPixelOperation.SourceCopy);
            //Dont forget to save it, we'll need it later!
            this.bitMapMainScreen = bitMapMainScreen;
        }

        public void GetAllWindowInfo()
        {
            //This is the process we search for. You can find this by hovering on an process in the bottom bar and using that name.
            //You can also search up all active windows and find it that way: https://stackoverflow.com/questions/7268302/get-the-titles-of-all-open-windows
            string processName = "Xeros";
            IntPtr hwndWindow = FindWindow(null, processName);

            //If the Pointer is 0, that means we didn't find anything :^(
            if (hwndWindow == (IntPtr)0)
                throw new Exception($"Unable to find {processName} as an active process.");

            //Otherwise we happily use the information :^D
            WINDOWINFO windowInfo = new WINDOWINFO();
            GetWindowInfo(hwndWindow, ref windowInfo);

            //Store the window information for later use
            this.windowInfo = windowInfo;

            //Same for the dimensions of the screen -> this means it can be updated by resizing the client/moving the client
            this.dimensions = new Dimensions
            {
                width = windowInfo.rcClient.Right - windowInfo.rcClient.Left,
                height = windowInfo.rcClient.Bottom - windowInfo.rcClient.Top,
            };
        }

        public void Initialize()
        {
            //Set-up ironOCR
            this.ocr = new IronTesseract();
            ocr.Language = OcrLanguage.English;
            ocr.Configuration.TesseractVersion = TesseractVersion.Tesseract5;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    struct INPUT
    {
        public SendInputEventType type;
        public MouseKeybdhardwareInputUnion mkhi;
    }
}
