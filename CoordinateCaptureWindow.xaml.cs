using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace AutoClickTool
{
    /// <summary>
    /// CoordinateCaptureWindow.xaml 的交互逻辑
    /// </summary>
    public partial class CoordinateCaptureWindow : Window
    {
        // Win32 API 导入
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        // 捕获的坐标
        public int CapturedX { get; private set; }
        public int CapturedY { get; private set; }

        // 键盘钩子
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private IntPtr _hookID = IntPtr.Zero;

        public CoordinateCaptureWindow()
        {
            InitializeComponent();
            
            // 设置窗口置顶
            Topmost = true;
            
            // 注册键盘事件
            KeyDown += CoordinateCaptureWindow_KeyDown;
        }

        private void CoordinateCaptureWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F2)
            {
                CaptureCurrentCoordinates();
            }
        }

        private void CaptureCurrentCoordinates()
        {
            POINT point;
            if (GetCursorPos(out point))
            {
                CapturedX = point.X;
                CapturedY = point.Y;
                
                // 更新UI
                XCoordinateTextBox.Text = CapturedX.ToString();
                YCoordinateTextBox.Text = CapturedY.ToString();
            }
        }

        private void CaptureButton_Click(object sender, RoutedEventArgs e)
        {
            CaptureCurrentCoordinates();
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}