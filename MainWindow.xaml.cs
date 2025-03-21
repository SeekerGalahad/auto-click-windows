using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace AutoClickTool
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        // 点击步骤集合
        private ObservableCollection<ClickStep> _steps;

        // 目标窗口句柄
        private IntPtr _targetWindowHandle = IntPtr.Zero;

        // 任务控制
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isPaused;
        private ManualResetEvent _pauseEvent;

        // 窗口标题列表
        private List<string> _windowTitles;

        // 基准分辨率
        private int _baseWidth;
        private int _baseHeight;

        // 鼠标录制器
        private MouseRecorder _recorder;

        public MainWindow()
        {
            InitializeComponent();

            // 初始化步骤集合
            _steps = new ObservableCollection<ClickStep>();
            StepsListView.ItemsSource = _steps;

            // 初始化暂停事件
            _pauseEvent = new ManualResetEvent(true);

            // 加载窗口标题列表
            LoadWindowTitles();
            
            // 默认选择第一个分辨率
            if (ResolutionComboBox.Items.Count > 0)
            {
                ResolutionComboBox.SelectedIndex = 0;
            }
            
            // 设置"设置基准分辨率"按钮的点击事件
            SetBaseResolutionButton.Click += SetBaseResolutionButton_Click;
        }

        #region 窗口操作

        /// <summary>
        /// 加载窗口标题列表
        /// </summary>
        private void LoadWindowTitles()
        {
            try
            {
                _windowTitles = WindowHelper.GetAllWindowTitles();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载窗口列表失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 刷新窗口列表按钮点击事件
        /// </summary>
        private void RefreshWindowsButton_Click(object sender, RoutedEventArgs e)
        {
            LoadWindowTitles();
            
            // 自动查找任务管理器窗口
            if (_windowTitles != null && _windowTitles.Count > 0)
            {
                string taskManagerTitle = _windowTitles.FirstOrDefault(t => 
                    t.Contains("任务管理器") || 
                    t.Contains("Task Manager") || 
                    t.ToLower().Contains("taskmgr"));
                
                if (!string.IsNullOrEmpty(taskManagerTitle))
                {
                    WindowTitleTextBox.Text = taskManagerTitle;
                    
                    // 获取窗口句柄
                    _targetWindowHandle = WindowHelper.GetWindowHandle(taskManagerTitle);
                    if (_targetWindowHandle != IntPtr.Zero)
                    {
                        // 获取窗口的实际分辨率
                        Size windowSize = WindowHelper.GetWindowSize(_targetWindowHandle);
                        string actualResolution = $"{(int)windowSize.Width}x{(int)windowSize.Height}";
                        ResolutionComboBox.Text = actualResolution;
                        
                        // 更新基准分辨率
                        _baseWidth = (int)windowSize.Width;
                        _baseHeight = (int)windowSize.Height;
                        
                        MessageBox.Show($"已自动选择任务管理器窗口，分辨率: {actualResolution}", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("已自动选择任务管理器窗口，但无法获取窗口句柄", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else
                {
                    MessageBox.Show("窗口列表已刷新，未找到任务管理器窗口", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show("窗口列表已刷新", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// 验证分辨率按钮点击事件
        /// </summary>
        private void VerifyResolutionButton_Click(object sender, RoutedEventArgs e)
        {
            // 获取窗口句柄
            string windowTitle = WindowTitleTextBox.Text.Trim();
            if (string.IsNullOrEmpty(windowTitle))
            {
                MessageBox.Show("请输入窗口标题", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _targetWindowHandle = WindowHelper.GetWindowHandle(windowTitle);
            if (_targetWindowHandle == IntPtr.Zero)
            {
                MessageBox.Show("未找到指定窗口", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 获取窗口的实际分辨率
            Size windowSize = WindowHelper.GetWindowSize(_targetWindowHandle);
            string actualResolution = $"{(int)windowSize.Width}x{(int)windowSize.Height}";
            
            // 自动填充窗口的实际分辨率
            ResolutionComboBox.Text = actualResolution;
            
            // 更新基准分辨率
            _baseWidth = (int)windowSize.Width;
            _baseHeight = (int)windowSize.Height;
            
            MessageBox.Show($"已获取窗口实际分辨率: {actualResolution}\n已设置为基准分辨率", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 设置当前分辨率为基准按钮点击事件
        /// </summary>
        private void SetBaseResolutionButton_Click(object sender, RoutedEventArgs e)
        {
            if (_targetWindowHandle == IntPtr.Zero)
            {
                MessageBox.Show("请先选择目标窗口", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Size windowSize = WindowHelper.GetWindowSize(_targetWindowHandle);
            if (windowSize.Width <= 0 || windowSize.Height <= 0)
            {
                MessageBox.Show("无法获取窗口尺寸", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _baseWidth = (int)windowSize.Width;
            _baseHeight = (int)windowSize.Height;

            // 更新所有步骤的基准分辨率
            foreach (var step in _steps)
            {
                step.BaseWidth = _baseWidth;
                step.BaseHeight = _baseHeight;
                step.UpdatePercentages();
            }

            MessageBox.Show($"已设置基准分辨率为: {_baseWidth}x{_baseHeight}", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 查看进程列表按钮点击事件
        /// </summary>
        private void ShowProcessListButton_Click(object sender, RoutedEventArgs e)
        {
            var processListWindow = new ProcessListWindow();
            processListWindow.Owner = this;
            
            if (processListWindow.ShowDialog() == true && processListWindow.SelectedWindow != null)
            {
                // 设置窗口标题
                WindowTitleTextBox.Text = processListWindow.SelectedWindow.Title;
                
                // 获取窗口句柄
                _targetWindowHandle = processListWindow.SelectedWindow.Handle;
                if (_targetWindowHandle != IntPtr.Zero)
                {
                    // 获取窗口的实际分辨率
                    Size windowSize = WindowHelper.GetWindowSize(_targetWindowHandle);
                    string actualResolution = $"{(int)windowSize.Width}x{(int)windowSize.Height}";
                    
                    // 自动填充分辨率并设置为基准
                    ResolutionComboBox.Text = actualResolution;
                    _baseWidth = (int)windowSize.Width;
                    _baseHeight = (int)windowSize.Height;
                    
                    MessageBox.Show($"已选择窗口: {processListWindow.SelectedWindow.Title}\n" +
                                    $"进程: {processListWindow.SelectedWindow.ProcessName} (ID: {processListWindow.SelectedWindow.ProcessId})\n" +
                                    $"分辨率: {actualResolution}", 
                                    "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("无法获取所选窗口的句柄", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        #endregion

        #region 步骤管理

        /// <summary>
        /// 添加步骤按钮点击事件
        /// </summary>
        private void AddStepButton_Click(object sender, RoutedEventArgs e)
        {
            var editWindow = new StepEditWindow();
            if (editWindow.ShowDialog() == true)
            {
                var step = editWindow.Step;
                step.Index = _steps.Count + 1;
                _steps.Add(step);
            }
        }

        /// <summary>
        /// 编辑步骤按钮点击事件
        /// </summary>
        private void EditStepButton_Click(object sender, RoutedEventArgs e)
        {
            if (StepsListView.SelectedItem is ClickStep selectedStep)
            {
                var editWindow = new StepEditWindow(selectedStep);
                editWindow.ShowDialog();
            }
            else
            {
                MessageBox.Show("请先选择一个步骤", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// 删除步骤按钮点击事件
        /// </summary>
        private void RemoveStepButton_Click(object sender, RoutedEventArgs e)
        {
            if (StepsListView.SelectedItem is ClickStep selectedStep)
            {
                if (MessageBox.Show($"确定要删除步骤 '{selectedStep.Name}' 吗？", "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    _steps.Remove(selectedStep);
                    
                    // 更新序号
                    for (int i = 0; i < _steps.Count; i++)
                    {
                        _steps[i].Index = i + 1;
                    }
                }
            }
            else
            {
                MessageBox.Show("请先选择一个步骤", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// 上移步骤按钮点击事件
        /// </summary>
        private void MoveUpButton_Click(object sender, RoutedEventArgs e)
        {
            if (StepsListView.SelectedItem is ClickStep selectedStep)
            {
                int index = _steps.IndexOf(selectedStep);
                if (index > 0)
                {
                    _steps.Move(index, index - 1);
                    
                    // 更新序号
                    for (int i = 0; i < _steps.Count; i++)
                    {
                        _steps[i].Index = i + 1;
                    }
                }
            }
            else
            {
                MessageBox.Show("请先选择一个步骤", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// 下移步骤按钮点击事件
        /// </summary>
        private void MoveDownButton_Click(object sender, RoutedEventArgs e)
        {
            if (StepsListView.SelectedItem is ClickStep selectedStep)
            {
                int index = _steps.IndexOf(selectedStep);
                if (index < _steps.Count - 1)
                {
                    _steps.Move(index, index + 1);
                    
                    // 更新序号
                    for (int i = 0; i < _steps.Count; i++)
                    {
                        _steps[i].Index = i + 1;
                    }
                }
            }
            else
            {
                MessageBox.Show("请先选择一个步骤", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// 捕获坐标按钮点击事件
        /// </summary>
        private void CaptureCoordinatesButton_Click(object sender, RoutedEventArgs e)
        {
            var captureWindow = new CoordinateCaptureWindow();
            if (captureWindow.ShowDialog() == true)
            {
                // 如果有选中的步骤，则更新坐标
                if (StepsListView.SelectedItem is ClickStep selectedStep)
                {
                    selectedStep.X = captureWindow.CapturedX;
                    selectedStep.Y = captureWindow.CapturedY;
                }
                else
                {
                    // 否则创建新步骤
                    var step = new ClickStep
                    {
                        Name = "捕获的步骤",
                        X = captureWindow.CapturedX,
                        Y = captureWindow.CapturedY,
                        Index = _steps.Count + 1
                    };
                    _steps.Add(step);
                }
            }
        }

        #endregion

        #region 任务执行

        /// <summary>
        /// 开始按钮点击事件
        /// </summary>
        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            // 验证窗口句柄
            if (_targetWindowHandle == IntPtr.Zero)
            {
                MessageBox.Show("请先验证目标窗口", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 验证步骤列表
            if (_steps.Count == 0)
            {
                MessageBox.Show("请先添加点击步骤", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 获取全局设置
            if (!int.TryParse(GlobalIntervalTextBox.Text, out int globalInterval) || globalInterval < 0)
            {
                MessageBox.Show("全局间隔必须是有效的非负整数", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(LoopCountTextBox.Text, out int loopCount) || loopCount <= 0)
            {
                MessageBox.Show("循环次数必须是大于0的整数", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 更新UI状态
            StartButton.IsEnabled = false;
            PauseButton.IsEnabled = true;
            StopButton.IsEnabled = true;
            StatusTextBlock.Text = "正在执行...";

            // 重置暂停状态
            _isPaused = false;
            _pauseEvent.Set();

            // 创建取消令牌
            _cancellationTokenSource = new CancellationTokenSource();
            
            // 最小化当前窗口
            this.WindowState = WindowState.Minimized;
            
            // 启动任务，添加短暂延迟确保窗口状态变化完成
            Task.Run(async () => 
            {
                // 短暂延迟，确保窗口最小化完成
                await Task.Delay(300);
                
                // 将焦点设置到目标窗口，并验证是否成功
                bool focusSuccess = WindowHelper.SetForeground(_targetWindowHandle);
                
                // 如果焦点设置失败，尝试重试
                int retryCount = 0;
                while (!focusSuccess && retryCount < 3)
                {
                    await Task.Delay(200);
                    focusSuccess = WindowHelper.SetForeground(_targetWindowHandle);
                    retryCount++;
                }
                
                // 如果仍然失败，通知用户
                if (!focusSuccess)
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        MessageBox.Show("无法将焦点设置到目标窗口，请确保目标窗口未被锁定或处于特殊状态", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                    });
                }
                
                // 执行任务
                await ExecuteTask(globalInterval, loopCount, _cancellationTokenSource.Token);
            });
        }

        /// <summary>
        /// 暂停按钮点击事件
        /// </summary>
        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isPaused)
            {
                // 恢复执行
                _isPaused = false;
                _pauseEvent.Set();
                PauseButton.Content = "暂停";
                StatusTextBlock.Text = "正在执行...";
            }
            else
            {
                // 暂停执行
                _isPaused = true;
                _pauseEvent.Reset();
                PauseButton.Content = "继续";
                StatusTextBlock.Text = "已暂停";
            }
        }

        /// <summary>
        /// 停止按钮点击事件
        /// </summary>
        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            // 取消任务
            _cancellationTokenSource?.Cancel();

            // 恢复暂停事件，确保任务能够退出
            _pauseEvent.Set();

            // 更新UI状态
            StartButton.IsEnabled = true;
            PauseButton.IsEnabled = false;
            StopButton.IsEnabled = false;
            PauseButton.Content = "暂停";
            StatusTextBlock.Text = "已停止";
            ProgressBar.Value = 0;
        }

        /// <summary>
        /// 执行任务
        /// </summary>
        /// <param name="globalInterval">全局间隔(毫秒)</param>
        /// <param name="loopCount">循环次数</param>
        /// <param name="cancellationToken">取消令牌</param>
        private async Task ExecuteTask(int globalInterval, int loopCount, CancellationToken cancellationToken)
        {
            try
            {
                // 计算总步骤数
                int totalSteps = _steps.Count * loopCount;
                int currentStep = 0;
                
                // 在任务开始前，先尝试点击窗口标题栏获取焦点（一次性操作）
                if (WindowHelper.IsWindowExists(_targetWindowHandle))
                {
                    WindowHelper.ClickWindowTitle(_targetWindowHandle);
                }
                
                // 检查是否启用自适应分辨率
                bool useAdaptiveResolution = AdaptiveResolutionCheckBox.IsChecked ?? false;
                
                // 如果启用自适应分辨率，获取当前窗口大小
                int currentWidth = 0;
                int currentHeight = 0;
                
                if (useAdaptiveResolution)
                {
                    Size windowSize = WindowHelper.GetWindowSize(_targetWindowHandle);
                    currentWidth = (int)windowSize.Width;
                    currentHeight = (int)windowSize.Height;
                }

                // 循环执行
                for (int loop = 0; loop < loopCount; loop++)
                {
                    // 检查是否取消
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    // 执行每个步骤
                    foreach (var step in _steps)
                    {
                        // 检查是否取消
                        if (cancellationToken.IsCancellationRequested)
                            break;

                        // 检查是否暂停
                        _pauseEvent.WaitOne();

                        // 更新UI状态
                        await Dispatcher.InvokeAsync(() =>
                        {
                            StatusTextBlock.Text = $"正在执行: {step.Name}";
                        });

                        // 执行步骤
                        try
                        {
                            // 等待延迟时间
                            await Task.Delay(step.DelayBefore, cancellationToken);

                            // 执行点击
                            for (int i = 0; i < step.Loop; i++)
                            {
                                // 检查窗口是否仍然存在
                                if (!WindowHelper.IsWindowExists(_targetWindowHandle))
                                {
                                    // 如果窗口不存在，则暂停任务
                                    await Dispatcher.InvokeAsync(() =>
                                    {
                                        _isPaused = true;
                                        _pauseEvent.Reset();
                                        PauseButton.Content = "继续";
                                        StatusTextBlock.Text = "已暂停: 目标窗口不存在";
                                        MessageBox.Show("目标窗口不存在，任务已暂停", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                                    });
                                    return;
                                }
                                
                                // 如果启用自适应分辨率，并且有可用的基准分辨率，则计算缩放后的坐标
                                int x = step.X;
                                int y = step.Y;
                                
                                if (useAdaptiveResolution && step.BaseWidth > 0 && step.BaseHeight > 0)
                                {
                                    // 如果窗口大小发生了变化，重新获取
                                    if (currentWidth <= 0 || currentHeight <= 0)
                                    {
                                        Size windowSize = WindowHelper.GetWindowSize(_targetWindowHandle);
                                        currentWidth = (int)windowSize.Width;
                                        currentHeight = (int)windowSize.Height;
                                    }
                                    
                                    // 使用相对百分比计算实际坐标
                                    x = step.GetScaledX(currentWidth);
                                    y = step.GetScaledY(currentHeight);
                                }

                                // 执行点击
                                WindowHelper.SimulateClick(_targetWindowHandle, x, y, step.ClickType);

                                // 如果有多次循环，则等待全局间隔时间
                                if (i < step.Loop - 1)
                                {
                                    await Task.Delay(globalInterval, cancellationToken);
                                }
                            }

                            // 更新进度
                            currentStep++;
                            await Dispatcher.InvokeAsync(() =>
                            {
                                ProgressBar.Value = (double)currentStep / totalSteps * 100;
                            });

                            // 等待全局间隔时间
                            await Task.Delay(globalInterval, cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            await Dispatcher.InvokeAsync(() =>
                            {
                                MessageBox.Show($"执行步骤 '{step.Name}' 时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                            });
                            return;
                        }
                    }
                }

                // 任务完成
                await Dispatcher.InvokeAsync(() =>
                {
                    StartButton.IsEnabled = true;
                    PauseButton.IsEnabled = false;
                    StopButton.IsEnabled = false;
                    PauseButton.Content = "暂停";
                    StatusTextBlock.Text = "已完成";
                    ProgressBar.Value = 100;
                    MessageBox.Show("任务已完成", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                });
            }
            catch (OperationCanceledException)
            {
                // 任务被取消
                await Dispatcher.InvokeAsync(() =>
                {
                    StatusTextBlock.Text = "已取消";
                });
            }
            catch (Exception ex)
            {
                // 发生异常
                await Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"执行任务时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    StartButton.IsEnabled = true;
                    PauseButton.IsEnabled = false;
                    StopButton.IsEnabled = false;
                    PauseButton.Content = "暂停";
                    StatusTextBlock.Text = "出错";
                });
            }
        }

        /// <summary>
        /// 开始录制按钮点击事件
        /// </summary>
        private void StartRecordingButton_Click(object sender, RoutedEventArgs e)
        {
            // 验证窗口句柄
            if (_targetWindowHandle == IntPtr.Zero)
            {
                MessageBox.Show("请先选择目标窗口", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            // 获取窗口标题
            string windowTitle = WindowTitleTextBox.Text.Trim();
            if (string.IsNullOrEmpty(windowTitle))
            {
                MessageBox.Show("未找到窗口标题，请重新选择窗口", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (StartRecordingButton.Content.ToString() == "开始录制")
            {
                // 开始录制
                _recorder = new MouseRecorder(_targetWindowHandle);
                _recorder.ActionRecorded += Recorder_ActionRecorded;
                _recorder.StartRecording();
                
                // 更新UI
                StartRecordingButton.Content = "停止录制";
                StatusTextBlock.Text = "正在录制鼠标操作...";
                
                // 最小化当前窗口
                this.WindowState = WindowState.Minimized;
                
                // 将焦点设置到目标窗口
                WindowHelper.SetForeground(_targetWindowHandle);
            }
            else
            {
                // 停止录制
                if (_recorder != null)
                {
                    _recorder.StopRecording();
                    _recorder.ActionRecorded -= Recorder_ActionRecorded;
                    _recorder = null;
                }
                
                // 更新UI
                StartRecordingButton.Content = "开始录制";
                StatusTextBlock.Text = "录制已停止";
                
                // 恢复窗口
                this.WindowState = WindowState.Normal;
            }
        }
        
        /// <summary>
        /// 处理录制动作事件
        /// </summary>
        private void Recorder_ActionRecorded(object sender, RecordedAction action)
        {
            // 在UI线程中更新
            Dispatcher.Invoke(() =>
            {
                // 将录制的操作转换为点击步骤
                var step = action.ToClickStep(_steps.Count + 1);
                
                // 设置基准分辨率
                step.BaseWidth = _baseWidth;
                step.BaseHeight = _baseHeight;
                step.UpdatePercentages();
                
                // 添加到步骤列表
                _steps.Add(step);
                
                // 确保最新的步骤可见
                if (StepsListView.Items.Count > 0)
                {
                    StepsListView.ScrollIntoView(StepsListView.Items[StepsListView.Items.Count - 1]);
                }
            });
        }

        #endregion
    }
}