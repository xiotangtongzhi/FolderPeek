using System;
using System.Windows.Forms;
namespace FolderPeek
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            L10n.Initialize();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
