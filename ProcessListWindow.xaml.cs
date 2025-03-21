using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace AutoClickTool
{
    /// <summary>
    /// ProcessListWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ProcessListWindow : Window
    {
        private List<WindowInfo> _allWindows;
        private List<WindowInfo> _filteredWindows;

        /// <summary>
        /// 选中的窗口信息
        /// </summary>
        public WindowInfo SelectedWindow { get; private set; }

        public ProcessListWindow()
        {
            InitializeComponent();
            LoadProcessList();
        }

        /// <summary>
        /// 加载进程列表
        /// </summary>
        private void LoadProcessList()
        {
            try
            {
                _allWindows = WindowHelper.GetTaskManagerProcessWindows();
                _filteredWindows = new List<WindowInfo>(_allWindows);
                ProcessListView.ItemsSource = _filteredWindows;
                
                if (_filteredWindows.Count > 0)
                {
                    ProcessListView.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载进程列表失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 搜索框文本变化事件
        /// </summary>
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = SearchTextBox.Text.ToLower();
            
            if (string.IsNullOrWhiteSpace(searchText))
            {
                _filteredWindows = new List<WindowInfo>(_allWindows);
            }
            else
            {
                _filteredWindows = _allWindows.Where(w => 
                    w.Title.ToLower().Contains(searchText) || 
                    w.ProcessName.ToLower().Contains(searchText) ||
                    w.ProcessId.ToString().Contains(searchText)).ToList();
            }
            
            ProcessListView.ItemsSource = _filteredWindows;
        }

        /// <summary>
        /// 刷新按钮点击事件
        /// </summary>
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadProcessList();
        }

        /// <summary>
        /// 列表选择变化事件
        /// </summary>
        private void ProcessListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectButton.IsEnabled = ProcessListView.SelectedItem != null;
        }

        /// <summary>
        /// 选择按钮点击事件
        /// </summary>
        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProcessListView.SelectedItem is WindowInfo selectedWindow)
            {
                SelectedWindow = selectedWindow;
                DialogResult = true;
                Close();
            }
        }

        /// <summary>
        /// 取消按钮点击事件
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
} 