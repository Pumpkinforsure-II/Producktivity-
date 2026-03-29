using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Producktivity
{
    static class Program
    {
        [DllImport("user32.dll")]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
            int X, int Y, int cx, int cy, uint uFlags);

        const uint SWP_NOMOVE = 0x0002;
        const uint SWP_NOSIZE = 0x0001;

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

            var sm = new StickerManager();
            sm.Load();

            var stats = new StatsManager();
            stats.Load();

            AppBlocker.LoadConfig();

            var duck = new Form1();
            var nest = new NestForm(duck, sm, stats);
            var chat = new ChatForm();

            duck.SetNestForm(nest);
            duck.SetChatForm(chat);
            duck.SetStickerManager(sm);
            duck.SetStatsManager(stats);

            chat.UpdateDuckName(duck.DuckName);

            duck.StartAnimation();

            var blocker = new AppBlocker();
            blocker.Start(duck);
            blocker.SetStatsManager(stats);

            nest.Show();
            duck.Show();

            int screenW = Screen.PrimaryScreen.Bounds.Width;
            nest.Location = new System.Drawing.Point(screenW / 2 - 65, 371);
            duck.Location = new System.Drawing.Point(duck.Location.X, 600);

            SetWindowPos(nest.Handle, duck.Handle, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);

            Application.Run(duck);

            blocker.Stop();
            stats.Save();
        }
    }
}