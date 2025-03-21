using System;

namespace AutoClickTool
{
    /// <summary>
    /// 记录的鼠标动作类型
    /// </summary>
    public enum RecordedActionType
    {
        /// <summary>
        /// 左键单击
        /// </summary>
        LeftClick,
        
        /// <summary>
        /// 左键双击
        /// </summary>
        LeftDoubleClick,
        
        /// <summary>
        /// 右键单击
        /// </summary>
        RightClick,
        
        /// <summary>
        /// 中键单击
        /// </summary>
        MiddleClick
    }
    
    /// <summary>
    /// 记录的点击动作类
    /// </summary>
    public class RecordedAction
    {
        /// <summary>
        /// 记录的时间
        /// </summary>
        public DateTime Timestamp { get; set; }
        
        /// <summary>
        /// 相对于窗口客户区的X坐标
        /// </summary>
        public int X { get; set; }
        
        /// <summary>
        /// 相对于窗口客户区的Y坐标
        /// </summary>
        public int Y { get; set; }
        
        /// <summary>
        /// 动作类型
        /// </summary>
        public RecordedActionType ActionType { get; set; }
        
        /// <summary>
        /// 距离上一个动作的延迟(毫秒)
        /// </summary>
        public int DelayFromPrevious { get; set; }
        
        /// <summary>
        /// 创建一个新的记录动作
        /// </summary>
        public RecordedAction()
        {
            Timestamp = DateTime.Now;
        }
        
        /// <summary>
        /// 转换为ClickStep
        /// </summary>
        public ClickStep ToClickStep(int index)
        {
            ClickStep step = new ClickStep
            {
                Index = index,
                Name = $"录制步骤 {index}",
                X = X,
                Y = Y,
                DelayBefore = DelayFromPrevious,
                Loop = 1
            };
            
            // 设置点击类型
            switch (ActionType)
            {
                case RecordedActionType.LeftClick:
                    step.ClickType = ClickType.LeftClick;
                    break;
                case RecordedActionType.LeftDoubleClick:
                    step.ClickType = ClickType.LeftDoubleClick;
                    break;
                case RecordedActionType.RightClick:
                    step.ClickType = ClickType.RightClick;
                    break;
                case RecordedActionType.MiddleClick:
                    step.ClickType = ClickType.MiddleClick;
                    break;
            }
            
            return step;
        }
    }
} 