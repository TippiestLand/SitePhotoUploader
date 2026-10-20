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
                Clipboard.SetText(filePath);
                Thread.Sleep(200);
                SendKeys.SendWait("^v");
                Thread.Sleep(300);
            }
            catch { }
        }
    }
}