using System;
using System.Threading;
using System.Windows.Forms;

namespace SitePhotoUploader
{
    public class FileDialogHelper
    {
        public bool SelectFileInOpenDialog(string filePath, int timeoutMs = 5000)
        {
            // 1. Ждем 2 секунды, как вы просили
            Thread.Sleep(2000);

            try
            {
                // 2. Копируем путь в буфер обмена
                Clipboard.SetText(filePath);
                Thread.Sleep(300);

                // 3. Вставляем путь (Ctrl+V) и нажимаем Enter
                // Это работает, потому что фокус после открытия окна Проводника
                // автоматически ставится на строку ввода имени файла.
                SendKeys.SendWait("^v");
                Thread.Sleep(500);

                SendKeys.SendWait("{ENTER}");
                Thread.Sleep(500); // Даем время окну закрыться

                // 4. На всякий случай нажимаем Enter еще раз, если первое окно было "Поиск"
                SendKeys.SendWait("{ENTER}");

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}