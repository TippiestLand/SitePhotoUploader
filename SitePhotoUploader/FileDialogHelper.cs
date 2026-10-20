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

                // 2. Ждем, пока Windows обновит буфер
                Thread.Sleep(500);

                // 3. Вставляем ОДИН раз
                SendKeys.SendWait("^v");
            }
            catch { }
        }
    }
}