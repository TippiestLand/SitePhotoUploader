using System.Drawing;

namespace SitePhotoUploader
{
    public enum StepType
    {
        Click,
        OpenFileDialog,
        CopyLink
    }

    public class Step
    {
        public StepType Type { get; set; }
        public Point Position { get; set; }
        public int DelayMs { get; set; } = 10000;
        public int ClickCount { get; set; } = 3;      // Клики по ссылке
        public int DownArrowCount { get; set; } = 1;  // Стрелки вниз в меню

        public Point InputPosition { get; set; }
        public Point OpenButtonPosition { get; set; }

        public override string ToString()
        {
            return Type switch
            {
                StepType.Click => $"Клик ({Position.X}, {Position.Y})",
                StepType.OpenFileDialog => $"Открыть файл",
                StepType.CopyLink => $"Скопировать ссылку ({ClickCount} клика, {DownArrowCount} стрелок)",
                _ => "Неизвестно"
            };
        }
    }
}