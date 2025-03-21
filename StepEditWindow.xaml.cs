using System;
using System.Windows;

namespace AutoClickTool
{
    /// <summary>
    /// StepEditWindow.xaml 的交互逻辑
    /// </summary>
    public partial class StepEditWindow : Window
    {
        // 当前编辑的步骤
        public ClickStep Step { get; private set; }

        // 是否是编辑模式
        private bool _isEditMode;

        /// <summary>
        /// 创建新步骤的构造函数
        /// </summary>
        public StepEditWindow()
        {
            InitializeComponent();
            
            // 创建新步骤
            Step = new ClickStep();
            _isEditMode = false;
            
            // 初始化UI
            InitializeUI();
        }

        /// <summary>
        /// 编辑现有步骤的构造函数
        /// </summary>
        /// <param name="step">要编辑的步骤</param>
        public StepEditWindow(ClickStep step)
        {
            InitializeComponent();
            
            // 复制步骤数据
            Step = step;
            _isEditMode = true;
            
            // 初始化UI
            InitializeUI();
        }

        /// <summary>
        /// 初始化UI控件
        /// </summary>
        private void InitializeUI()
        {
            // 设置步骤数据到UI
            NameTextBox.Text = Step.Name;
            XCoordinateTextBox.Text = Step.X.ToString();
            YCoordinateTextBox.Text = Step.Y.ToString();
            DelayTextBox.Text = Step.DelayBefore.ToString();
            LoopTextBox.Text = Step.Loop.ToString();
            
            // 设置点击类型
            switch (Step.ClickType)
            {
                case ClickType.LeftClick:
                    ClickTypeComboBox.SelectedIndex = 0;
                    break;
                case ClickType.LeftDoubleClick:
                    ClickTypeComboBox.SelectedIndex = 1;
                    break;
                case ClickType.RightClick:
                    ClickTypeComboBox.SelectedIndex = 2;
                    break;
                case ClickType.MiddleClick:
                    ClickTypeComboBox.SelectedIndex = 3;
                    break;
            }
        }

        /// <summary>
        /// 捕获坐标按钮点击事件
        /// </summary>
        private void CaptureButton_Click(object sender, RoutedEventArgs e)
        {
            var captureWindow = new CoordinateCaptureWindow();
            if (captureWindow.ShowDialog() == true)
            {
                XCoordinateTextBox.Text = captureWindow.CapturedX.ToString();
                YCoordinateTextBox.Text = captureWindow.CapturedY.ToString();
            }
        }

        /// <summary>
        /// 保存按钮点击事件
        /// </summary>
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // 验证输入
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                MessageBox.Show("请输入步骤名称", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(XCoordinateTextBox.Text, out int x) || !int.TryParse(YCoordinateTextBox.Text, out int y))
            {
                MessageBox.Show("坐标必须是有效的整数", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(DelayTextBox.Text, out int delay) || delay < 0)
            {
                MessageBox.Show("延迟时间必须是有效的非负整数", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(LoopTextBox.Text, out int loop) || loop <= 0)
            {
                MessageBox.Show("循环次数必须是大于0的整数", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 更新步骤数据
            Step.Name = NameTextBox.Text;
            Step.X = x;
            Step.Y = y;
            Step.DelayBefore = delay;
            Step.Loop = loop;

            // 设置点击类型
            switch (ClickTypeComboBox.SelectedIndex)
            {
                case 0:
                    Step.ClickType = ClickType.LeftClick;
                    break;
                case 1:
                    Step.ClickType = ClickType.LeftDoubleClick;
                    break;
                case 2:
                    Step.ClickType = ClickType.RightClick;
                    break;
                case 3:
                    Step.ClickType = ClickType.MiddleClick;
                    break;
            }

            // 关闭窗口
            DialogResult = true;
            Close();
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