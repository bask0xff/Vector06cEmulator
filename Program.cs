using System;
using System.IO;
using System.Windows.Forms;

namespace Vector06cEmulator
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var mainForm = new MainForm();
            Application.Run(mainForm);
        }
    }
}