using System;
using System.Threading;
using System.Windows.Forms;

namespace SitePhotoUploader
{
    public class FileDialogHelper
    {
        public void InsertPathIntoSaveDialog(string filePath, int delayMs)
        {
            try
            {
                // 1. Ждем, пока окно Проводника точно появится
                Thread.Sleep(1000);

                // 2. Очищаем текущее поле (Ctrl+A, Delete)
                SendKeys.SendWait("^a");
                Thread.Sleep(200);
                SendKeys.SendWait("{DEL}");
                Thread.Sleep(200);

                // 3. Вводим путь посимвольно (это обходит проблему с буфером)
                foreach (char c in filePath)
                {
                    if (c == '\\')
                        SendKeys.SendWait("{\\}");
                    else if (c == ':')
                        SendKeys.SendWait("{:}");
                    else
                        SendKeys.SendWait(c.ToString());

                    Thread.Sleep(10); // Небольшая пауза между символами
                }

                Thread.Sleep(500);

                // 4. Нажимаем Enter для открытия
                SendKeys.SendWait("{ENTER}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка ввода: " + ex.Message);
            }
        }
    }
}