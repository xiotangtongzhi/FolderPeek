using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
class InstalledProbe
{
    [STAThread] static int Main(string[] args)
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        var assembly = Assembly.LoadFrom(args[0]);
        bool expectedEnglish = args[1] == "en-US";
        var locale = assembly.GetType("FolderPeek.L10n");
        if ((bool)locale.GetField("English").GetValue(null) != expectedEnglish) throw new Exception("Wrong installed language");
        locale.GetMethod("Initialize").Invoke(null, null);
        string settings = Path.Combine(Path.GetTempPath(), "FolderPeek-probe-"+Guid.NewGuid().ToString("N"), "settings.xml");
        var type = assembly.GetType("FolderPeek.MainForm");
        using (var form = (Form)Activator.CreateInstance(type, new object[] { settings }))
        {
            var follow = (CheckBox)type.GetField("follow", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(form);
            if (follow.Text != (expectedEnglish ? "Auto follow" : "自动跟随")) throw new Exception("Wrong installed UI text");
            follow.Checked = false;
            form.Show(); Application.DoEvents();
            if (!form.Visible || !form.Text.Contains("v1.5")) throw new Exception("Installed form failed to show");
            form.Close();
        }
        Directory.Delete(Path.GetDirectoryName(settings), true);
        Console.WriteLine("PASS installed executable language, initialization and window: " + args[1]);
        return 0;
    }
}
