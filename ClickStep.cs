using System;
using System.ComponentModel;

namespace AutoClickTool
{
    /// <summary>
    /// 点击类型枚举
    /// </summary>
    public enum ClickType
    {
        LeftClick,
        LeftDoubleClick,
        RightClick,
        MiddleClick
    }

    /// <summary>
    /// 点击步骤类，表示一个点击操作的配置
    /// </summary>
    public class ClickStep : INotifyPropertyChanged
    {
        private int _index;
        private string _name;
        private int _x;
        private int _y;
        private ClickType _clickType;
        private int _delayBefore;
        private int _loop;
        private double _xPercentage; // 相对X坐标百分比
        private double _yPercentage; // 相对Y坐标百分比
        private int _baseWidth = 0;  // 基准分辨率宽度
        private int _baseHeight = 0; // 基准分辨率高度

        /// <summary>
        /// 步骤序号
        /// </summary>
        public int Index
        {
            get => _index;
            set
            {
                if (_index != value)
                {
                    _index = value;
                    OnPropertyChanged(nameof(Index));
                }
            }
        }

        /// <summary>
        /// 步骤名称
        /// </summary>
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        /// <summary>
        /// X坐标
        /// </summary>
        public int X
        {
            get => _x;
            set
            {
                if (_x != value)
                {
                    _x = value;
                    OnPropertyChanged(nameof(X));
                }
            }
        }

        /// <summary>
        /// Y坐标
        /// </summary>
        public int Y
        {
            get => _y;
            set
            {
                if (_y != value)
                {
                    _y = value;
                    OnPropertyChanged(nameof(Y));
                }
            }
        }

        /// <summary>
        /// 点击类型
        /// </summary>
        public ClickType ClickType
        {
            get => _clickType;
            set
            {
                if (_clickType != value)
                {
                    _clickType = value;
                    OnPropertyChanged(nameof(ClickType));
                }
            }
        }

        /// <summary>
        /// 点击前延迟时间(毫秒)
        /// </summary>
        public int DelayBefore
        {
            get => _delayBefore;
            set
            {
                if (_delayBefore != value)
                {
                    _delayBefore = value;
                    OnPropertyChanged(nameof(DelayBefore));
                }
            }
        }

        /// <summary>
        /// 循环次数
        /// </summary>
        public int Loop
        {
            get => _loop;
            set
            {
                if (_loop != value)
                {
                    _loop = value;
                    OnPropertyChanged(nameof(Loop));
                }
            }
        }

        /// <summary>
        /// X坐标相对百分比 (0.0 - 1.0)
        /// </summary>
        public double XPercentage
        {
            get => _xPercentage;
            set
            {
                if (_xPercentage != value)
                {
                    _xPercentage = value;
                    OnPropertyChanged(nameof(XPercentage));
                }
            }
        }

        /// <summary>
        /// Y坐标相对百分比 (0.0 - 1.0)
        /// </summary>
        public double YPercentage
        {
            get => _yPercentage;
            set
            {
                if (_yPercentage != value)
                {
                    _yPercentage = value;
                    OnPropertyChanged(nameof(YPercentage));
                }
            }
        }

        /// <summary>
        /// 基准分辨率宽度
        /// </summary>
        public int BaseWidth
        {
            get => _baseWidth;
            set
            {
                if (_baseWidth != value)
                {
                    _baseWidth = value;
                    OnPropertyChanged(nameof(BaseWidth));
                }
            }
        }

        /// <summary>
        /// 基准分辨率高度
        /// </summary>
        public int BaseHeight
        {
            get => _baseHeight;
            set
            {
                if (_baseHeight != value)
                {
                    _baseHeight = value;
                    OnPropertyChanged(nameof(BaseHeight));
                }
            }
        }

        /// <summary>
        /// 计算在指定分辨率下的实际X坐标
        /// </summary>
        /// <param name="targetWidth">目标分辨率宽度</param>
        /// <returns>调整后的X坐标</returns>
        public int GetScaledX(int targetWidth)
        {
            if (_baseWidth <= 0) return _x;
            return (int)Math.Round(_xPercentage * targetWidth);
        }

        /// <summary>
        /// 计算在指定分辨率下的实际Y坐标
        /// </summary>
        /// <param name="targetHeight">目标分辨率高度</param>
        /// <returns>调整后的Y坐标</returns>
        public int GetScaledY(int targetHeight)
        {
            if (_baseHeight <= 0) return _y;
            return (int)Math.Round(_yPercentage * targetHeight);
        }

        /// <summary>
        /// 更新坐标百分比
        /// </summary>
        public void UpdatePercentages()
        {
            if (_baseWidth > 0)
                _xPercentage = (double)_x / _baseWidth;
            
            if (_baseHeight > 0)
                _yPercentage = (double)_y / _baseHeight;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public ClickStep()
        {
            Name = "新步骤";
            ClickType = ClickType.LeftClick;
            DelayBefore = 1000;
            Loop = 1;
        }

        /// <summary>
        /// 属性变更事件
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// 触发属性变更事件
        /// </summary>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}