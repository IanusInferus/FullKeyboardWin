using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace FullKeyboardWin
{
    public class Program
    {
        private static Mutex mutex = new Mutex(true, "{1D579EA4-2D42-42BE-8A60-0EE002667A8F}");
        private static MainWindow MainWindow;

        public static void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            if (MainWindow == null)
            {
                MessageBox.Show(e.Exception.ToString(), "Error");
            }
            else
            {
                MessageBox.Show(MainWindow, e.Exception.ToString(), "Error");
            }
            e.Handled = true;
        }

        [STAThread]
        public static int Main(String[] args)
        {
            if (!mutex.WaitOne(TimeSpan.Zero, true))
            {
                MessageBox.Show("Running. Press Win+Shift+O to show.", "Error");
                return 1;
            }

            if (!Debugger.IsAttached)
            {
                try
                {
                    var a = new App();
                    MainWindow = new MainWindow();
                    a.DispatcherUnhandledException += App_DispatcherUnhandledException;
                    a.Run(MainWindow);
                    Environment.Exit(0);
                    return 0;
                }
                catch (Exception ex)
                {
                    if (MainWindow == null)
                    {
                        MessageBox.Show(ex.ToString(), "Error");
                    }
                    else
                    {
                        MessageBox.Show(MainWindow, ex.ToString(), "Error");
                    }
                    Environment.Exit(-1);
                    return -1;
                }
            }
            else
            {
                var Success = false;
                try
                {
                    var a = new App();
                    MainWindow = new MainWindow();
                    a.Run(MainWindow);
                    Success = true;
                    Environment.Exit(0);
                    return 0;
                }
                finally
                {
                    if (!Success)
                    {
                        Environment.Exit(-1);
                    }
                }
            }
        }
    }
}
