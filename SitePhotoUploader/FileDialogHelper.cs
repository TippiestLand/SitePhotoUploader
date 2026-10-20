using System;
using System.Threading;
using System.Windows.Automation;

namespace SitePhotoUploader
{
    public class FileDialogHelper
    {
        public bool SelectFileInOpenDialog(string filePath, int timeoutMs = 5000)
        {
            // Ждем 2 секунды
            Thread.Sleep(2000);

            // Ищем открывшееся окно Проводника
            var dialog = FindOpenDialog(timeoutMs);
            if (dialog == null)
            {
                return false; // Окно не найдено
            }

            // 1. Находим поле ввода (куда вставлять путь к файлу)
            var edit = FindElementByControlType(dialog, ControlType.Edit);
            if (edit == null) return false;

            // 2. Вставляем путь
            if (edit.TryGetCurrentPattern(ValuePattern.Pattern, out object valuePatternObj))
            {
                ((ValuePattern)valuePatternObj).SetValue(filePath);
            }
            else
            {
                // Если не сработало, пробуем кликнуть и отправить текст
                edit.SetFocus();
                Thread.Sleep(200);
                System.Windows.Forms.SendKeys.SendWait(filePath);
            }

            Thread.Sleep(500); // Даем время на ввод

            // 3. Находим кнопку "Открыть" (Open) или "Сохранить"
            var openButton = FindElementByName(dialog, "Открыть")
                          ?? FindElementByName(dialog, "Open")
                          ?? FindElementByName(dialog, "&Open");

            if (openButton != null)
            {
                // Нажимаем кнопку
                if (openButton.TryGetCurrentPattern(InvokePattern.Pattern, out object invokePatternObj))
                {
                    ((InvokePattern)invokePatternObj).Invoke();
                    return true;
                }
            }

            // Если кнопку не нашли, просто жмем Enter
            System.Windows.Forms.SendKeys.SendWait("{ENTER}");
            return true;
        }

        private AutomationElement FindOpenDialog(int timeoutMs)
        {
            DateTime endTime = DateTime.Now.AddMilliseconds(timeoutMs);

            while (DateTime.Now < endTime)
            {
                var desktop = AutomationElement.RootElement;
                var windows = desktop.FindAll(TreeScope.Children, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window));

                foreach (AutomationElement window in windows)
                {
                    try
                    {
                        string name = window.Current.Name ?? "";

                        if (name.Contains("Открыть") || name.Contains("Open") ||
                            name.Contains("Выбор файла") || name.Contains("Choose File"))
                        {
                            return window;
                        }
                    }
                    catch { }
                }
                Thread.Sleep(500);
            }
            return null;
        }

        private AutomationElement FindElementByControlType(AutomationElement parent, ControlType type)
        {
            var condition = new PropertyCondition(AutomationElement.ControlTypeProperty, type);
            return parent.FindFirst(TreeScope.Descendants, condition);
        }

        private AutomationElement FindElementByName(AutomationElement parent, string name)
        {
            var condition = new PropertyCondition(AutomationElement.NameProperty, name);
            return parent.FindFirst(TreeScope.Descendants, condition);
        }
    }
}