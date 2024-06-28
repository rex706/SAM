using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;
using SteamAuth;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;
using Win32Interop.WinHandles;

namespace SAM.Core
{
    class WindowUtils
    {
        #region dll imports

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowThreadProcessId(IntPtr handle, out int processId);

        [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, [Out] StringBuilder lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, IntPtr lParam);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", EntryPoint = "PostMessageA")]
        public static extern bool PostMessage(IntPtr hWnd, uint msg, int wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, uint dwExtraInfo);

        delegate bool EnumThreadDelegate(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern bool EnumThreadWindows(int dwThreadId, EnumThreadDelegate lpfn, IntPtr lParam);

        #endregion

        public const int WM_GETTEXT = 0xD;
        public const int WM_GETTEXTLENGTH = 0xE;
        public const int WM_KEYDOWN = 0x0100;
        public const int WM_KEYUP = 0x0101;
        public const int WM_CHAR = 0x0102;
        public const int VK_RETURN = 0x0D;
        public const int VK_TAB = 0x09;
        public const int VK_SPACE = 0x20;

        readonly static char[] specialChars = { '{', '}', '(', ')', '[', ']', '+', '^', '%', '~' };
        private static bool loginAllCancelled = false;

        private static IEnumerable<IntPtr> EnumerateProcessWindowHandles(Process process)
        {
            var handles = new List<IntPtr>();

            foreach (ProcessThread thread in process.Threads)
                EnumThreadWindows(thread.Id, (hWnd, lParam) => { handles.Add(hWnd); return true; }, IntPtr.Zero);

            return handles;
        }

        private static string GetWindowTextRaw(IntPtr hwnd)
        {
            // Allocate correct string length first
            int length = (int)SendMessage(hwnd, WM_GETTEXTLENGTH, 0, IntPtr.Zero);
            StringBuilder sb = new StringBuilder(length + 1);
            SendMessage(hwnd, WM_GETTEXT, (IntPtr)sb.Capacity, sb);
            return sb.ToString();
        }

        public static IEnumerable<Process> GetChildProcesses(Process process)
        {
            List<Process> children = new List<Process>();
            try
            {
                ManagementObjectSearcher mos = new ManagementObjectSearcher(String.Format("Select * From Win32_Process Where ParentProcessID={0}", process.Id));

                foreach (ManagementObject mo in mos.Get())
                {
                    children.Add(Process.GetProcessById(Convert.ToInt32(mo["ProcessID"])));
                }
            }
            catch(Exception e) { 
                Console.WriteLine(e.Message);
            }

            return children;
        }

        public static WindowHandle GetSteamLoginWindow(Process steamProcess)
        {
            IEnumerable<Process> children = GetChildProcesses(steamProcess);

            foreach (Process childProcess in children)
            {
                if (childProcess.ProcessName == "steamwebhelper")
                {
                    IEnumerable<IntPtr> windows = EnumerateProcessWindowHandles(childProcess);
                    return GetSteamLoginWindow(windows);
                }
            }

            return WindowHandle.Invalid;
        }

        public static WindowHandle GetSteamLoginWindow(string processName)
        {
            Process[] steamProcess = Process.GetProcessesByName(processName);
            foreach (Process process in steamProcess)
            {
                WindowHandle handle = GetSteamLoginWindow(process);
                if (handle.IsValid)
                {
                    return handle;
                }
            }

            return WindowHandle.Invalid;
        }

        private static WindowHandle GetSteamLoginWindow(IEnumerable<IntPtr> windows)
        {
            foreach (IntPtr windowHandle in windows)
            {
                string text = GetWindowTextRaw(windowHandle);

                if ((text.Contains("Steam") && text.Length > 5) || text.Equals("蒸汽平台登录"))
                {
                    return new WindowHandle(windowHandle);
                }
            }

            return WindowHandle.Invalid;
        }

        public static WindowHandle GetMainSteamClientWindow(Process steamProcess)
        {
            IEnumerable<IntPtr> windows = EnumerateProcessWindowHandles(steamProcess);
            return GetMainSteamClientWindow(windows);   
        }

        public static WindowHandle GetMainSteamClientWindow(string processName)
        {
            Process[] steamProcess = Process.GetProcessesByName(processName);
            foreach (Process process in steamProcess)
            {
                IEnumerable<IntPtr> windows = EnumerateProcessWindowHandles(process);

                WindowHandle handle = GetMainSteamClientWindow(windows);
                if (handle.IsValid)
                {
                    return handle;
                }
            }

            return WindowHandle.Invalid;
        }

        private static WindowHandle GetMainSteamClientWindow(IEnumerable<IntPtr> windows)
        {
            foreach (IntPtr windowHandle in windows)
            {
                string text = GetWindowTextRaw(windowHandle);

                if (text.Equals("Steam") || text.Equals("蒸汽平台"))
                {
                    return new WindowHandle(windowHandle);
                }
            }

            return WindowHandle.Invalid;
        }

        public static WindowHandle GetLegacySteamLoginWindow()
        {
            return TopLevelWindowUtils.FindWindow(wh =>
            wh.GetClassName().Equals("vguiPopupWindow") &&
            ((wh.GetWindowText().Contains("Steam") &&
            !wh.GetWindowText().Contains("-") &&
            !wh.GetWindowText().Contains("—") &&
             wh.GetWindowText().Length > 5) ||
             wh.GetWindowText().Equals("蒸汽平台登录")));
        }

        public static WindowHandle GetLegacySteamGuardWindow()
        {
            // Also checking for vguiPopupWindow class name to avoid catching things like browser tabs.
            WindowHandle windowHandle = TopLevelWindowUtils.FindWindow(wh =>
            wh.GetClassName().Equals("vguiPopupWindow") &&
            (wh.GetWindowText().StartsWith("Steam Guard") ||
             wh.GetWindowText().StartsWith("Steam 令牌") ||
             wh.GetWindowText().StartsWith("Steam ガード")));
            return windowHandle;
        }

        public static WindowHandle GetLegacySteamWarningWindow()
        {
            return TopLevelWindowUtils.FindWindow(wh =>
            wh.GetClassName().Equals("vguiPopupWindow") &&
            (wh.GetWindowText().StartsWith("Steam - ") ||
             wh.GetWindowText().StartsWith("Steam — ")));
        }

        public static WindowHandle GetLegacyMainSteamClientWindow()
        {
            return TopLevelWindowUtils.FindWindow(wh =>
            wh.GetClassName().Equals("vguiPopupWindow") &&
            (wh.GetWindowText().Equals("Steam") ||
            wh.GetWindowText().Equals("蒸汽平台")));
        }

        public static LoginWindowState GetLoginWindowState(WindowHandle loginWindow)
        {
            if (!loginWindow.IsValid)
            {
                return LoginWindowState.Invalid;
            }

            using (var automation = new UIA3Automation())
            {
                try
                {
                    AutomationElement window = automation.FromHandle(loginWindow.RawPtr);

                    if (window == null)
                    {
                        return LoginWindowState.Invalid;
                    }

                    window.Focus();
#if DEBUG
                    AutomationElement[] descendants = window.FindAllChildren();
#endif
                    AutomationElement document = window.FindFirstDescendant(e => e.ByControlType(ControlType.Document));

                    if (document.FindAllChildren().Length == 0)
                    {
                        return LoginWindowState.Invalid;
                    }

                    int childNum = document.FindAllChildren().Length;

                    if (childNum == 2)
                    {
                        return LoginWindowState.Loading;
                    }

                    AutomationElement[] inputs = document.FindAllChildren(e => e.ByControlType(ControlType.Edit));
                    AutomationElement[] buttons = document.FindAllChildren(e => e.ByControlType(ControlType.Button));
                    AutomationElement[] groups = document.FindAllChildren(e => e.ByControlType(ControlType.Group));
                    AutomationElement[] images = document.FindAllChildren(e => e.ByControlType(ControlType.Image));

                    if (inputs.Length == 0 && images.Length > 0 && buttons.Length > 0)
                    {
                        return LoginWindowState.Selection;
                    }
                    else if (inputs.Length == 0 && images.Length == 0 && buttons.Length == 1)
                    {
                        return LoginWindowState.Error;
                    }
                    else if (inputs.Length == 5)
                    {
                        return LoginWindowState.Code;
                    }
                    else if (inputs.Length == 2 && buttons.Length == 1)
                    {
                        return LoginWindowState.Login;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    return LoginWindowState.Error;
                }
            }

            return LoginWindowState.Invalid;
        }

        public static LoginWindowState HandleAccountSelection(WindowHandle loginWindow)
        {
            using (var automation = new UIA3Automation())
            {
                try
                {
                    AutomationElement window = automation.FromHandle(loginWindow.RawPtr);

                    window.Focus();

                    AutomationElement document = window.FindFirstDescendant(e => e.ByControlType(ControlType.Document));
                    AutomationElement[] groups = document.FindAllChildren(e => e.ByControlType(ControlType.Group));

                    Button addAccountButton = groups[groups.Length - 1].AsButton();
                    addAccountButton.Invoke();

                    return LoginWindowState.Login;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    return LoginWindowState.Invalid;
                }
            }
        }

        public static LoginWindowState TryCredentialsEntry(WindowHandle loginWindow, string username, string password, bool remember)
        {
            using (var automation = new UIA3Automation())
            {
                try
                {
                    AutomationElement window = automation.FromHandle(loginWindow.RawPtr);

                    window.Focus();

                    AutomationElement document = window.FindFirstDescendant(e => e.ByControlType(ControlType.Document));

                    AutomationElement[] inputs = document.FindAllChildren(e => e.ByControlType(ControlType.Edit));
                    AutomationElement[] buttons = document.FindAllChildren(e => e.ByControlType(ControlType.Button));
                    AutomationElement[] groups = document.FindAllChildren(e => e.ByControlType(ControlType.Group));

                    Button signInButton = buttons[0].AsButton();

                    if (signInButton.IsEnabled)
                    {
                        TextBox usernameBox = inputs[0].AsTextBox();
                        usernameBox.WaitUntilEnabled();
                        usernameBox.Text = username;

                        TextBox passwordBox = inputs[1].AsTextBox();
                        passwordBox.WaitUntilEnabled();
                        passwordBox.Text = password;

                        Button checkBoxButton = groups[0].AsButton();
                        bool isChecked = checkBoxButton.FindFirstChild(e => e.ByControlType(ControlType.Image)) != null;

                        if (remember != isChecked)
                        {
                            checkBoxButton.Focus();
                            checkBoxButton.WaitUntilEnabled();
                            checkBoxButton.Invoke();
                        }

                        signInButton.Focus();
                        signInButton.WaitUntilEnabled();
                        signInButton.Invoke();

                        return LoginWindowState.Success;
                    }

                    return LoginWindowState.Invalid;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    return LoginWindowState.Invalid;
                }
            }
        }

        public static LoginWindowState TryCodeEntry(WindowHandle loginWindow, string secret)
        {
            using (var automation = new UIA3Automation())
            {
                try
                {
                    AutomationElement window = automation.FromHandle(loginWindow.RawPtr);

                    window.Focus();

                    AutomationElement document = window.FindFirstDescendant(e => e.ByControlType(ControlType.Document));
                    AutomationElement[] inputs = document.FindAllChildren(e => e.ByControlType(ControlType.Edit));

                    string code = Generate2FACode(secret);

                    try
                    {
                        for (int i = 0; i < inputs.Length; i++)
                        {
                            TextBox textBox = inputs[i].AsTextBox();
                            textBox.Text = code[i].ToString();
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        return LoginWindowState.Code;
                    }

                    return LoginWindowState.Success;
                }
                catch (Exception ex) 
                {
                    Console.WriteLine(ex.Message);
                    return LoginWindowState.Invalid;
                }
            }
        }

        public static Process WaitForSteamProcess(WindowHandle windowHandle)
        {
            Process process = null;

            // Wait for valid process to wait for input idle.
            Console.WriteLine("Waiting for it to be idle.");
            while (process == null)
            {
                GetWindowThreadProcessId(windowHandle.RawPtr, out int procId);

                // Wait for valid process id from handle.
                while (procId == 0)
                {
                    Thread.Sleep(100);
                    GetWindowThreadProcessId(windowHandle.RawPtr, out procId);
                }

                try
                {
                    process = Process.GetProcessById(procId);
                }
                catch
                {
                    process = null;
                }
            }

            return process;
        }

        public static WindowHandle WaitForSteamClientWindow()
        {
            WindowHandle steamClientWindow = WindowHandle.Invalid;

            Console.WriteLine("Waiting for full Steam client to initialize.");

            int waitCounter = 0;

            while (!steamClientWindow.IsValid && !loginAllCancelled)
            {
                if (waitCounter >= 600)
                {
                    MessageBoxResult messageBoxResult = MessageBox.Show(
                    "SAM has been waiting for Steam for longer than 60 seconds." +
                    "Would you like to skip this account and continue?" +
                    "Click No to wait another 60 seconds.",
                    "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                    if (messageBoxResult == MessageBoxResult.Yes)
                    {
                        return steamClientWindow;
                    }
                    else
                    {
                        waitCounter = 0;
                    }
                }

                steamClientWindow = GetMainSteamClientWindow("Steam");
                Thread.Sleep(100);
                waitCounter += 1;
            }

            loginAllCancelled = false;

            return steamClientWindow;
        }

        public static void CancelLoginAll()
        {
            loginAllCancelled = true;
        }

        /**
         * Because CapsLock is handled by system directly, thus sending
         * it to one particular window is invalid - a window could not
         * respond to CapsLock, only the system can.
         * 
         * For this reason, I break into a low-level API, which may cause
         * an inconsistency to the original `SendWait` method.
         * 
         * https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-keybd_event
         */
        public static void SendCapsLockGlobally()
        {
            // Press key down
            keybd_event((byte)System.Windows.Forms.Keys.CapsLock, 0, 0, 0);
            // Press key up
            keybd_event((byte)System.Windows.Forms.Keys.CapsLock, 0, 0x2, 0);
        }

        public static void SendCharacter(IntPtr hwnd, VirtualInputMethod inputMethod, char c)
        {
            switch (inputMethod)
            {
                case VirtualInputMethod.SendMessage:
                    SendMessage(hwnd, WM_CHAR, c, IntPtr.Zero);
                    break;

                case VirtualInputMethod.PostMessage:
                    PostMessage(hwnd, WM_CHAR, (IntPtr)c, IntPtr.Zero);
                    break;

                default:
                    if (IsSpecialCharacter(c))
                    {
                        if (inputMethod == VirtualInputMethod.SendWait)
                        {
                            System.Windows.Forms.SendKeys.SendWait("{" + c.ToString() + "}");
                        }
                        else
                        {
                            System.Windows.Forms.SendKeys.Send("{" + c.ToString() + "}");
                        }
                    }
                    else
                    {
                        if (inputMethod == VirtualInputMethod.SendWait)
                        {
                            System.Windows.Forms.SendKeys.SendWait(c.ToString());
                        }
                        else
                        {
                            System.Windows.Forms.SendKeys.Send(c.ToString());
                        }
                    }
                    break;
            }
        }

        public static void SendEnter(IntPtr hwnd, VirtualInputMethod inputMethod)
        {
            switch (inputMethod)
            {
                case VirtualInputMethod.SendMessage:
                    SendMessage(hwnd, WM_KEYDOWN, VK_RETURN, IntPtr.Zero);
                    SendMessage(hwnd, WM_KEYUP, VK_RETURN, IntPtr.Zero);
                    break;

                case VirtualInputMethod.PostMessage:
                    PostMessage(hwnd, WM_KEYDOWN, VK_RETURN, IntPtr.Zero);
                    PostMessage(hwnd, WM_KEYUP, VK_RETURN, IntPtr.Zero);
                    break;

                case VirtualInputMethod.SendWait:
                    SetForegroundWindow(hwnd);
                    System.Windows.Forms.SendKeys.SendWait("{ENTER}");
                    break;
            }
        }

        public static void SendTab(IntPtr hwnd, VirtualInputMethod inputMethod)
        {
            switch (inputMethod)
            {
                case VirtualInputMethod.SendMessage:
                    SendMessage(hwnd, WM_KEYDOWN, VK_TAB, IntPtr.Zero);
                    SendMessage(hwnd, WM_KEYUP, VK_TAB, IntPtr.Zero);
                    break;

                case VirtualInputMethod.PostMessage:
                    PostMessage(hwnd, WM_KEYDOWN, (IntPtr)VK_TAB, IntPtr.Zero);
                    PostMessage(hwnd, WM_KEYUP, (IntPtr)VK_TAB, IntPtr.Zero);
                    break;

                case VirtualInputMethod.SendWait:
                    SetForegroundWindow(hwnd);
                    System.Windows.Forms.SendKeys.SendWait("{TAB}");
                    break;
            }
        }

        public static void SendSpace(IntPtr hwnd, VirtualInputMethod inputMethod)
        {
            switch (inputMethod)
            {
                case VirtualInputMethod.SendMessage:
                    SendMessage(hwnd, WM_KEYDOWN, VK_SPACE, IntPtr.Zero);
                    SendMessage(hwnd, WM_KEYUP, VK_SPACE, IntPtr.Zero);
                    break;

                case VirtualInputMethod.PostMessage:
                    PostMessage(hwnd, WM_KEYDOWN, (IntPtr)VK_SPACE, IntPtr.Zero);
                    PostMessage(hwnd, WM_KEYUP, (IntPtr)VK_SPACE, IntPtr.Zero);
                    break;

                case VirtualInputMethod.SendWait:
                    SetForegroundWindow(hwnd);
                    System.Windows.Forms.SendKeys.SendWait(" ");
                    break;
            }
        }

        public static void ClearSteamUserDataFolder(string steamPath, int sleepTime, int maxRetry)
        {
            WindowHandle steamLoginWindow = GetLegacySteamLoginWindow();
            int waitCount = 0;

            while (steamLoginWindow.IsValid && waitCount < maxRetry)
            {
                Thread.Sleep(sleepTime);
                waitCount++;
            }

            string path = steamPath + "\\userdata";

            if (Directory.Exists(path))
            {
                Console.WriteLine("Deleting userdata files...");
                Directory.Delete(path, true);
                Console.WriteLine("userdata files deleted!");
            }
            else
            {
                Console.WriteLine("userdata directory not found.");
            }
        }

        public static bool IsSpecialCharacter(char c)
        {
            foreach (char special in specialChars)
            {
                if (c.Equals(special))
                {
                    return true;
                }
            }

            return false;
        }

        public static string Generate2FACode(string shared_secret)
        {
            SteamGuardAccount authAccount = new SteamGuardAccount { SharedSecret = shared_secret };
            string code = authAccount.GenerateSteamGuardCode();
            return code;
        }
    }
}
