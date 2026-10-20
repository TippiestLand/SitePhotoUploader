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
        public Point Position { get; set; } // Для обычных кликов и кликов по ссылке
        public int DelayMs { get; set; } = 10000;
        public int ClickCount { get; set; } = 3;

        // Новые поля для шага "Открыть файл"
        public Point InputPosition { get; set; } // Куда кликнуть, чтобы вставить путь
        public Point OpenButtonPosition { get; set; } // Куда кликнуть, чтобы нажать "Открыть"

        public override string ToString()
        {
            return Type switch
            {
                StepType.Click => $"Клик ({Position.X}, {Position.Y})",
                StepType.OpenFileDialog => $"Открыть файл (ввод: {InputPosition.X},{InputPosition.Y}, кнопка: {OpenButtonPosition.X},{OpenButtonPosition.Y})",
                StepType.CopyLink => $"Скопировать ссылку ({ClickCount} клика)",
                _ => "Неизвестно"
            };
        }
    }
}