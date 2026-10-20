using System;
using System.Threading;
using System.Windows.Forms;

namespace SitePhotoUploader
{
    public class FileDialogHelper
    {
        public void InsertPath(string filePath)
        {
            try
            {
                // 1. Записываем в буфер
                Clipboard.Clear();
                Clipboard.SetText(filePath);

                // 2. Ждем, пока Windows обновит буфер (очень частая причина сбоев)
                Thread.Sleep(500);

                // 3. Вставляем. Отправляем дважды на случай потери фокуса
                SendKeys.SendWait("^v");
                Thread.Sleep(300);
                SendKeys.SendWait("^v");
                Thread.Sleep(500); // Ждем, пока текст появится в строке
            }
            catch { }
        }
    }
}