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