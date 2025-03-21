using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace AutoClickTool
{
    /// <summary>
    /// 窗口操作辅助类
    /// </summary>
    public static class WindowHelper
    {
        #region Win32 API

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, int dwExtraInfo);
        
        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        private static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindowVisible(IntPtr hWnd);
        
        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        
        [DllImport("user32.dll")]
        private static extern bool SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        
        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr hWnd);
        
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        
        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr ProcessId);
        
        [DllImport("user32.dll")]
        private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        // 鼠标事件标志
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
        private const uint MOUSEEVENTF_ABSOLUTE = 0x8000;
        
        // ShowWindow命令
        private const int SW_SHOW = 5;
        private const int SW_RESTORE = 9;
        private const int SW_SHOWNOACTIVATE = 4;
        
        // Windows消息常量
        private const uint WM_LBUTTONDOWN = 0x0201;
        private const uint WM_LBUTTONUP = 0x0202;
        private const uint WM_LBUTTONDBLCLK = 0x0203;
        private const uint WM_RBUTTONDOWN = 0x0204;
        private const uint WM_RBUTTONUP = 0x0205;
        private const uint WM_MBUTTONDOWN = 0x0207;
        private const uint WM_MBUTTONUP = 0x0208;
        
        // 坐标打包宏
        private static IntPtr MakeLParam(int x, int y) { return (IntPtr)((y << 16) | (x & 0xFFFF)); }

        #endregion

        /// <summary>
        /// 获取所有可见窗口的标题
        /// </summary>
        /// <returns>窗口标题列表</returns>
        public static List<string> GetAllWindowTitles()
        {
            List<string> titles = new List<string>();
            EnumWindows((hWnd, lParam) =>
            {
                if (IsWindowVisible(hWnd))
                {
                    StringBuilder sb = new StringBuilder(256);
                    GetWindowText(hWnd, sb, 256);
                    string title = sb.ToString().Trim();
                    if (!string.IsNullOrEmpty(title))
                    {
                        titles.Add(title);
                    }
                }
                return true;
            }, IntPtr.Zero);
            return titles;
        }

        /// <summary>
        /// 根据窗口标题获取窗口句柄
        /// </summary>
        /// <param name="windowTitle">窗口标题</param>
        /// <returns>窗口句柄</returns>
        public static IntPtr GetWindowHandle(string windowTitle)
        {
            return FindWindow(null, windowTitle);
        }

        /// <summary>
        /// 获取窗口分辨率
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <returns>窗口分辨率</returns>
        public static Size GetWindowSize(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero)
                return new Size(0, 0);

            RECT rect;
            if (GetWindowRect(hWnd, out rect))
            {
                int width = rect.Right - rect.Left;
                int height = rect.Bottom - rect.Top;
                return new Size(width, height);
            }

            return new Size(0, 0);
        }

        /// <summary>
        /// 验证窗口分辨率是否符合要求
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <param name="expectedResolution">期望的分辨率</param>
        /// <returns>是否符合要求</returns>
        public static bool VerifyWindowResolution(IntPtr hWnd, string expectedResolution)
        {
            if (hWnd == IntPtr.Zero)
                return false;

            Size size = GetWindowSize(hWnd);
            string[] parts = expectedResolution.Split('x');
            if (parts.Length != 2)
                return false;

            if (int.TryParse(parts[0], out int expectedWidth) && int.TryParse(parts[1], out int expectedHeight))
            {
                // 允许一定的误差
                const int tolerance = 10;
                return Math.Abs(size.Width - expectedWidth) <= tolerance && Math.Abs(size.Height - expectedHeight) <= tolerance;
            }

            return false;
        }

        /// <summary>
        /// 设置窗口为前台窗口
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <returns>是否成功</returns>
        public static bool SetForeground(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero || !IsWindow(hWnd))
                return false;

            // 确保窗口不是最小化状态
            ShowWindow(hWnd, SW_RESTORE);
            
            // 获取当前线程和目标窗口线程
            IntPtr currentForegroundWindow = GetForegroundWindow();
            uint currentThreadId = GetWindowThreadProcessId(currentForegroundWindow, IntPtr.Zero);
            uint targetThreadId = GetWindowThreadProcessId(hWnd, IntPtr.Zero);
            
            bool threadAttached = false;
            
            try
            {
                // 如果线程不同，则关联线程输入
                if (currentThreadId != targetThreadId)
                {
                    threadAttached = AttachThreadInput(currentThreadId, targetThreadId, true);
                }
                
                // 尝试设置前台窗口
                bool result = SetForegroundWindow(hWnd);
                
                // 添加重试机制，最多尝试3次
                int retryCount = 0;
                while (!IsWindowForeground(hWnd) && retryCount < 3)
                {
                    // 短暂延迟后再次尝试
                    Thread.Sleep(100);
                    
                    // 再次确保窗口可见
                    ShowWindow(hWnd, SW_SHOW);
                    
                    result = SetForegroundWindow(hWnd);
                    retryCount++;
                }
                
                // 验证是否成功设置为前台窗口
                return IsWindowForeground(hWnd);
            }
            finally
            {
                // 如果线程已关联，则分离线程输入
                if (threadAttached)
                {
                    AttachThreadInput(currentThreadId, targetThreadId, false);
                }
            }
        }

        /// <summary>
        /// 点击窗口标题栏以获取焦点
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <returns>是否成功点击标题栏</returns>
        public static bool ClickWindowTitle(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero || !IsWindow(hWnd))
                return false;
                
            // 获取窗口位置
            RECT rect;
            if (!GetWindowRect(hWnd, out rect))
                return false;
                
            // 计算标题栏中心点坐标（标题栏通常在窗口顶部，高度约为30像素）
            int titleBarHeight = 30;
            int titleX = rect.Left + (rect.Right - rect.Left) / 2;
            int titleY = rect.Top + titleBarHeight / 2;
            
            // 设置鼠标位置到标题栏中心
            SetCursorPos(titleX, titleY);
            
            // 执行左键点击
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
            Thread.Sleep(50);
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
            
            // 等待窗口激活
            Thread.Sleep(200);
            
            // 验证是否成功设置为前台窗口
            return IsWindowForeground(hWnd);
        }
        
        /// <summary>
        /// 模拟鼠标点击
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <param name="x">相对窗口的X坐标</param>
        /// <param name="y">相对窗口的Y坐标</param>
        /// <param name="clickType">点击类型</param>
        /// <returns>操作后的鼠标位置</returns>
        public static POINT SimulateClick(IntPtr hWnd, int x, int y, ClickType clickType)
        {
            if (hWnd == IntPtr.Zero || !IsWindow(hWnd))
            {
                // 返回当前鼠标位置
                POINT currentPos;
                GetCursorPos(out currentPos);
                return currentPos;
            }
            
            // 检查窗口是否是前台窗口
            bool isForeground = IsWindowForeground(hWnd);

            if (isForeground)
            {
                // 如果是前台窗口，使用传统方式点击（更可靠）
                return SimulateClickForeground(hWnd, x, y, clickType);
            }
            else
            {
                // 如果不是前台窗口，使用消息方式点击
                return SimulateClickBackground(hWnd, x, y, clickType);
            }
        }

        /// <summary>
        /// 获取当前鼠标位置
        /// </summary>
        /// <param name="lpPoint">接收鼠标位置的结构体</param>
        /// <returns>是否成功获取</returns>
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(out POINT lpPoint);
        
        /// <summary>
        /// 对前台窗口模拟鼠标点击（传统方式）
        /// </summary>
        /// <returns>操作后的鼠标位置</returns>
        private static POINT SimulateClickForeground(IntPtr hWnd, int x, int y, ClickType clickType)
        {
            // 获取当前鼠标位置
            POINT currentMousePos;
            GetCursorPos(out currentMousePos);
            
            // 将窗口坐标转换为屏幕坐标
            POINT point = new POINT { X = x, Y = y };
            ClientToScreen(hWnd, ref point);
            
            // 记录当前鼠标位置，以便操作后恢复（可选）
            int originalX = currentMousePos.X;
            int originalY = currentMousePos.Y;

            // 确保窗口是前台窗口
            if (!IsWindowForeground(hWnd))
            {
                // 尝试设置为前台窗口
                SetForegroundWindow(hWnd);
                
                // 等待窗口激活，给系统足够时间处理焦点变化
                Thread.Sleep(200);
                
                // 如果仍然不是前台窗口，再次尝试
                if (!IsWindowForeground(hWnd))
                {
                    SetForegroundWindow(hWnd);
                    Thread.Sleep(200);
                }
            }
            else
            {
                // 窗口已经是前台，短暂等待以确保稳定
                Thread.Sleep(50);
            }

            // 再次获取当前鼠标位置，确保获取最新位置
            GetCursorPos(out currentMousePos);

            // 强制移动鼠标到目标位置，确保每次点击都在正确的坐标上
            SetCursorPos(point.X, point.Y);
            
            // 等待鼠标移动完成
            Thread.Sleep(100);
            
            // 执行点击
            switch (clickType)
            {
                case ClickType.LeftClick:
                    // 再次确认鼠标位置
                    SetCursorPos(point.X, point.Y);
                    Thread.Sleep(50);
                    
                    mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_ABSOLUTE, point.X, point.Y, 0, 0);
                    Thread.Sleep(50);
                    mouse_event(MOUSEEVENTF_LEFTUP | MOUSEEVENTF_ABSOLUTE, point.X, point.Y, 0, 0);
                    break;

                case ClickType.LeftDoubleClick:
                    // 再次确认鼠标位置
                    SetCursorPos(point.X, point.Y);
                    Thread.Sleep(50);
                    
                    // 第一次点击
                    mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_ABSOLUTE, point.X, point.Y, 0, 0);
                    Thread.Sleep(50);
                    mouse_event(MOUSEEVENTF_LEFTUP | MOUSEEVENTF_ABSOLUTE, point.X, point.Y, 0, 0);
                    
                    // 短暂延迟
                    Thread.Sleep(100);
                    
                    // 再次确认鼠标位置，确保双击时鼠标没有移动
                    SetCursorPos(point.X, point.Y);
                    Thread.Sleep(50);
                    
                    // 第二次点击
                    mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_ABSOLUTE, point.X, point.Y, 0, 0);
                    Thread.Sleep(50);
                    mouse_event(MOUSEEVENTF_LEFTUP | MOUSEEVENTF_ABSOLUTE, point.X, point.Y, 0, 0);
                    break;

                case ClickType.RightClick:
                    // 再次确认鼠标位置
                    SetCursorPos(point.X, point.Y);
                    Thread.Sleep(50);
                    
                    mouse_event(MOUSEEVENTF_RIGHTDOWN | MOUSEEVENTF_ABSOLUTE, point.X, point.Y, 0, 0);
                    Thread.Sleep(50);
                    mouse_event(MOUSEEVENTF_RIGHTUP | MOUSEEVENTF_ABSOLUTE, point.X, point.Y, 0, 0);
                    break;
                    
                case ClickType.MiddleClick:
                    // 再次确认鼠标位置
                    SetCursorPos(point.X, point.Y);
                    Thread.Sleep(50);
                    
                    mouse_event(MOUSEEVENTF_MIDDLEDOWN | MOUSEEVENTF_ABSOLUTE, point.X, point.Y, 0, 0);
                    Thread.Sleep(50);
                    mouse_event(MOUSEEVENTF_MIDDLEUP | MOUSEEVENTF_ABSOLUTE, point.X, point.Y, 0, 0);
                    break;
            }
            
            // 操作完成后，获取最终鼠标位置
            POINT finalMousePos;
            GetCursorPos(out finalMousePos);
            
            // 返回最终鼠标位置
            return finalMousePos;
        }

        /// <summary>
        /// 对非前台窗口模拟鼠标点击（消息方式）
        /// </summary>
        /// <returns>操作后的鼠标位置</returns>
        private static POINT SimulateClickBackground(IntPtr hWnd, int x, int y, ClickType clickType)
        {
            // 获取当前鼠标位置
            POINT currentMousePos;
            GetCursorPos(out currentMousePos);
            
            // 将窗口坐标转换为屏幕坐标
            POINT point = new POINT { X = x, Y = y };
            ClientToScreen(hWnd, ref point);
            
            // 记录当前鼠标位置，以便操作后恢复（可选）
            int originalX = currentMousePos.X;
            int originalY = currentMousePos.Y;
            
            // 再次获取当前鼠标位置，确保获取最新位置
            GetCursorPos(out currentMousePos);
            
            // 强制移动鼠标到目标位置，确保每次点击都在正确的坐标上
            SetCursorPos(point.X, point.Y);
            
            // 等待鼠标移动完成
            Thread.Sleep(100);
            
            // 创建坐标参数
            IntPtr lParam = MakeLParam(x, y);

            // 根据点击类型发送不同的消息
            switch (clickType)
            {
                case ClickType.LeftClick:
                    // 再次确认鼠标位置
                    SetCursorPos(point.X, point.Y);
                    Thread.Sleep(50);
                    
                    // 发送鼠标按下和抬起消息
                    SendMessage(hWnd, WM_LBUTTONDOWN, IntPtr.Zero, lParam);
                    Thread.Sleep(50);
                    SendMessage(hWnd, WM_LBUTTONUP, IntPtr.Zero, lParam);
                    break;

                case ClickType.LeftDoubleClick:
                    // 再次确认鼠标位置
                    SetCursorPos(point.X, point.Y);
                    Thread.Sleep(50);
                    
                    // 发送双击消息
                    SendMessage(hWnd, WM_LBUTTONDOWN, IntPtr.Zero, lParam);
                    Thread.Sleep(50);
                    SendMessage(hWnd, WM_LBUTTONUP, IntPtr.Zero, lParam);
                    Thread.Sleep(50);
                    
                    // 再次确认鼠标位置，确保双击时鼠标没有移动
                    SetCursorPos(point.X, point.Y);
                    Thread.Sleep(50);
                    
                    SendMessage(hWnd, WM_LBUTTONDBLCLK, IntPtr.Zero, lParam);
                    Thread.Sleep(50);
                    SendMessage(hWnd, WM_LBUTTONUP, IntPtr.Zero, lParam);
                    break;

                case ClickType.RightClick:
                    // 再次确认鼠标位置
                    SetCursorPos(point.X, point.Y);
                    Thread.Sleep(50);
                    
                    // 发送右键按下和抬起消息
                    SendMessage(hWnd, WM_RBUTTONDOWN, IntPtr.Zero, lParam);
                    Thread.Sleep(50);
                    SendMessage(hWnd, WM_RBUTTONUP, IntPtr.Zero, lParam);
                    break;
                    
                case ClickType.MiddleClick:
                    // 再次确认鼠标位置
                    SetCursorPos(point.X, point.Y);
                    Thread.Sleep(50);
                    
                    // 发送中键按下和抬起消息
                    SendMessage(hWnd, WM_MBUTTONDOWN, IntPtr.Zero, lParam);
                    Thread.Sleep(50);
                    SendMessage(hWnd, WM_MBUTTONUP, IntPtr.Zero, lParam);
                    break;
            }
            
            // 操作完成后，获取最终鼠标位置
            POINT finalMousePos;
            GetCursorPos(out finalMousePos);
            
            // 返回最终鼠标位置
            return finalMousePos;
        }

        /// <summary>
        /// 检查窗口是否仍然存在并且是前台窗口
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <returns>是否是前台窗口</returns>
        public static bool IsWindowForeground(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero)
                return false;

            IntPtr foregroundWindow = GetForegroundWindow();
            return foregroundWindow == hWnd;
        }
        
        /// <summary>
        /// 检查窗口是否仍然存在
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <returns>窗口是否存在</returns>
        public static bool IsWindowExists(IntPtr hWnd)
        {
            return hWnd != IntPtr.Zero && IsWindow(hWnd);
        }
    }
}