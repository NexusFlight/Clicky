using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using System.Drawing;
using System.Diagnostics;
using System.Threading;

namespace clicky
{
    class Program
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);
        //Mouse actions
        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const int MOUSEEVENTF_RIGHTUP = 0x10;
        static Dictionary<Key, Point> points = new Dictionary<Key, Point>();
        static bool latch = false;

        [STAThread]
        static void Main(string[] args)
        {

            var tcs = new TaskCompletionSource<object>();
            var thread = new Thread(() =>
            {
                try
                {
                    clicker();
                    tcs.SetResult(null);
                }
                catch (Exception e)
                {
                    tcs.SetException(e);
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            LowLevelKeyboardListener kbh = new LowLevelKeyboardListener();
            kbh.OnKeyPressed += kbh_OnKeyPressed;
            kbh.HookKeyboard();
            Application.Run();
            
            kbh.UnHookKeyboard();
            

        }
        
        static void clicker()
        {
            Console.WriteLine(Thread.CurrentThread.GetApartmentState());
            bool keylatch = false;
            while (!Keyboard.IsKeyDown(Key.Escape))
            {
                if(latch)
                {
                    if (points.ContainsKey(Key.Z)) {
                        points.TryGetValue(Key.Z, out System.Drawing.Point mainClick);
                        System.Windows.Forms.Cursor.Position = mainClick;
                        mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, (uint)mainClick.X, (uint)mainClick.Y, 0, 0);
                    }
                }
                if (Keyboard.IsKeyDown(Key.X) && !keylatch)
                {
                    latch = !latch;
                    keylatch = true;
                }
                if (!Keyboard.IsKeyDown(Key.X) && keylatch)
                {
                    keylatch = false;
                }
            }
            Environment.Exit(0);
        }

        static void kbh_OnKeyPressed(object sender, KeyPressedArgs e)
        {
            int x = System.Windows.Forms.Cursor.Position.X;
            int y = System.Windows.Forms.Cursor.Position.Y;
            if (!latch && (Key)e.KeyPressed != Key.X && (Key)e.KeyPressed != Key.Escape)
            {
                if (points.ContainsKey((Key)e.KeyPressed))
                {
                    Console.WriteLine("Point deleted, Press key again to remap");
                    points.Remove((Key)e.KeyPressed);
                }
                else
                {
                    points.Add((Key)e.KeyPressed, new System.Drawing.Point(x, y));
                    Console.WriteLine("New point added for key {0}", (Key)e.KeyPressed);
                }
            }
            else
            {
                if (points.ContainsKey((Key)e.KeyPressed))
                {
                    points.TryGetValue((Key)e.KeyPressed, out System.Drawing.Point mainClick);
                    System.Windows.Forms.Cursor.Position = mainClick;
                    mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, (uint)mainClick.X, (uint)mainClick.Y, 0, 0);
                }
            }
        }



    }



    public class LowLevelKeyboardListener
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        public event EventHandler<KeyPressedArgs> OnKeyPressed;

        private LowLevelKeyboardProc _proc;
        private IntPtr _hookID = IntPtr.Zero;

        public LowLevelKeyboardListener()
        {
            _proc = HookCallback;
        }

        public void HookKeyboard()
        {
            _hookID = SetHook(_proc);
        }

        public void UnHookKeyboard()
        {
            UnhookWindowsHookEx(_hookID);
        }

        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);

                if (OnKeyPressed != null) { OnKeyPressed(this, new KeyPressedArgs(KeyInterop.KeyFromVirtualKey(vkCode))); }
            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }
    }

    public class KeyPressedArgs : EventArgs
    {
        public Key KeyPressed { get; private set; }

        public KeyPressedArgs(Key key)
        {
            KeyPressed = key;
        }
    }



}
