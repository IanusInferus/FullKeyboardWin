using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using WindowsInput;
using WindowsInput.Native;
using System.Windows.Controls.Primitives;

namespace FullKeyboardWin
{
    public partial class MainWindow : Window
    {
        private InputSimulator inputSimulator;
        private WindowsInputDeviceStateAdaptor inputStateAdapter;
        private HwndSource hwndSource;
        private System.Windows.Forms.NotifyIcon notifyIcon = null;

        public MainWindow()
        {
            InitializeComponent();
            inputSimulator = new InputSimulator();
            inputStateAdapter = new WindowsInputDeviceStateAdaptor();
        }

        private void Window_Loaded(object sender, RoutedEventArgs rea)
        {
            var desktopWorkingArea = SystemParameters.WorkArea;
            if (desktopWorkingArea.Width > 0)
            {
                var Height = Math.Min(this.Height / this.Width * desktopWorkingArea.Width, desktopWorkingArea.Height / 2);
                this.Width = desktopWorkingArea.Width;
                this.Height = Height;
                this.Left = 0;
                this.Top = desktopWorkingArea.Bottom - Height;
            }
            else
            {
                this.Left = (desktopWorkingArea.Right - this.Width) / 2;
                this.Top = desktopWorkingArea.Bottom - this.Height;
            }

            var helper = new WindowInteropHelper(this);
            PInvokeWrapper.SetWindowNoFocus(helper.Handle);
            hwndSource = HwndSource.FromHwnd(helper.Handle);

            //Win+Ctrl+O is taken by explorer.exe, but we can workaround by killing explorer.exe and registering before restarting it
            PInvokeWrapper.TryRegisterHotKey(hwndSource, 9000, PInvokeWrapper.ModifierCode.MOD_WIN | PInvokeWrapper.ModifierCode.MOD_CONTROL, VirtualKeyCode.VK_O, () =>
            {
                this.Visibility = this.IsVisible ? Visibility.Hidden : Visibility.Visible;
            });

            PInvokeWrapper.TryRegisterHotKey(hwndSource, 9001, PInvokeWrapper.ModifierCode.MOD_WIN | PInvokeWrapper.ModifierCode.MOD_SHIFT, VirtualKeyCode.VK_O, () =>
            {
                this.Visibility = this.IsVisible ? Visibility.Hidden : Visibility.Visible;
            });

            notifyIcon = new System.Windows.Forms.NotifyIcon();
            notifyIcon.Click += (s, e) =>
            {
                this.Visibility = this.IsVisible ? Visibility.Hidden : Visibility.Visible;
            };
            notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location);
            notifyIcon.Visible = true;

            var modifierKeys = new HashSet<VirtualKeyCode> { VirtualKeyCode.LCONTROL, VirtualKeyCode.RCONTROL, VirtualKeyCode.LSHIFT, VirtualKeyCode.RSHIFT, VirtualKeyCode.LMENU, VirtualKeyCode.RMENU, VirtualKeyCode.LWIN, VirtualKeyCode.RWIN };
            var lockKeys = new HashSet<VirtualKeyCode> { VirtualKeyCode.CAPITAL, VirtualKeyCode.NUMLOCK, VirtualKeyCode.SCROLL, VirtualKeyCode.VOLUME_MUTE };

            foreach (var b in FindLogicalChildren<ToggleButton>(this))
            {
                if ((b.Tag is String s) && (s != ""))
                {
                    var keyCode = (VirtualKeyCode)Enum.Parse(typeof(VirtualKeyCode), s);
                    var isModifier = modifierKeys.Contains(keyCode);
                    var isLock = lockKeys.Contains(keyCode);

                    Action keyDown;
                    Action keyUp;

                    if (isLock)
                    {
                        Func<bool> getToggling = () =>
                        {
                            if (keyCode == VirtualKeyCode.VOLUME_MUTE)
                            {
                                return AudioManager.GetMasterVolumeMute();
                            }
                            else
                            {
                                return inputStateAdapter.IsTogglingKeyInEffect(keyCode);
                            }
                        };
                        var isChecked = getToggling();
                        b.IsChecked = isChecked;

                        keyDown = () =>
                        {
                            inputSimulator.Keyboard.KeyDown(keyCode);
                            isChecked = getToggling();
                            b.IsChecked = isChecked;
                        };
                        keyUp = () =>
                        {
                            inputSimulator.Keyboard.KeyUp(keyCode);
                            isChecked = getToggling();
                            b.IsChecked = isChecked;
                        };
                    }
                    else
                    {
                        keyDown = () =>
                        {
                            inputSimulator.Keyboard.KeyDown(keyCode);
                            b.IsChecked = true;
                        };
                        keyUp = () =>
                        {
                            inputSimulator.Keyboard.KeyUp(keyCode);
                            b.IsChecked = false;
                        };
                    }

                    var isPressing = false;
                    var timer = new System.Windows.Threading.DispatcherTimer();
                    timer.Tick += (_, e) =>
                    {
                        timer.Stop();
                        keyDown();
                        timer.Interval = TimeSpan.FromMilliseconds(50);
                        timer.Start();
                    };
                    b.PreviewMouseDown += (_, e) =>
                    {
                        if (e.StylusDevice != null)
                        {
                            e.Handled = true;
                            return;
                        }
                        isPressing = true;
                        keyDown();
                        e.Handled = true;
                        if (!(isModifier || isLock))
                        {
                            timer.Interval = TimeSpan.FromMilliseconds(200);
                            timer.Start();
                        }
                    };
                    b.PreviewMouseUp += (_, e) =>
                    {
                        if (e.StylusDevice != null)
                        {
                            e.Handled = true;
                            return;
                        }
                        timer.Stop();
                        if (isPressing)
                        {
                            keyUp();
                            isPressing = false;
                            e.Handled = true;
                        }
                    };
                    b.MouseLeave += (_, e) =>
                    {
                        if (e.StylusDevice != null)
                        {
                            e.Handled = true;
                            return;
                        }
                        timer.Stop();
                        if (isPressing)
                        {
                            keyUp();
                            isPressing = false;
                            e.Handled = true;
                        }
                    };
                    b.PreviewTouchDown += (_, e) =>
                    {
                        isPressing = true;
                        keyDown();
                        e.Handled = true;
                        if (!(isModifier || isLock))
                        {
                            timer.Interval = TimeSpan.FromMilliseconds(200);
                            timer.Start();
                        }
                    };
                    b.PreviewTouchUp += (_, e) =>
                    {
                        timer.Stop();
                        if (isPressing)
                        {
                            keyUp();
                            isPressing = false;
                            e.Handled = true;
                        }
                    };
                    b.TouchLeave += (_, e) =>
                    {
                        timer.Stop();
                        if (isPressing)
                        {
                            keyUp();
                            isPressing = false;
                            e.Handled = true;
                        }
                    };
                }
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            notifyIcon.Visible = false;
            notifyIcon.Dispose();
            hwndSource.Dispose();
        }

        private static IEnumerable<T> FindLogicalChildren<T>(DependencyObject obj) where T : DependencyObject
        {
            if (obj != null)
            {
                if (obj is T)
                {
                    yield return obj as T;
                }

                foreach (DependencyObject child in LogicalTreeHelper.GetChildren(obj).OfType<DependencyObject>())
                {
                    foreach (T c in FindLogicalChildren<T>(child))
                    {
                        yield return c;
                    }
                }
            }
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void ButtonHide_Click(object sender, RoutedEventArgs e)
        {
            this.Visibility = Visibility.Hidden;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
            {
                this.DragMove();
            }
        }
    }
}
