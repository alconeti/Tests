using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Tests
{
    public partial class Form1 : Form
    {
        ContextMenu ctx = new ContextMenu();
        IntPtr progman;
        Thread thread;

        public Form1()
        {
            InitializeComponent();
        }

        private static void PrintVisibleWindowHandles(IntPtr hwnd, int maxLevel = -1, int level = 0)
        {
            bool isVisible = W32.IsWindowVisible(hwnd);

            if (isVisible && (maxLevel == -1 || level <= maxLevel))
            {
                StringBuilder className = new StringBuilder(256);
                W32.GetClassName(hwnd, className, className.Capacity);

                StringBuilder windowTitle = new StringBuilder(256);
                W32.GetWindowText(hwnd, windowTitle, className.Capacity);

                Console.WriteLine("".PadLeft(level * 2) + "0x{0:X8} \"{1}\" {2}", hwnd.ToInt64(), windowTitle, className);

                level++;

                // Enumerates all child windows of the current window
                W32.EnumChildWindows(hwnd, new W32.EnumWindowsProc((childhandle, childparamhandle) =>
                {
                    PrintVisibleWindowHandles(childhandle, maxLevel, level);
                    return true;
                }), IntPtr.Zero);
            }
        }
        private static void PrintVisibleWindowHandles(int maxLevel = -1)
        {
            // Enumerates all existing top window handles. This includes open and visible windows, as well as invisible windows.
            W32.EnumWindows(new W32.EnumWindowsProc((tophandle, topparamhandle) =>
            {
                PrintVisibleWindowHandles(tophandle, maxLevel);
                return true;
            }), IntPtr.Zero);
        }

        // Ocultas para que no se muestre en Alt + Tab
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                // turn on WS_EX_TOOLWINDOW style bit
                cp.ExStyle |= 0x80;
                return cp;
            }
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            MaximumSize = new Size(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
            WindowState = FormWindowState.Maximized;
            Visible = false;
            ShowInTaskbar = false;
            notifyIcon1.Visible = true;
            notifyIcon1.Icon = SystemIcons.Warning;
            CreateCtxMenu();
            //Run();
            thread = new Thread(Run);
            thread.Start();

            PrintVisibleWindowHandles(2);
            progman = W32.FindWindow("Progman", null);
            IntPtr result = IntPtr.Zero;
            W32.SendMessageTimeout(progman, 0x052C, new IntPtr(0), IntPtr.Zero, W32.SendMessageTimeoutFlags.SMTO_NORMAL, 1000, out result);
            PrintVisibleWindowHandles(2);

            IntPtr workerw = IntPtr.Zero;

            W32.EnumWindows(new W32.EnumWindowsProc((tophandle, topparamhandle) =>
            {
                IntPtr p = W32.FindWindowEx(tophandle, IntPtr.Zero, "SHELLDLL_DefView", IntPtr.Zero);

                if (p != IntPtr.Zero)
                {
                    // Gets the WorkerW Window after the current one.
                    workerw = W32.FindWindowEx(IntPtr.Zero, tophandle, "WorkerW", IntPtr.Zero);
                }

                return true;
            }), IntPtr.Zero);
            W32.SetParent(this.Handle, workerw);

        }

        private void CreateCtxMenu()
        {
            ctx.MenuItems.Add(new MenuItem("Exit", new EventHandler(exitMenuItem_Click)));
            notifyIcon1.ContextMenu = ctx;
        }
        public void Run()
        {
            try
            {
                this.Invoke((MethodInvoker)delegate
                {
                    if (IsActive(this.Handle))
                    {
                        string file = @"C:\Users\Alcon\Downloads\pixel-room-rainy-night-moewalls-com.mp4";
                        //string file = @"C:\Users\Alcon\Downloads\winter-night-cafe-moewalls-com.mp4";
                        axWindowsMediaPlayer1.URL = file;
                        axWindowsMediaPlayer1.settings.autoStart = true;
                        axWindowsMediaPlayer1.settings.setMode("loop", true);
                        axWindowsMediaPlayer1.uiMode = "None";
                    }
                });
            }
            catch (Exception ex)
            {
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            Cerrar();
        }

        private void exitMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void Cerrar()
        {
            //thread.Abort();
            axWindowsMediaPlayer1.close();
            axWindowsMediaPlayer1.Dispose();
            SetImage();
            Application.Exit();
        }
        public void SetImage()
        {
            W32.SendMessage(progman, 0x0034, 4, IntPtr.Zero);
        }

        // Para saber si la aplicacion está en primer plano
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        public bool IsActive(IntPtr handle)
        {
            IntPtr activeHandle = GetForegroundWindow();
            return (activeHandle == handle);
        }
    }
}

