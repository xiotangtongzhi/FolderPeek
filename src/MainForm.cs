using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FolderPeek
{
    internal sealed class MainForm : Form
    {
        private readonly MapCanvas map = new MapCanvas();
        private readonly MixedPathBox pathBox = new MixedPathBox();
        private readonly CheckBox follow = new SquareCheckBox { Text = L10n.T("自动跟随", "Auto follow"), Checked = true, AutoSize = true };
        private readonly CheckBox files = new SquareCheckBox { Text = L10n.T("显示文件", "Show files"), Checked = true, AutoSize = true };
        private readonly CheckBox pin = new SquareCheckBox { Text = L10n.T("窗口置顶", "Always on top"), AutoSize = true };
        private readonly DepthPicker depth = new DepthPicker { Width = 92 };
        private readonly SquareButton themeButton = new SquareButton { ThemeSymbol = true, Width = 44, Height = 40, FlatStyle = FlatStyle.Flat, Margin = new Padding(10,0,0,0), AccessibleName = L10n.T("日间模式，点击切换夜间模式", "Day mode; switch to night mode") };
        private readonly SquareButton collapseButton = new SquareButton { CollapseSymbol=true, Height=26, Width=48, FlatStyle=FlatStyle.Flat, Margin=Padding.Empty, Cursor=Cursors.Hand, AccessibleName=L10n.T("收起顶部名称、工具栏和路径", "Hide title, toolbar and path") };
        private TableLayoutPanel contentLayout;
        private FlowLayoutPanel toolbar;
        private bool collapsed;
        private TableLayoutPanel chromeLayout;
        private Panel titlePanel;
        private float titleRowHeight, pathRowHeight;
        private readonly ToolTip tips = new ToolTip();
        private readonly SemaphoreSlim workers = new SemaphoreSlim(2,2);
        private readonly Dictionary<string,int> limits = new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);
        private CancellationTokenSource cancel = new CancellationTokenSource();
        private readonly AppSettings settings;
        private readonly string settingsFile;
        private ExplorerFollower follower;
        private string rootPath, lastExplorerPath;
        private IntPtr lastExplorerFrame;
        private int generation;
        private bool closing, choosing, opening;
        public MainForm(string settingsOverride = null)
        {
            settingsFile = settingsOverride ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"FolderPeek","settings-v1.5.xml");
            settings = SettingsStore.Load(settingsFile);
            settings.PageSize = Math.Max(1,Math.Min(100,settings.PageSize));
            Text = "Folder Peek · v1.5"; Font = MixedText.English; AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Color.White; ForeColor = Color.FromArgb(40,55,69); MinimumSize = new Size(800,360);
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(Math.Max(800,Math.Min(settings.Width,1800)),Math.Max(360,Math.Min(settings.Height,1400)));
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
            FormBorderStyle = FormBorderStyle.None; Padding = new Padding(1);
            BuildUi(); follow.Checked = settings.FollowExplorer; files.Checked = settings.ShowFiles; pin.Checked = settings.AlwaysOnTop;
            TopMost = pin.Checked;
            Theme.Tooltip(tips,() => settings.DarkMode,() => Font); ApplyTheme();
            HandleCreated += (s,e) => Theme.Caption(Handle,settings.DarkMode);
            if (settings.HasBounds)
            {
                var r = new Rectangle(settings.X,settings.Y,Width,Height);
                foreach (var screen in Screen.AllScreens)
                    if (Rectangle.Intersect(screen.WorkingArea,r).Width >= 180 && Rectangle.Intersect(screen.WorkingArea,r).Height >= 100)
                    { StartPosition = FormStartPosition.Manual; Location = r.Location; break; }
            }
            Shown += (s,e) =>
            {
                if (PathRules.IsWindowsFileSystemPath(settings.RootPath)) SetRoot(settings.RootPath);
                follower = new ExplorerFollower(ReceiveSnapshot) { Enabled = follow.Checked };
                if(settings.Collapsed)ToggleCollapse();
            };
            KeyPreview = true;
            KeyDown += (s,e) =>
            {
                if (e.KeyCode == Keys.F5) { Reload(); e.Handled = true; }
                if (e.Control && e.KeyCode == Keys.O) { Choose(); e.SuppressKeyPress = true; }
                if (e.Control && e.KeyCode == Keys.L) { pathBox.Focus(); pathBox.SelectAll(); e.SuppressKeyPress = true; }
            };
            FormClosing += (s,e) =>
            {
                closing = true; follower?.Dispose(); cancel.Cancel();
                var b = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
                settings.RootPath = rootPath; settings.FollowExplorer = follow.Checked; settings.ShowFiles = files.Checked; settings.AlwaysOnTop = pin.Checked;
                settings.ExpandDepth = SelectedDepth; settings.HasBounds = true; settings.X = b.X; settings.Y = b.Y; settings.Width = (int)Math.Round(b.Width*96f/DeviceDpi); settings.Height = (int)Math.Round(b.Height*96f/DeviceDpi); settings.Collapsed = collapsed;
                SettingsStore.Save(settingsFile,settings);
            };
        }
        private int SelectedDepth { get { return new[] {1,2,3,5,8,12}[Math.Max(0,depth.SelectedIndex)]; } }
        private Button Button(string text, Action action)
        {
            var b = new SquareButton { Text = text, AutoSize = true, Height = 40, FlatStyle = FlatStyle.Flat, BackColor = Color.White, Cursor = Cursors.Hand, Margin = new Padding(0,0,7,0), Padding = new Padding(4,0,4,0) };
            b.FlatAppearance.BorderColor = Color.FromArgb(222,230,236); b.FlatAppearance.MouseOverBackColor = Color.FromArgb(235,242,248);
            b.Click += (s,e) => action(); return b;
        }
        private void BuildUi()
        {
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(12,12,12,8) };
            contentLayout = layout;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute,48));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute,32));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));
            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = true, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Margin = new Padding(0,0,0,8) };
            toolbar = actions;
            actions.Controls.Add(Button(L10n.T("选择文件夹", "Choose folder"),Choose));
            actions.Controls.Add(Button(L10n.T("上一级", "Up"),() => { if (rootPath == null) return; try { var p = Directory.GetParent(rootPath); if (p != null) SetRoot(p.FullName); } catch (Exception ex) when (ex is ArgumentException || ex is IOException || ex is System.Security.SecurityException) { Hint(DirectoryPager.FriendlyError(ex)); } }));
            actions.Controls.Add(Button(L10n.T("刷新", "Refresh"),Reload));
            foreach (var c in new[] { follow,files,pin }) { c.Margin = new Padding(9,6,5,0); actions.Controls.Add(c); }
            follow.CheckedChanged += (s,e) => { lastExplorerFrame = IntPtr.Zero; lastExplorerPath = null; if (follower != null) follower.Enabled = follow.Checked; Hint(follow.Checked ? L10n.T("自动跟随已开启：下一次切换文件夹继续更新", "Auto follow enabled: updates on the next folder change") : L10n.T("自动跟随已暂停", "Auto follow paused")); };
            files.CheckedChanged += (s,e) => Reload(); pin.CheckedChanged += (s,e) => TopMost = pin.Checked;
            var depthGroup = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = false, Margin = new Padding(8,0,0,0) };
            depthGroup.Controls.Add(new Label { Font = L10n.English ? MixedText.English : MixedText.Chinese, Text = L10n.T("展开深度", "Depth"),AutoSize = true,Margin = new Padding(0,6,5,0) });
            depth.Items.AddRange(new object[] { L10n.T("1 层", "1"),L10n.T("2 层", "2"),L10n.T("3 层", "3"),L10n.T("5 层", "5"),L10n.T("8 层", "8"),L10n.T("12 层", "12") });
            int di = Array.IndexOf(new[] {1,2,3,5,8,12},settings.ExpandDepth); depth.SelectedIndex = di < 0 ? 3 : di;
            depth.Margin = new Padding(0,3,0,0); depth.SelectedIndexChanged += (s,e) => Reload(); depthGroup.Controls.Add(depth); actions.Controls.Add(depthGroup);
            themeButton.Click += (s,e) => { settings.DarkMode = !settings.DarkMode; ApplyTheme(); }; actions.Controls.Add(themeButton);
            tips.SetToolTip(follow,L10n.T("只跟随最前面的资源管理器窗口；手动查看分支不会关闭此开关", "Follows the frontmost File Explorer window; browsing branches keeps follow enabled"));
            tips.SetToolTip(depth,L10n.T("自动展开的层数；每个文件夹默认先显示 5 项", "Number of levels to expand; each folder initially shows 5 items"));
            layout.Controls.Add(actions,0,0);
            pathBox.Dock = DockStyle.Fill; pathBox.Margin = new Padding(0,1,0,7); pathBox.BorderStyle = BorderStyle.FixedSingle;
            pathBox.AccessibleName = L10n.T("当前文件夹完整路径", "Current folder path");
            pathBox.KeyDown += (s,e) => { if (e.KeyCode != Keys.Enter) return; e.SuppressKeyPress = true; string p = pathBox.Text.Trim().Trim('"'); if (!PathRules.IsWindowsFileSystemPath(p)) { Hint(L10n.T("请输入完整文件夹路径", "Enter a full folder path")); return; } SetRoot(p); };
            layout.Controls.Add(pathBox,0,1);
            map.Dock = DockStyle.Fill; map.Margin = Padding.Empty; map.PageSize = settings.PageSize;
            map.FocusNode = n => SetRoot(n.Entry.FullPath);
            map.ShowMore = n => { int count; if (!limits.TryGetValue(n.Entry.FullPath,out count)) count = settings.PageSize; limits[n.Entry.FullPath] = Math.Min(100000,count+settings.PageSize); LoadRoot(rootPath,false,true); };
            map.PageSizeChanged = value => { settings.PageSize = value; map.PageSize = value; limits.Clear(); Reload(); };
            map.OpenNode = async n => { if (opening) return; opening = true; try { string result = await ShellNavigator.OpenAsync(n.Entry); if (!closing) Hint(result); } finally { opening = false; } };
            var foldRow = new FlowLayoutPanel { Dock=DockStyle.Fill, WrapContents=false, Margin=Padding.Empty, Padding=Padding.Empty };
            foldRow.MouseDown += (s,e) => { if(e.Button==MouseButtons.Left && collapsed){ReleaseCapture();SendMessage(Handle,0xA1,new IntPtr(2),IntPtr.Zero);} };
            foldRow.DoubleClick += (s,e) => ToggleMaximize();
            collapseButton.Click += (s,e) => ToggleCollapse();tips.SetToolTip(collapseButton,L10n.T("收起顶部名称、工具栏和路径", "Hide title, toolbar and path"));foldRow.Controls.Add(collapseButton);
            layout.Controls.Add(foldRow,0,2);
            layout.Controls.Add(map,0,3);
            var chrome = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = Padding.Empty };
            chromeLayout=chrome;
            chrome.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));chrome.RowStyles.Add(new RowStyle(SizeType.Absolute,42));chrome.RowStyles.Add(new RowStyle(SizeType.Percent,100));
            var title = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty };
            titlePanel=title;
            var titleText = new Label { Text = "Folder Peek · v1.5", AutoSize = false, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(12,0,0,0) };
            titleText.MouseDown += (s,e) => { if(e.Button==MouseButtons.Left) { ReleaseCapture();SendMessage(Handle,0xA1,new IntPtr(2),IntPtr.Zero); } };
            titleText.DoubleClick += (s,e) => ToggleMaximize();
            var windowButtons = new FlowLayoutPanel { Dock = DockStyle.Right, Width = 141, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Margin = Padding.Empty, Padding = Padding.Empty };
            foreach(var item in new[] { new { Text="−", Action=(Action)(()=>WindowState=FormWindowState.Minimized), Name=L10n.T("最小化", "Minimize") },new { Text="□", Action=(Action)ToggleMaximize, Name=L10n.T("最大化或还原", "Maximize or restore") },new { Text="×", Action=(Action)Close, Name=L10n.T("关闭", "Close") } })
            {
                var action=item.Action;var b=new SquareButton { Text=item.Text,Width=47,Height=40,FlatStyle=FlatStyle.Flat,Margin=Padding.Empty,AccessibleName=item.Name };b.Click+=(s,e)=>action();windowButtons.Controls.Add(b);
            }
            title.Controls.Add(titleText);title.Controls.Add(windowButtons);chrome.Controls.Add(title,0,0);chrome.Controls.Add(layout,0,1);Controls.Add(chrome);
        }
        [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern bool ReleaseCapture();
        [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hwnd,int message,IntPtr wparam,IntPtr lparam);
        private void ToggleMaximize() { MaximizedBounds=Screen.FromControl(this).WorkingArea;WindowState=WindowState==FormWindowState.Maximized?FormWindowState.Normal:FormWindowState.Maximized; }
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if(m.Msg==0x84 && WindowState==FormWindowState.Normal)
            {
                int raw=unchecked((int)m.LParam.ToInt64());var p=PointToClient(new Point((short)(raw&65535),(short)(raw>>16)));const int edge=6;
                bool left=p.X<edge,right=p.X>=Width-edge,top=p.Y<edge,bottom=p.Y>=Height-edge;
                if(top&&left)m.Result=new IntPtr(13);else if(top&&right)m.Result=new IntPtr(14);else if(bottom&&left)m.Result=new IntPtr(16);else if(bottom&&right)m.Result=new IntPtr(17);
                else if(left)m.Result=new IntPtr(10);else if(right)m.Result=new IntPtr(11);else if(top)m.Result=new IntPtr(12);else if(bottom)m.Result=new IntPtr(15);
            }
        }
        private void ToggleCollapse()
        {
            SuspendLayout();chromeLayout.SuspendLayout();contentLayout.SuspendLayout();
            if(!collapsed)
            {
                titleRowHeight=chromeLayout.RowStyles[0].Height;pathRowHeight=contentLayout.RowStyles[1].Height;
                collapsed=true;toolbar.Visible=false;pathBox.Visible=false;titlePanel.Visible=false;
                chromeLayout.RowStyles[0].Height=0;
                contentLayout.RowStyles[0].SizeType=SizeType.Absolute;contentLayout.RowStyles[0].Height=0;
                contentLayout.RowStyles[1].Height=0;
            }
            else
            {
                collapsed=false;chromeLayout.RowStyles[0].Height=titleRowHeight;
                contentLayout.RowStyles[0].SizeType=SizeType.AutoSize;contentLayout.RowStyles[1].Height=pathRowHeight;
                titlePanel.Visible=true;toolbar.Visible=true;pathBox.Visible=true;
            }
            collapseButton.Collapsed=collapsed;collapseButton.AccessibleName=collapsed?L10n.T("展开顶部名称、工具栏和路径", "Show title, toolbar and path"):L10n.T("收起顶部名称、工具栏和路径", "Hide title, toolbar and path");
            tips.SetToolTip(collapseButton,collapseButton.AccessibleName);collapseButton.Invalidate();
            contentLayout.ResumeLayout(true);chromeLayout.ResumeLayout(true);ResumeLayout(true);
        }
        private void ApplyTheme()
        {
            Theme.Apply(this,settings.DarkMode); pathBox.Visible = !collapsed; map.SetTheme(settings.DarkMode);
            themeButton.Dark = settings.DarkMode;
            themeButton.AccessibleName = settings.DarkMode ? L10n.T("夜间模式，点击切换日间模式", "Night mode; switch to day mode") : L10n.T("日间模式，点击切换夜间模式", "Day mode; switch to night mode");
            tips.SetToolTip(themeButton,themeButton.AccessibleName);
            if(IsHandleCreated)Theme.Caption(Handle,settings.DarkMode);
            Invalidate(true);
        }
        private void Hint(string text) { tips.SetToolTip(pathBox,(rootPath ?? "")+"\n"+text); map.Message = text; map.AccessibleDescription = text; map.Invalidate(); }
        private void Reload() { if (rootPath != null) LoadRoot(rootPath,false,true); }
        private void Choose()
        {
            choosing = true; if (follower != null) follower.Enabled = false;
            try { using (var d = new FolderBrowserDialog { Description = L10n.T("选择要查看的文件夹", "Choose a folder to view"), ShowNewFolderButton = false, SelectedPath = rootPath ?? "" }) if (d.ShowDialog(this) == DialogResult.OK) SetRoot(d.SelectedPath); }
            finally { choosing = false; if (follower != null) follower.Enabled = follow.Checked; }
        }
        private void ReceiveSnapshot(ExplorerSnapshot snapshot)
        {
            if (closing || !IsHandleCreated) return;
            try { BeginInvoke((Action)(() =>
            {
                if (closing || choosing || !follow.Checked || (DateTime.UtcNow-snapshot.ObservedUtc).TotalSeconds > 5) return;
                if (snapshot.Frame != IntPtr.Zero && Native.TopExplorerWindow() != snapshot.Frame) return;
                if (snapshot.Path != null)
                {
                    bool changed = snapshot.Frame != lastExplorerFrame || !PathRules.SamePath(lastExplorerPath,snapshot.Path);
                    lastExplorerFrame = snapshot.Frame; lastExplorerPath = snapshot.Path;
                    if (changed && !PathRules.SamePath(rootPath,snapshot.Path)) SetRoot(snapshot.Path);
                }
                else if (snapshot.Status != null) { lastExplorerFrame = IntPtr.Zero; lastExplorerPath = null; Hint(snapshot.Status); }
            })); } catch (InvalidOperationException) { }
        }
        private void SetRoot(string path) { LoadRoot(path,true,false); }
        private async void LoadRoot(string path,bool resetLimits,bool preserveView)
        {
            if (path == null) return;
            var old = cancel; old.Cancel(); cancel = new CancellationTokenSource(); old.Dispose();
            var token = cancel.Token; int current = ++generation;
            if (resetLimits) limits.Clear();
            rootPath = path; pathBox.Text = path; pathBox.Visible = !collapsed; Text = "Folder Peek · v1.5";
            if (!preserveView) map.SetSnapshot(null);
            Hint(L10n.T("正在读取文件夹数量和分支…", "Reading folder counts and branches..."));
            int levels = SelectedDepth; bool includeFiles = files.Checked; int pageSize = settings.PageSize;
            var pageLimits = new Dictionary<string,int>(limits,StringComparer.OrdinalIgnoreCase);
            try
            {
                await workers.WaitAsync(token); MapSnapshot snapshot;
                try { snapshot = await Task.Run(() => MapSnapshot.Read(path,includeFiles,levels,2500,pageSize,pageLimits,token),token); }
                finally { workers.Release(); }
                if (closing || token.IsCancellationRequested || current != generation) return;
                map.SetSnapshot(snapshot,preserveView); Hint(L10n.T("中键拖动 · 点击展示更多追加 ", "Middle-drag to pan; Show more adds ")+pageSize+L10n.T(" 项 · F5 刷新", " items; F5 refreshes"));
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException || ex is System.Security.SecurityException || ex is NotSupportedException)
            { if (!closing && current == generation) Hint(DirectoryPager.FriendlyError(ex)); }
        }
        protected override void Dispose(bool disposing) { if (disposing) tips.Dispose(); base.Dispose(disposing); }
    }
}








