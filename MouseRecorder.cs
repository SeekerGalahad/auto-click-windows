using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace AutoClickTool
{
    /// <summary>
    /// 鼠标操作录制器
    /// </summary>
    public class MouseRecorder
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
        
        // 录制状态
        private bool _isRecording;
        private DateTime _lastActionTime;
        
        /// <summary>
        /// 当记录到新的操作时触发
        /// </summary>
        public event EventHandler<RecordedAction> ActionRecorded;
        
        /// <summary>
        /// 初始化鼠标录制器
        /// </summary>
        /// <param name="targetWindowHandle">目标窗口句柄</param>
        public MouseRecorder(IntPtr targetWindowHandle)
        {
            _targetWindowHandle = targetWindowHandle;
            _isRecording = false;
            _mouseProc = MouseHookCallback;
        }
        
        /// <summary>
        /// 开始录制
        /// </summary>
        public void StartRecording()
        {
            if (_isRecording)
                return;
                
            // 安装鼠标钩子
            _hookID = SetHook(_mouseProc);
            _isRecording = true;
            _lastActionTime = DateTime.Now;
        }
        
        /// <summary>
        /// 停止录制
        /// </summary>
        public void StopRecording()
        {
            if (!_isRecording)
                return;
                
            // 卸载鼠标钩子
            if (_hookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookID);
                _hookID = IntPtr.Zero;
            }
            
            _isRecording = false;
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
            action.DelayFromPrevious = (int)(action.Timestamp - _lastActionTime).TotalMilliseconds;
            
            // 更新最后动作时间
            _lastActionTime = action.Timestamp;
            
            // 触发事件
            ActionRecorded?.Invoke(this, action);
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