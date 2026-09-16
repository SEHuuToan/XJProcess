using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Net.Http;

namespace XJProcess.service
{
    class ScanService
    {
        public delegate void ScanerDelegate(ScanerCodes codes);
        public static event ScanerDelegate ScanerEvent;
        private static int hKeyboardHook = 0;
        private static ScanerCodes codes = new ScanerCodes();

        private static HookProc hookproc;
        delegate int HookProc(int nCode, Int32 wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern int SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hInstance, int threadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern bool UnhookWindowsHookEx(int idHook);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern int CallNextHookEx(int idHook, int nCode, Int32 wParam, IntPtr lParam);

        [DllImport("user32", EntryPoint = "GetKeyNameText")]
        private static extern int GetKeyNameText(int IParam, StringBuilder lpBuffer, int nSize);

        [DllImport("user32", EntryPoint = "GetKeyboardState")]
        private static extern int GetKeyboardState(byte[] pbKeyState);

        [DllImport("user32", EntryPoint = "ToAscii")]
        private static extern bool ToAscii(int VirtualKey, int ScanCode, byte[] lpKeySate, ref uint lpChar, int uFlags);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetModuleHandle(string name);

        public ScanService()
        {
        }

        public static bool Start()
        {
            if (hKeyboardHook == 0)
            {
                hookproc = new HookProc(KeyboardHookProc);
                IntPtr modulePtr = GetModuleHandle(Process.GetCurrentProcess().MainModule.ModuleName);
                hKeyboardHook = SetWindowsHookEx(13, hookproc, modulePtr, 0);
            }
            return (hKeyboardHook != 0);
        }

        public static bool Stop()
        {
            if (hKeyboardHook != 0)
            {
                bool retKeyboard = UnhookWindowsHookEx(hKeyboardHook);
                hKeyboardHook = 0;
                return retKeyboard;
            }
            return true;
        }

        private static int KeyboardHookProc(int nCode, Int32 wParam, IntPtr lParam)
        {
            EventMsg msg = (EventMsg)Marshal.PtrToStructure(lParam, typeof(EventMsg));
            codes.Add(msg);

            // Kiểm tra điều kiện quét xong mã đơn
            if (ScanerEvent != null && msg.message == 13 && msg.paramH > 0 && codes.Result.Length > 15
                && !string.IsNullOrEmpty(codes.Result))
            {
                ScanerEvent(codes);

                // Lấy data chuẩn xác để tiến hành gọi API ngầm
                string scannedData = codes.Result;
                Task.Run(async () =>
                {
                    await CallApiProcessOrderAsync(scannedData);
                });
            }
            return CallNextHookEx(hKeyboardHook, nCode, wParam, lParam);
        }

        /// <summary>
        /// Hàm gọi API xử lý mã đơn hàng sau khi quét thành công
        /// </summary>
        private static async Task CallApiProcessOrderAsync(string orderCode)
        {
            try
            {
                // TODO: Viết code gọi API thực tế của bạn ở đây
                // Ví dụ:
                // using (var client = new HttpClient())
                // {
                //     var response = await client.GetAsync($"https://api.yourdomain.com/check?code={orderCode}");
                //     if (response.IsSuccessStatusCode)
                //     {
                //         string result = await response.Content.ReadAsStringAsync();
                //         // Xử lý kết quả trả về
                //     }
                // }

                Console.WriteLine($"Đã gọi API thành công cho mã đơn: {orderCode}");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi gọi API: {ex.Message}");
            }
        }

        public class ScanerCodes
        {
            private int ts = 100;
            private List<List<EventMsg>> _keys = new List<List<EventMsg>>();
            private List<int> _keydown = new List<int>();
            private List<string> _result = new List<string>();
            private DateTime _last = DateTime.Now;
            private byte[] _state = new byte[256];
            private string _key = string.Empty;
            private string _cur = string.Empty;

            public EventMsg Event
            {
                get
                {
                    if (_keys.Count == 0)
                    {
                        return new EventMsg();
                    }
                    else
                    {
                        return _keys[_keys.Count - 1][_keys[_keys.Count - 1].Count - 1];
                    }
                }
            }

            public List<int> KeyDowns => _keydown;
            public DateTime LastInput => _last;
            public byte[] KeyboardState => _state;
            public int KeyDownCount => _keydown.Count;

            public string Result
            {
                get
                {
                    if (_result.Count > 0)
                    {
                        return _result[_result.Count - 1].Trim();
                    }
                    else
                    {
                        return null;
                    }
                }
            }

            public string CurrentKey => _key;
            public string CurrentChar => _cur;
            public bool isShift => _keydown.Contains(160);

            public void Add(EventMsg msg)
            {
                if (_keys.Count == 0)
                {
                    _keys.Add(new List<EventMsg>());
                    _keys[0].Add(msg);
                    _result.Add(string.Empty);
                }
                else if (_keydown.Count > 0)
                {
                    _keys[_keys.Count - 1].Add(msg);
                }
                else if (((TimeSpan)(DateTime.Now - _last)).TotalMilliseconds < ts)
                {
                    _keys[_keys.Count - 1].Add(msg);
                }
                else
                {
                    _keys.Add(new List<EventMsg>());
                    _keys[_keys.Count - 1].Add(msg);
                    _result.Add(string.Empty);
                }

                _last = DateTime.Now;

                if (msg.paramH == 0 && !_keydown.Contains(msg.message))
                {
                    _keydown.Add(msg.message);
                }

                if (msg.paramH > 0 && _keydown.Contains(msg.message))
                {
                    _keydown.Remove(msg.message);
                }

                int v = msg.message & 0xff;
                int c = msg.paramL & 0xff;
                StringBuilder strKeyName = new StringBuilder(500);
                if (GetKeyNameText(c * 65536, strKeyName, 255) > 0)
                {
                    _key = strKeyName.ToString().Trim(new char[] { ' ', '\0' });
                    GetKeyboardState(_state);
                    if (_key.Length == 1 && msg.paramH == 0)
                    {
                        _cur = ShiftChar(_key, isShift, _state).ToString();
                        _result[_result.Count - 1] += _cur;
                    }
                    else
                    {
                        _cur = string.Empty;
                    }
                }
            }

            private char ShiftChar(string k, bool isShiftDown, byte[] state)
            {
                bool capslock = state[0x14] == 1;
                bool numlock = state[0x90] == 1;
                bool scrolllock = state[0x91] == 1;
                bool shiftdown = state[0xa0] == 1;
                char chr = (capslock ? k.ToUpper() : k.ToLower()).ToCharArray()[0];
                if (isShiftDown)
                {
                    if (chr >= 'a' && chr <= 'z')
                    {
                        chr = (char)((int)chr - 32);
                    }
                    else if (chr >= 'A' && chr <= 'Z')
                    {
                        chr = (char)((int)chr + 32);
                    }
                    else
                    {
                        string s = "`1234567890-=[];',./";
                        string u = "~!@#$%^&*()_+{}:\"<>?";
                        if (s.IndexOf(chr) >= 0)
                        {
                            return (u.ToCharArray())[s.IndexOf(chr)];
                        }
                    }
                }
                return chr;
            }
        }

        public struct EventMsg
        {
            public int message;
            public int paramL;
            public int paramH;
            public int Time;
            public int hwnd;
        }
    }
}