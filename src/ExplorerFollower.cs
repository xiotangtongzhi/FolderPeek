using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace FolderPeek
{
    internal sealed class ExplorerSnapshot
    {
        public IntPtr Frame { get; set; }
        public string Path { get; set; }
        public string Status { get; set; }
        public DateTime ObservedUtc { get; set; } = DateTime.UtcNow;
    }

    internal sealed class ExplorerFollower : IDisposable
    {
        private readonly Thread thread;
        private readonly Action<ExplorerSnapshot> callback;
        private volatile bool enabled;
        private volatile bool disposed;
        private Control dispatcher;
        public bool Enabled { get => enabled; set => enabled = value; }
        public ExplorerFollower(Action<ExplorerSnapshot> callback)
        {
            this.callback = callback;
            thread = new Thread(Run) { IsBackground = true, Name = "Folder Peek Explorer STA" };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }
        private void Run()
        {
            using (var control = new Control())
            using (var timer = new System.Windows.Forms.Timer { Interval = 650 })
            using (var context = new ApplicationContext())
            {
                var handle = control.Handle;
                dispatcher = control;
                timer.Tick += (s, e) =>
                {
                    if (disposed) { context.ExitThread(); return; }
                    if (!enabled) return;
                    timer.Stop();
                    ExplorerSnapshot snapshot;
                    try { snapshot = Capture(); }
                    catch (Exception ex) when (ex is COMException || ex is InvalidCastException || ex is InvalidOperationException || ex is System.Reflection.TargetInvocationException || ex is Microsoft.CSharp.RuntimeBinder.RuntimeBinderException || ex is System.ComponentModel.Win32Exception || ex is System.Security.SecurityException)
                    { snapshot = new ExplorerSnapshot { Status = L10n.T("暂时无法读取资源管理器；可手动选择目录", "Cannot read File Explorer right now; choose a folder manually") }; }
                    finally { if (!disposed) timer.Start(); }
                    if (enabled && !disposed && snapshot != null) callback(snapshot);
                };
                timer.Start();
                if (!disposed) Application.Run(context);
                dispatcher = null;
            }
        }

        private static ExplorerSnapshot Capture()
        {
            IntPtr frame = Native.TopExplorerWindow();
            if (frame == IntPtr.Zero) return new ExplorerSnapshot { Status = L10n.T("未发现打开的资源管理器文件夹 · 可选择文件夹", "No open File Explorer folder found; choose a folder") };
            var className = new StringBuilder(128);
            Native.GetClassName(frame, className, className.Capacity);
            if (className.ToString() != "CabinetWClass" && className.ToString() != "ExploreWClass") return null;
            uint processId;
            Native.GetWindowThreadProcessId(frame, out processId);
            try
            {
                using (var process = Process.GetProcessById((int)processId))
                    if (!string.Equals(process.ProcessName, "explorer", StringComparison.OrdinalIgnoreCase)) return null;
            }
            catch (ArgumentException) { return null; }
            var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            bool uncertain = false;
            bool matched = false;
            bool visibleVirtualLocation = false;
            object shell = null, windows = null;
            try
            {
                Type shellType = Type.GetTypeFromProgID("Shell.Application", false);
                if (shellType == null) return new ExplorerSnapshot { Frame = frame, Status = L10n.T("资源管理器接口不可用；请手动选择目录", "File Explorer interface unavailable; choose a folder manually") };
                shell = Activator.CreateInstance(shellType);
                windows = ((dynamic)shell).Windows();
                int count = ((dynamic)windows).Count;
                for (int i = 0; i < count; i++)
                {
                    object window = null, browserObject = null, view = null, folder = null;
                    IntPtr pidl = IntPtr.Zero;
                    try
                    {
                        window = ((dynamic)windows).Item(i);
                        if (window == null) continue;
                        var shellHandle = new IntPtr((long)((dynamic)window).HWND);
                        if (shellHandle != frame && Native.GetAncestor(shellHandle,2) != frame) continue;
                        matched = true;
                        var provider = window as IComServiceProvider;
                        if (provider == null) { uncertain = true; continue; }
                        Guid service = new Guid("4C96BE40-915C-11CF-99D3-00AA004AE837");
                        Guid browserId = typeof(IShellBrowser).GUID;
                        if (provider.QueryService(ref service, ref browserId, out browserObject) < 0 || browserObject == null)
                        { uncertain = true; continue; }
                        var browser = (IShellBrowser)browserObject;
                        // Windows 11 registers multiple tabs under one frame. Skip inactive tabs
                        // before querying their view; a stale hidden tab must not veto the active one.
                        IntPtr browserWindow;
                        if (browser.GetWindow(out browserWindow) >= 0 && browserWindow != IntPtr.Zero && browserWindow != frame)
                            if (!Native.IsWindowVisible(browserWindow) || !Native.IsChild(frame,browserWindow)) continue;
                        if (browser.QueryActiveShellView(out view) < 0 || view == null) { uncertain = true; continue; }
                        IntPtr viewWindow;
                        var oleWindow = view as IShellViewWindow;
                        if (oleWindow == null || oleWindow.GetWindow(out viewWindow) < 0 || viewWindow == IntPtr.Zero)
                        { uncertain = true; continue; }
                        // Win11 tabs are not guaranteed by ShellWindows. Visibility is a conservative
                        // filter only; differing surviving paths or any unresolved candidate are rejected.
                        if (!Native.IsWindowVisible(viewWindow) || !Native.IsChild(frame, viewWindow)) continue;
                        var folderView = view as IFolderView;
                        Guid persistId = typeof(IPersistFolder2).GUID;
                        if (folderView == null || folderView.GetFolder(ref persistId, out folder) < 0 || folder == null)
                        { uncertain = true; continue; }
                        if (((IPersistFolder2)folder).GetCurFolder(out pidl) < 0 || pidl == IntPtr.Zero)
                        { uncertain = true; continue; }
                        var buffer = new StringBuilder(32768);
                        if (!Native.SHGetPathFromIDListEx(pidl, buffer, (uint)buffer.Capacity, 0))
                        { visibleVirtualLocation = true; continue; }
                        string path = buffer.ToString();
                        if (PathRules.IsWindowsFileSystemPath(path))
                        {
                            // I/O can stall while Explorer navigates in the same frame. Re-query the
                            // view's folder after validation and do not publish stale tab paths.
                            string currentPath = ReadCurrentViewPath(folderView);
                            if (!PathRules.SamePath(path, currentPath) || !Native.IsWindowVisible(viewWindow) || !Native.IsChild(frame, viewWindow)) return null;
                            paths.Add(path);
                        }
                        else visibleVirtualLocation = true;
                    }
                    catch (Exception ex) when (ex is COMException || ex is InvalidCastException || ex is Microsoft.CSharp.RuntimeBinder.RuntimeBinderException || ex is IOException || ex is UnauthorizedAccessException || ex is System.Security.SecurityException)
                    { uncertain = true; }
                    finally
                    {
                        if (pidl != IntPtr.Zero) Marshal.FreeCoTaskMem(pidl);
                        Release(folder); Release(view); Release(browserObject); Release(window);
                    }
                }
            }
            finally { Release(windows); Release(shell); }
            if (Native.TopExplorerWindow() != frame) return null;
            if (uncertain || paths.Count > 1 || (visibleVirtualLocation && paths.Count > 0))
                return new ExplorerSnapshot { Frame = frame, Status = L10n.T("未能确认活动标签页，保留当前目录；可手动选择", "Cannot determine the active tab; keeping the current folder") };
            foreach (string path in paths) return new ExplorerSnapshot { Frame = frame, Path = path, ObservedUtc = DateTime.UtcNow };
            return new ExplorerSnapshot { Frame = frame, Status = matched ? L10n.T("此资源管理器位置不是可读取的文件夹", "This File Explorer location is not a readable folder") : L10n.T("暂未识别此资源管理器窗口；可手动选择目录", "File Explorer window not recognized; choose a folder manually") };
        }
        private static string ReadCurrentViewPath(IFolderView view)
        {
            object folder = null;
            IntPtr pidl = IntPtr.Zero;
            try
            {
                Guid iid = typeof(IPersistFolder2).GUID;
                Marshal.ThrowExceptionForHR(view.GetFolder(ref iid, out folder));
                if (folder == null) return null;
                Marshal.ThrowExceptionForHR(((IPersistFolder2)folder).GetCurFolder(out pidl));
                if (pidl == IntPtr.Zero) return null;
                var buffer = new StringBuilder(32768);
                return Native.SHGetPathFromIDListEx(pidl, buffer, (uint)buffer.Capacity, 0) ? buffer.ToString() : null;
            }
            finally { if (pidl != IntPtr.Zero) Marshal.FreeCoTaskMem(pidl); Release(folder); }
        }
        private static void Release(object obj)
        {
            if (obj != null && Marshal.IsComObject(obj))
                try { Marshal.ReleaseComObject(obj); } catch (InvalidComObjectException) { }
        }
        public void Dispose()
        {
            disposed = true;
            enabled = false;
            var control = dispatcher;
            if (control != null && !control.IsDisposed)
                try { control.BeginInvoke((Action)(() => Application.ExitThread())); } catch (InvalidOperationException) { }
            // Do not block app exit if an unresponsive Explorer COM call is still pending.
        }
    }

    internal static class Native
    {
        internal delegate bool EnumWindowProc(IntPtr hwnd, IntPtr param);
        [DllImport("user32.dll")] internal static extern bool EnumWindows(EnumWindowProc callback, IntPtr param);
        [DllImport("user32.dll")] internal static extern bool IsIconic(IntPtr hwnd);
        [DllImport("dwmapi.dll")] private static extern int DwmGetWindowAttribute(IntPtr hwnd, int attribute, out int value, int size);
        internal static IntPtr TopExplorerWindow()
        {
            IntPtr found = IntPtr.Zero;
            EnumWindows((hwnd, unused) =>
            {
                if (!IsWindowVisible(hwnd) || IsIconic(hwnd)) return true;
                var name = new StringBuilder(128); GetClassName(hwnd,name,name.Capacity);
                if (name.ToString() != "CabinetWClass" && name.ToString() != "ExploreWClass") return true;
                int cloaked; if (DwmGetWindowAttribute(hwnd,14,out cloaked,4) == 0 && cloaked != 0) return true;
                found = hwnd; return false;
            },IntPtr.Zero);
            return found;
        }
        [DllImport("user32.dll")] internal static extern IntPtr GetAncestor(IntPtr hwnd,uint flags);
        [DllImport("user32.dll")] internal static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] internal static extern int GetClassName(IntPtr window, StringBuilder name, int count);
        [DllImport("user32.dll")] internal static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
        [DllImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] internal static extern bool IsWindowVisible(IntPtr window);
        [DllImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] internal static extern bool IsChild(IntPtr parent, IntPtr child);
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)][return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SHGetPathFromIDListEx(IntPtr pidl, StringBuilder path, uint size, uint flags);
    }
    [ComImport, Guid("6D5140C1-7436-11CE-8034-00AA006009FA"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IComServiceProvider
    { [PreserveSig] int QueryService(ref Guid service, ref Guid iid, [MarshalAs(UnmanagedType.Interface)] out object result); }
    [ComImport, Guid("000214E3-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IShellViewWindow
    { [PreserveSig] int GetWindow(out IntPtr window); [PreserveSig] int ContextSensitiveHelp([MarshalAs(UnmanagedType.Bool)] bool enter); }
    [ComImport, Guid("00000114-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IOleWindow
    { [PreserveSig] int GetWindow(out IntPtr window); [PreserveSig] int ContextSensitiveHelp([MarshalAs(UnmanagedType.Bool)] bool enter); }
    [ComImport, Guid("000214E2-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IShellBrowser
    {
        [PreserveSig] int GetWindow(out IntPtr window);
        [PreserveSig] int ContextSensitiveHelp([MarshalAs(UnmanagedType.Bool)] bool enter);
        [PreserveSig] int InsertMenusSB(IntPtr menu, IntPtr widths);
        [PreserveSig] int SetMenuSB(IntPtr menu, IntPtr reserved, IntPtr active);
        [PreserveSig] int RemoveMenusSB(IntPtr menu);
        [PreserveSig] int SetStatusTextSB([MarshalAs(UnmanagedType.LPWStr)] string text);
        [PreserveSig] int EnableModelessSB([MarshalAs(UnmanagedType.Bool)] bool enable);
        [PreserveSig] int TranslateAcceleratorSB(IntPtr message, ushort id);
        [PreserveSig] int BrowseObject(IntPtr pidl, uint flags);
        [PreserveSig] int GetViewStateStream(uint mode, out IntPtr stream);
        [PreserveSig] int GetControlWindow(uint id, out IntPtr window);
        [PreserveSig] int SendControlMsg(uint id, uint message, IntPtr wparam, IntPtr lparam, out IntPtr result);
        [PreserveSig] int QueryActiveShellView([MarshalAs(UnmanagedType.Interface)] out object view);
    }
    [ComImport, Guid("cde725b0-ccc9-4519-917e-325d72fab4ce"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IFolderView
    {
        [PreserveSig] int GetCurrentViewMode(out uint mode);
        [PreserveSig] int SetCurrentViewMode(uint mode);
        [PreserveSig] int GetFolder(ref Guid iid, [MarshalAs(UnmanagedType.Interface)] out object folder);
    }
    [ComImport, Guid("1AC3D9F0-175C-11D1-95BE-00609797EA4F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IPersistFolder2
    {
        [PreserveSig] int GetClassID(out Guid classId);
        [PreserveSig] int Initialize(IntPtr pidl);
        [PreserveSig] int GetCurFolder(out IntPtr pidl);
    }
}




