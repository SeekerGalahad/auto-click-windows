using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace AutoClickTool
{
    /// <summary>
    /// RecordingWindow.xaml 的交互逻辑
    /// </summary>
    public partial class RecordingWindow : Window
    {
        // 鼠标钩子相关
        private const int WH_MOUSE_LL = 14;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_LBUTTONDBLCLK = 0x0203;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_RBUTTONUP = 0x0205;
        private const int WM_MBUTTONDOWN = 0x0207;
        private const int WM_MBUTTONUP = 0x0208;
        
        private IntPtr _hookID = IntPtr.Zero;
        private HookProc _mouseProc;
        
        // 目标窗口
        private IntPtr _targetWindowHandle;
        private string _targetWindowTitle;
        
        // 录制状态
        private bool _isRecording;
        private DateTime _lastActionTime;
        
        // 记录的操作列表
        public List<RecordedAction> RecordedActions { get; private set; }
        
        /// <summary>
        /// 初始化录制窗口
        /// </summary>
        /// <param name="targetWindowHandle">目标窗口句柄</param>
        /// <param name="targetWindowTitle">目标窗口标题</param>
        public RecordingWindow(IntPtr targetWindowHandle, string targetWindowTitle)
        {
            InitializeComponent();
            
            _targetWindowHandle = targetWindowHandle;
            _targetWindowTitle = targetWindowTitle;
            _isRecording = false;
            
            RecordedActions = new List<RecordedAction>();
            
            // 设置目标窗口信息
            TargetWindowTextBlock.Text = _targetWindowTitle;
            
            // 设置键盘事件处理
            KeyDown += RecordingWindow_KeyDown;
        }
        
        /// <summary>
        /// 窗口加载完成事件
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 安装鼠标钩子
            _mouseProc = MouseHookCallback;
            _hookID = SetHook(_mouseProc);
        }
        
        /// <summary>
        /// 窗口关闭事件
        /// </summary>
        private void Window_Closed(object sender, EventArgs e)
        {
            // 如果正在录制，停止录制
            if (_isRecording)
            {
                StopRecording();
            }
            
            // 卸载鼠标钩子
            UnhookWindowsHookEx(_hookID);
        }
        
        /// <summary>
        /// 键盘按下事件处理
        /// </summary>
        private void RecordingWindow_KeyDown(object sender, KeyEventArgs e)
        {
            // 如果按下Esc键，停止录制并取消
            if (e.Key == Key.Escape)
            {
                if (_isRecording)
                {
                    StopRecording();
                }
                
                DialogResult = false;
                Close();
            }
        }
        
        /// <summary>
        /// 安装鼠标钩子
        /// </summary>
        private IntPtr SetHook(HookProc proc)
        {
            using (var curProcess = System.Diagnostics.Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_MOUSE_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }
        
        /// <summary>
        /// 鼠标钩子回调函数
        /// </summary>
        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && _isRecording)
            {
                // 获取鼠标坐标结构
                MSLLHOOKSTRUCT hookStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
                
                // 获取当前鼠标所在窗口
                IntPtr windowHandle = WindowHelper.WindowFromPoint(hookStruct.pt.x, hookStruct.pt.y);
                
                // 检查窗口是否为目标窗口
                if (windowHandle == _targetWindowHandle)
                {
                    // 获取相对于窗口客户区的坐标
                    WindowHelper.POINT clientPoint = new WindowHelper.POINT { x = hookStruct.pt.x, y = hookStruct.pt.y };
                    WindowHelper.ScreenToClient(windowHandle, ref clientPoint);
                    
                    // 根据消息类型处理不同的鼠标事件
                    if (wParam == (IntPtr)WM_LBUTTONDOWN)
                    {
                        RecordAction(clientPoint.x, clientPoint.y, RecordedActionType.LeftClick);
                    }
                    else if (wParam == (IntPtr)WM_LBUTTONDBLCLK)
                    {
                        RecordAction(clientPoint.x, clientPoint.y, RecordedActionType.LeftDoubleClick);
                    }
                    else if (wParam == (IntPtr)WM_RBUTTONDOWN)
                    {
                        RecordAction(clientPoint.x, clientPoint.y, RecordedActionType.RightClick);
                    }
                    else if (wParam == (IntPtr)WM_MBUTTONDOWN)
                    {
                        RecordAction(clientPoint.x, clientPoint.y, RecordedActionType.MiddleClick);
                    }
                }
            }
            
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }
        
        /// <summary>
        /// 记录一个动作
        /// </summary>
        private void RecordAction(int x, int y, RecordedActionType actionType)
        {
            // 创建新的记录动作
            RecordedAction action = new RecordedAction
            {
                X = x,
                Y = y,
                ActionType = actionType
            };
            
            // 计算与上一个动作的时间间隔
            if (RecordedActions.Count > 0)
            {
                action.DelayFromPrevious = (int)(action.Timestamp - _lastActionTime).TotalMilliseconds;
            }
            
            // 更新最后动作时间
            _lastActionTime = action.Timestamp;
            
            // 添加到列表
            RecordedActions.Add(action);
            
            // 更新UI
            Dispatcher.Invoke(() =>
            {
                ActionCountTextBlock.Text = RecordedActions.Count.ToString();
            });
        }
        
        /// <summary>
        /// 开始录制
        /// </summary>
        private void StartRecording()
        {
            if (_targetWindowHandle == IntPtr.Zero)
            {
                MessageBox.Show("未选择目标窗口", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            _isRecording = true;
            _lastActionTime = DateTime.Now;
            
            // 更新UI状态
            StatusTextBlock.Text = "正在录制";
            StartButton.IsEnabled = false;
            StopButton.IsEnabled = true;
            
            // 将焦点设置到目标窗口
            WindowHelper.SetForeground(_targetWindowHandle);
        }
        
        /// <summary>
        /// 停止录制
        /// </summary>
        private void StopRecording()
        {
            _isRecording = false;
            
            // 更新UI状态
            StatusTextBlock.Text = "已停止";
            StartButton.IsEnabled = true;
            StopButton.IsEnabled = false;
        }
        
        /// <summary>
        /// 开始录制按钮点击事件
        /// </summary>
        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            StartRecording();
        }
        
        /// <summary>
        /// 停止录制按钮点击事件
        /// </summary>
        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            StopRecording();
        }
        
        /// <summary>
        /// 取消按钮点击事件
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // 如果正在录制，停止录制
            if (_isRecording)
            {
                StopRecording();
            }
            
            DialogResult = false;
            Close();
        }
        
        #region Win32 API
        
        private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);
        
        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public WindowHelper.POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }
        
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);
        
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);
        
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
        
        #endregion
    }
} 