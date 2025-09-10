#if WINDOWS
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace LlmTornado.Agents.Native;

public static partial class ComputerToolUtility
{
    /// <summary>
    /// Get screen size on Windows pc
    /// </summary>
    /// <returns></returns>
    public static System.Drawing.Size GetScreenSize()
    {
        int width = GetSystemMetrics(SM_CXSCREEN);
        int height = GetSystemMetrics(SM_CYSCREEN);
        return new System.Drawing.Size(width, height);
    }
    
    // -----------------------
    // Cursor & Mouse Control
    // -----------------------
    /// <summary>
    /// Set cursor position
    /// </summary>
    /// <param name="X"></param>
    /// <param name="Y"></param>
    /// <returns></returns>
    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int X, int Y);

    /// <summary>
    /// Get current cursor position
    /// </summary>
    /// <param name="lpPoint"></param>
    /// <returns></returns>
    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    /// <summary>
    /// Trigger mouse click event
    /// </summary>
    /// <param name="dwFlags"></param>
    /// <param name="dx"></param>
    /// <param name="dy"></param>
    /// <param name="dwData"></param>
    /// <param name="dwExtraInfo"></param>
    [DllImport("user32.dll")]
    private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

    /// <summary>
    /// Get screen size
    /// </summary>
    /// <param name="nIndex"></param>
    /// <returns></returns>
    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    /// <summary>
    /// Width of screen in px
    /// </summary>
    private const int SM_CXSCREEN = 0;
    /// <summary>
    /// Height of screen in px
    /// </summary>
    private const int SM_CYSCREEN = 1;

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
    }

    private const uint MOUSEEVENTF_MOVE = 0x0001;
    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;
    private const uint MOUSEEVENTF_WHEEL = 0x0800;
    private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
    private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
    private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
    private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
    
    /// <summary>
    /// Set cursor position [windows only]
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    public static void SetCursorPosition(int x, int y)
    {
        SetCursorPos(x, y);
    }

    /// <summary>
    /// Get Cursor position [windows only]
    /// </summary>
    /// <returns></returns>
    public static POINT GetCursorPosition()
    {
        GetCursorPos(out POINT point);
        return point;
    }

    /// <summary>
    /// Smoothly move cursor to new position
    /// </summary>
    /// <param name="toX"></param>
    /// <param name="toY"></param>
    /// <param name="steps"></param>
    public static void MoveCursorSmooth(int toX, int toY, int steps = 50)
    {
        POINT start = GetCursorPosition();
        for (int i = 1; i <= steps; i++)
        {
            int x = start.X + (toX - start.X) * i / steps;
            int y = start.Y + (toY - start.Y) * i / steps;
            SetCursorPos(x, y);
            Thread.Sleep(5);
        }
    }

    /// <summary>
    /// Trigger mouse right click event
    /// </summary>
    public static void RightClick()
    {
        mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, UIntPtr.Zero);
        mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, UIntPtr.Zero);
    }

    /// <summary>
    /// Simulates a middle mouse button click.
    /// </summary>
    /// <remarks>This method performs a middle mouse button click by simulating both the press and
    /// release actions. It is typically used for automation or testing scenarios where programmatic mouse input is
    /// required.</remarks>
    public static void MiddleClick()
    {
        mouse_event(MOUSEEVENTF_MIDDLEDOWN, 0, 0, 0, UIntPtr.Zero);
        mouse_event(MOUSEEVENTF_MIDDLEUP, 0, 0, 0, UIntPtr.Zero);
    }

    /// <summary>
    /// Simulates a left mouse button click at the current cursor position.
    /// </summary>
    /// <remarks>This method performs a left mouse button press followed by a release, effectively
    /// simulating a click. It uses the <c>mouse_event</c> function from the Windows API to generate the mouse
    /// events.</remarks>
    public static void Click()
    {
        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
        mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
    }

    /// <summary>
    /// Simulates a double-click action by performing two consecutive click operations with a short delay in
    /// between.
    /// </summary>
    /// <remarks>This method performs two click actions with a 100-millisecond delay between them to
    /// mimic a typical double-click behavior. Ensure that the environment where this method is used supports the
    /// concept of a double-click.</remarks>
    public static void DoubleClick()
    {
        Click();
        Thread.Sleep(100);
        Click();
    }

    /// <summary>
    /// Moves the cursor to the specified screen coordinates and performs a double-click action.
    /// </summary>
    /// <remarks>The cursor is moved smoothly to the specified position before performing the
    /// double-click. A short delay is introduced between the two clicks to ensure the double-click is
    /// registered.</remarks>
    /// <param name="toX">The X-coordinate of the target position on the screen.</param>
    /// <param name="toY">The Y-coordinate of the target position on the screen.</param>
    public static void MoveAndDoubleClick(int toX, int toY)
    {
        MoveCursorSmooth(toX, toY);
        Click();
        Thread.Sleep(100);
        Click();
    }

    /// <summary>
    /// Moves the cursor to the specified screen coordinates and performs a mouse click.
    /// </summary>
    /// <remarks>This method moves the cursor smoothly to the specified position, performs a click
    /// action,  and introduces a brief delay after the click. The delay ensures that subsequent operations  have
    /// time to process the click event.</remarks>
    /// <param name="toX">The X-coordinate, in pixels, to move the cursor to.</param>
    /// <param name="toY">The Y-coordinate, in pixels, to move the cursor to.</param>
    public static void MoveAndClick(int toX, int toY)
    {
        MoveCursorSmooth(toX, toY);
        Click();
        Thread.Sleep(100);
    }

    /// <summary>
    /// Moves the mouse cursor to the specified screen coordinates and performs a right-click.
    /// </summary>
    /// <remarks>The method moves the cursor smoothly to the specified position, performs a
    /// right-click,  and introduces a brief delay of 100 milliseconds after the click.</remarks>
    /// <param name="toX">The X-coordinate, in pixels, to move the cursor to.</param>
    /// <param name="toY">The Y-coordinate, in pixels, to move the cursor to.</param>
    public static void MoveAndRightClick(int toX, int toY)
    {
        MoveCursorSmooth(toX, toY);
        RightClick();
        Thread.Sleep(100);
    }

    /// <summary>
    /// Simulates a mouse scroll action by sending a scroll event with the specified amount.
    /// </summary>
    /// <remarks>The method uses the system's mouse event functionality to perform the scroll action. 
    /// The <paramref name="amount"/> parameter represents the scroll delta, typically in units  defined by the
    /// system's mouse wheel settings.</remarks>
    /// <param name="amount">The amount to scroll. Positive values scroll up, and negative values scroll down.</param>
    public static void Scroll(int amount)
    {
        mouse_event(MOUSEEVENTF_WHEEL, 0, 0, (uint)amount, UIntPtr.Zero);
    }

    /// <summary>
    /// Drag Items from current mouse position to new X,Y location
    /// </summary>
    /// <param name="toX"></param>
    /// <param name="toY"></param>
    public static void Drag(int toX, int toY)
    {
        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
        MoveCursorSmooth(toX, toY);
        mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
    }

    // -------------------
    // Keyboard Control
    // -------------------
    /// <summary>
    /// 
    /// </summary>
    /// <param name="bVk"></param>
    /// <param name="bScan"></param>
    /// <param name="dwFlags"></param>
    /// <param name="dwExtraInfo"></param>
    [DllImport("user32.dll", SetLastError = true)]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    private const int KEYEVENTF_KEYDOWN = 0x0000;
    private const int KEYEVENTF_KEYUP = 0x0002;

    /// <summary>
    /// Translates a character to the corresponding virtual-key code and shift state.
    /// </summary>
    /// <remarks>The shift state can be a combination of the following bits: <list type="bullet">
    /// <item><description>1: Shift key is pressed.</description></item> <item><description>2: Control key is
    /// pressed.</description></item> <item><description>4: Alt key is pressed.</description></item>
    /// </list></remarks>
    /// <param name="ch">The character to be translated into a virtual-key code.</param>
    /// <returns>A short integer containing the virtual-key code in the low-order byte and the shift state in the high-order
    /// byte. If the function cannot translate the character, it returns -1.</returns>
    [DllImport("user32.dll")]
    private static extern short VkKeyScan(char ch);

    /// <summary>
    /// Simulates a key press for the specified key code.
    /// </summary>
    /// <remarks>This method sends a key down and key up event with a short delay in between to
    /// simulate a key press. It is intended for use in scenarios where programmatic key input is
    /// required.</remarks>
    /// <param name="keyCode">The virtual key code of the key to press. Must be a valid key code.</param>
    public static void PressKey(byte keyCode)
    {
        keybd_event(keyCode, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
        Thread.Sleep(50);
        keybd_event(keyCode, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    /// <summary>
    /// Simulates a key press for the specified key code.
    /// </summary>
    /// <remarks>This method uses the Windows API to simulate a key press event. The key is pressed
    /// and held for a short duration before being released. The method is intended for use in environments where
    /// direct keyboard input simulation is required.</remarks>
    /// <param name="keyCode">A string representing the key to be pressed. The string should contain a single character whose virtual key
    /// code will be determined and used for the key press simulation.</param>
    public static void PressKey(string keyCode)
    {
        short vkCode = VkKeyScan(keyCode[0]); // Get virtual key code for the character
        byte keyCod = (byte)(vkCode & 0xFF); // Extract the virtual key code (low-order byte)
        keybd_event(keyCod, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
        Thread.Sleep(50);
        keybd_event(keyCod, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    /// <summary>
    /// Simulates typing a list of strings by sending virtual key codes for each character.
    /// </summary>
    /// <remarks>This method converts each character in the provided strings to its corresponding
    /// virtual key code and simulates key presses. If a character requires a shift key, the method simulates
    /// pressing and releasing the left shift key. A delay is introduced between each key press to mimic natural
    /// typing.</remarks>
    /// <param name="texts">A list of strings to be typed. Each string in the list is processed sequentially.</param>
    public static void Type(List<string> texts)
    {
        foreach (string text in texts)
        {
            foreach (char ch in text)
            {
                short vkCode = VkKeyScan(ch); // Get virtual key code for the character
                byte keyCode = (byte)(vkCode & 0xFF); // Extract the virtual key code (low-order byte)

                // Check if a shift state is required (high-order byte)
                bool needsShift = ((vkCode >> 8) & 1) != 0;

                if (needsShift)
                {
                    PressKey((byte)0xA0); // Simulate Left Shift press
                }

                PressKey(keyCode);

                if (needsShift)
                {
                    PressKey((byte)0xA0); // Simulate Left Shift release
                }
                Thread.Sleep(50);
            }
        }
    }

    /// <summary>
    /// Simulates typing a list of strings by sending virtual key codes for each character.
    /// </summary>
    /// <remarks>This method converts each character in the provided strings to its corresponding
    /// virtual key code and simulates key presses. If a character requires a shift key, the method simulates
    /// pressing and releasing the left shift key. A delay is introduced between each key press to mimic natural
    /// typing.</remarks>
    /// <param name="text">A strings to be typed.</param>
    public static void Type(string text)
    {
        foreach (char ch in text)
        {
            short vkCode = VkKeyScan(ch); // Get virtual key code for the character
            byte keyCode = (byte)(vkCode & 0xFF); // Extract the virtual key code (low-order byte)

            // Check if a shift state is required (high-order byte)
            bool needsShift = ((vkCode >> 8) & 1) != 0;

            if (needsShift)
            {
                PressKey((byte)0xA0); // Simulate Left Shift press
            }

            PressKey(keyCode);

            if (needsShift)
            {
                PressKey((byte)0xA0); // Simulate Left Shift release
            }
            Thread.Sleep(50);
        }
    }

    // -------------------
    // Screenshot using GDI
    // -------------------
    /// <summary>
    /// The BitBlt function performs a bit-block transfer of the color data corresponding to a rectangle of pixels 
    /// from the specified source device context into a destination device context.
    /// </summary>
    /// <param name="hdcDest"></param>
    /// <param name="nXDest"></param>
    /// <param name="nYDest"></param>
    /// <param name="nWidth"></param>
    /// <param name="nHeight"></param>
    /// <param name="hdcSrc"></param>
    /// <param name="nXSrc"></param>
    /// <param name="nYSrc"></param>
    /// <param name="dwRop"></param>
    /// <returns></returns>
    [DllImport("gdi32.dll")]
    private static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight,
        IntPtr hdcSrc, int nXSrc, int nYSrc, int dwRop);
    /// <summary>
    /// Retrieves a handle to the desktop window.
    /// </summary>
    /// <remarks>The desktop window covers the entire screen and is the area on which icons and other
    /// windows are painted.</remarks>
    /// <returns>An <see cref="IntPtr"/> representing the handle to the desktop window.</returns>
    [DllImport("user32.dll")]
    private static extern IntPtr GetDesktopWindow();

    /// <summary>
    /// Retrieves the device context (DC) for the entire window, including title bar, menus, and scroll bars.
    /// </summary>
    /// <remarks>The device context must be released after use by calling the <c>ReleaseDC</c>
    /// function, to avoid resource leaks.</remarks>
    /// <param name="hWnd">A handle to the window whose DC is to be retrieved. If this value is <see langword="null"/>, the DC for the
    /// entire screen is retrieved.</param>
    /// <returns>An <see cref="IntPtr"/> to the device context for the specified window. If the function fails, the return
    /// value is <see cref="IntPtr.Zero"/>.</returns>
    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowDC(IntPtr hWnd);

    /// <summary>
    /// Creates a memory device context (DC) compatible with the specified device.
    /// </summary>
    /// <remarks>The created memory DC can be used for off-screen drawing and is compatible with the
    /// device context specified by <paramref name="hdc"/>. The application must call the <c>DeleteDC</c> function
    /// to delete the memory DC when it is no longer needed.</remarks>
    /// <param name="hdc">A handle to an existing device context. If this parameter is <see langword="null"/>, the function creates a
    /// memory DC compatible with the application's current screen.</param>
    /// <returns>A handle to the newly created memory device context. Returns <see cref="IntPtr.Zero"/> if the function
    /// fails.</returns>
    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    /// <summary>
    /// Creates a bitmap compatible with the device context specified by the given handle.
    /// </summary>
    /// <remarks>The bitmap created by this function can be selected into any device context that is
    /// compatible with the specified device context.</remarks>
    /// <param name="hdc">A handle to the device context.</param>
    /// <param name="nWidth">The width, in pixels, of the bitmap to be created.</param>
    /// <param name="nHeight">The height, in pixels, of the bitmap to be created.</param>
    /// <returns>A handle to the newly created bitmap. If the function fails, the return value is <see cref="IntPtr.Zero"/>.</returns>

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

    /// <summary>
    /// Selects an object into the specified device context, replacing the previous object.
    /// </summary>
    /// <remarks>The selected object is used for drawing operations in the device context.  The caller
    /// is responsible for restoring the original object by calling <c>SelectObject</c> with the returned
    /// handle.</remarks>
    /// <param name="hdc">A handle to the device context.</param>
    /// <param name="hgdiobj">A handle to the object to be selected. This can be a pen, brush, bitmap, region, or font.</param>
    /// <returns>A handle to the object being replaced, or <see cref="IntPtr.Zero"/> if an error occurs.</returns>
    [DllImport("gdi32.dll")]
    private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

    /// <summary>
    /// Deletes the specified device context (DC).
    /// </summary>
    /// <remarks>This method is a P/Invoke declaration for the DeleteDC function in the GDI32.dll. It
    /// is used to release the resources associated with a device context.</remarks>
    /// <param name="hdc">A handle to the device context to be deleted. This handle must have been created by a previous call to a GDI
    /// function.</param>
    /// <returns><see langword="true"/> if the device context is successfully deleted; otherwise, <see langword="false"/>.</returns>
    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(IntPtr hdc);

    /// <summary>
    /// Deletes a GDI object.
    /// </summary>
    /// <remarks>This method is a P/Invoke declaration for the DeleteObject function in the GDI32.dll.
    /// It is used to release resources associated with GDI objects such as bitmaps, pens, and brushes.</remarks>
    /// <param name="hObject">A handle to the GDI object to be deleted.</param>
    /// <returns><see langword="true"/> if the object is successfully deleted; otherwise, <see langword="false"/>.</returns>
    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    private const int SRCCOPY = 0x00CC0020;

    /// <summary>
    /// Captures a screenshot of the entire screen.
    /// </summary>
    /// <remarks>The method captures the current state of the screen and returns it as a bitmap image.
    /// The caller is responsible for disposing of the returned <see cref="Bitmap"/> object to free
    /// resources.</remarks>
    /// <returns>A <see cref="Bitmap"/> object containing the screenshot of the current screen.</returns>
    public static Bitmap TakeScreenshot()
    {
        int width = GetSystemMetrics(SM_CXSCREEN);
        int height = GetSystemMetrics(SM_CYSCREEN);

        Bitmap bmp = new Bitmap(width, height);
        using (Graphics g = Graphics.FromImage(bmp))
        {
            IntPtr hdcDest = g.GetHdc();
            IntPtr hdcSrc = GetWindowDC(GetDesktopWindow());
            BitBlt(hdcDest, 0, 0, width, height, hdcSrc, 0, 0, SRCCOPY);
            g.ReleaseHdc(hdcDest);
        }
        return bmp;
    }

    /// <summary>
    /// Captures a screenshot of the entire screen and returns it as a byte array in the specified image format.
    /// </summary>
    /// <remarks>The method captures the current screen content and converts it into a byte array
    /// using the specified image format. If no format is provided, the screenshot is saved in PNG format by
    /// default.</remarks>
    /// <param name="format">The image format to use for the screenshot. Defaults to PNG if not specified.</param>
    /// <returns>A byte array containing the screenshot image data in the specified format.</returns>
    public static byte[] TakeScreenshotByteArray(ImageFormat? format = null)
    {
        int width = GetSystemMetrics(SM_CXSCREEN);
        int height = GetSystemMetrics(SM_CYSCREEN);

        format ??= ImageFormat.Png; // default to PNG

        using (MemoryStream ms = new MemoryStream())
        {
            Bitmap bmp = new Bitmap(width, height);

            using (Graphics g = Graphics.FromImage(bmp))
            {
                IntPtr hdcDest = g.GetHdc();
                IntPtr hdcSrc = GetWindowDC(GetDesktopWindow());
                BitBlt(hdcDest, 0, 0, width, height, hdcSrc, 0, 0, SRCCOPY);
                g.ReleaseHdc(hdcDest);
            }

            //bmp.Save("screenshot.png");

            bmp.Save(ms, format);

            bmp.Dispose();

            return ms.ToArray();
        }
    }
}
#endif