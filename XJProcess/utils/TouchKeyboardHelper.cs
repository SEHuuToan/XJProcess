using System;
using System.Runtime.InteropServices;

namespace XJProcess.utils
{
    public static class TouchKeyboardHelper
    {
        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr ShellExecute(
            IntPtr hwnd,
            string lpOperation,
            string lpFile,
            string lpParameters,
            string lpDirectory,
            int nShowCmd);
        [ComImport, Guid("41B86272-9D88-4179-85C1-61B877303E2F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface ITipInvocation
        {
            void Toggle(IntPtr hwnd);
        }

        public static void Show()
        {
            try
            {
                var uiHostClass = Guid.Parse("688265B3-D726-4C26-80A0-639F41222D89");
                var type = Type.GetTypeFromCLSID(uiHostClass);
                if (type != null)
                {
                    var tipInvocation = (ITipInvocation)Activator.CreateInstance(type);
                    tipInvocation?.Toggle(IntPtr.Zero);
                    return;
                }
            }
            catch
            {
            }

            try
            {
                string tabTipPath = @"C:\Program Files\Common Files\microsoft shared\ink\TabTip.exe";
                IntPtr result = ShellExecute(IntPtr.Zero, "open", tabTipPath, null, null, 5);
                if (result.ToInt64() <= 32)
                {
                    ShellExecute(IntPtr.Zero, "open", "osk.exe", null, null, 5);
                }
            }
            catch
            {
            }
        }
    }
}