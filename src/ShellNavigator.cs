using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FolderPeek
{
    // Uses Shell.Explore for directories and SHOpenFolderAndSelectItems for files.
    // It never invokes a default/open verb on an arbitrary target path.
    // Even a directory replaced by an executable during a race is not executed.
    internal static class ShellNavigator
    {
        private const uint SfgaoFolder = 0x20000000;
        private const uint SfgaoFileSystem = 0x40000000;
        public static Task<string> OpenAsync(Entry entry)
        {
            var result = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            var thread = new Thread(() =>
            {
                int initialized = -1;
                try
                {
                    initialized = CoInitializeEx(IntPtr.Zero, 0x2 | 0x4);
                    if (initialized < 0) { result.TrySetResult(L10n.T("无法初始化资源管理器接口", "Cannot initialize the File Explorer interface")); return; }
                    using (var context = new ApplicationContext())
                    using (var timer = new System.Windows.Forms.Timer { Interval = 1 })
                    {
                        timer.Tick += (s, e) =>
                        {
                            timer.Stop();
                            try { result.TrySetResult(Open(entry)); }
                            catch (Exception)
                            { result.TrySetResult(L10n.T("无法打开此位置：目录可能已移动、无权访问或设备未连接", "Cannot open location: folder moved, access denied or device disconnected")); }
                            finally { context.ExitThread(); }
                        };
                        timer.Start();
                        Application.Run(context);
                    }
                }
                catch (Exception)
                {
                    // Always complete the UI request if COM/UI setup fails on this worker.
                    result.TrySetResult(L10n.T("无法完成资源管理器请求，请稍后重试", "Cannot complete the File Explorer request; please retry"));
                }
                finally { if (initialized >= 0) CoUninitialize(); }
            }) { IsBackground = true, Name = "Folder Peek folder navigation" };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            return result.Task;
        }
        private static string Open(Entry entry)
        {
            if (!PathRules.IsWindowsFileSystemPath(entry.FullPath)) return L10n.T("此路径不是文件系统位置", "This path is not a file system location");
            FileAttributes attributes = File.GetAttributes(entry.FullPath);
            bool directory = (attributes & FileAttributes.Directory) != 0;
            if (NavigationPolicy.Decide(entry.IsDirectory, directory) == NavigationAction.RejectChangedDirectory) return L10n.T("此位置不是目录，请选择目录或按 F5 刷新", "This location is not a folder; choose a folder or press F5");
            IntPtr folderPidl = IntPtr.Zero;
            IntPtr itemPidl = IntPtr.Zero;
            try
            {
                string folderPath = directory ? entry.FullPath : Path.GetDirectoryName(entry.FullPath);
                uint shellAttributes;
                Marshal.ThrowExceptionForHR(SHParseDisplayName(folderPath, IntPtr.Zero, out folderPidl, SfgaoFolder | SfgaoFileSystem, out shellAttributes));
                if (folderPidl == IntPtr.Zero || (shellAttributes & (SfgaoFolder | SfgaoFileSystem)) != (SfgaoFolder | SfgaoFileSystem))
                    return L10n.T("此位置不是可打开的文件系统目录", "This location is not an accessible file system folder");
                if (directory)
                {
                    object shell = null;
                    try
                    {
                        // Shell.Explore is specifically folder navigation, not a default file verb.
                        // cidl=0 in SHOpenFolderAndSelectItems would select this folder in its parent.
                        shell = Activator.CreateInstance(Type.GetTypeFromProgID("Shell.Application", true));
                        ((dynamic)shell).Explore(folderPath);
                    }
                    finally { if (shell != null && Marshal.IsComObject(shell)) Marshal.ReleaseComObject(shell); }
                }
                else
                {
                    uint unused;
                    Marshal.ThrowExceptionForHR(SHParseDisplayName(entry.FullPath, IntPtr.Zero, out itemPidl, 0, out unused));
                    IntPtr child = ILFindLastID(itemPidl);
                    if (child == IntPtr.Zero) return L10n.T("无法定位此文件", "Cannot locate this file");
                    Marshal.ThrowExceptionForHR(SHOpenFolderAndSelectItems(folderPidl, 1, new[] { child }, 0));
                }
                return directory ? L10n.T("已请求资源管理器打开目录", "Requested File Explorer to open the folder") : L10n.T("已请求资源管理器定位文件（不执行文件）", "Requested File Explorer to locate the file (without executing it)");
            }
            finally
            {
                if (itemPidl != IntPtr.Zero) Marshal.FreeCoTaskMem(itemPidl);
                if (folderPidl != IntPtr.Zero) Marshal.FreeCoTaskMem(folderPidl);
            }
        }
        [DllImport("ole32.dll")]
        private static extern int CoInitializeEx(IntPtr reserved, uint flags);
        [DllImport("ole32.dll")]
        private static extern void CoUninitialize();
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHParseDisplayName(string name, IntPtr bindingContext, out IntPtr pidl, uint attributesIn, out uint attributesOut);
        [DllImport("shell32.dll")]
        private static extern int SHOpenFolderAndSelectItems(IntPtr folder, uint count, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] IntPtr[] children, uint flags);
        [DllImport("shell32.dll")]
        private static extern IntPtr ILFindLastID(IntPtr pidl);
    }
}
