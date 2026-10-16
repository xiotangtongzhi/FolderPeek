using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace FolderPeek
{
    internal sealed class MapCanvas : Control
    {
        private sealed class Row { public MapNode Node; public RectangleF Rect; public Row Parent; public bool More; public int Branch; public float LineY { get { return Rect.Bottom-3; } } }
        private readonly List<Row> rows = new List<Row>();
        private readonly ToolTip tip = new ToolTip();
        private readonly ContextMenuStrip menu = new ContextMenuStrip();
        private Row selected, hovered;
        private MapSnapshot snapshot;
        private bool dark;
        private float zoom = 1;
        private PointF offset;
        private Point dragStart;
        private PointF dragOffset;
        private bool dragging;
        private readonly Color[] accents = { Color.FromArgb(66,107,146),Color.FromArgb(63,129,117),Color.FromArgb(141,111,158),Color.FromArgb(165,130,76) };
        public Action<MapNode> FocusNode, OpenNode, ShowMore;
        public Action<int> PageSizeChanged;
        public int PageSize = 5;
        public string Message = L10n.T("打开资源管理器文件夹，或点击“选择文件夹”", "Open a folder in File Explorer, or click Choose folder");
        public MapCanvas()
        {
            SetStyle(ControlStyles.Selectable | ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint,true);
            BackColor = Color.FromArgb(250,251,252); TabStop = true;
            AccessibleName = L10n.T("目录分支树：中键拖动，滚轮上下移动，Ctrl+滚轮缩放", "Folder tree: middle-drag to pan, wheel to scroll, Ctrl+wheel to zoom");
            menu.Items.Add(L10n.T("聚焦此文件夹", "Focus this folder"),null,(s,e) => { if (selected != null && !selected.More && selected.Node.Entry.IsDirectory) FocusNode?.Invoke(selected.Node); });
            menu.Items.Add(L10n.T("在资源管理器打开 / 定位", "Open / locate in File Explorer"),null,(s,e) => { if (selected != null) OpenNode?.Invoke(selected.Node); });
            menu.Items.Add(L10n.T("复制完整路径", "Copy full path"),null,(s,e) => { if (selected != null) try { Clipboard.SetText(selected.Node.Entry.FullPath); } catch (System.Runtime.InteropServices.ExternalException) {} });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(L10n.T("回到根目录视图", "Reset tree view"),null,(s,e) => { offset = PointF.Empty; zoom = 1; Invalidate(); });
            var pageMenu = new ToolStripMenuItem(L10n.T("每次展示数量", "Items per page"));
            foreach (int count in new[] { 5,10,20 }) { int value = count; pageMenu.DropDownItems.Add(count+L10n.T(" 项", " items"),null,(s,e) => PageSizeChanged?.Invoke(value)); }
            menu.Items.Add(pageMenu);
            ContextMenuStrip = menu;
            Theme.Tooltip(tip,() => dark,() => Font);
            tip.SetToolTip(this,L10n.T("按住鼠标中键拖动；Ctrl+滚轮缩放；双击文件夹聚焦", "Middle-drag to pan; Ctrl+wheel to zoom; double-click a folder to focus"));
        }
        public void SetTheme(bool value)
        {
            dark=value;BackColor=Theme.Background(dark);ForeColor=Theme.Text(dark);
            Theme.Menu(menu,dark,Font);Invalidate();
        }
        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            if(menu!=null)Theme.Menu(menu,dark,Font);
            if(rows!=null && snapshot!=null)SetSnapshot(snapshot,true);
        }
        public void SetSnapshot(MapSnapshot value, bool preserveView = false)
        {
            snapshot = value; selected = hovered = null; tip.Hide(this);
            if (!preserveView) offset = PointF.Empty;
            rows.Clear(); float y = 26;
            if (snapshot != null) Place(snapshot.Root,null,0,ref y);
            Invalidate();
        }
        private void Place(MapNode node, Row parent, int branch, ref float y)
        {
            float x = 26 + node.Depth*110;
            string label = Label(node);
            int width = Math.Min(960,Math.Max(315,MixedText.Measure(label).Width+26));
            var row = new Row { Node = node,Parent = parent,Branch = branch,Rect = new RectangleF(x,y,width,43) };
            rows.Add(row); y += 52;
            for (int i=0;i<node.Children.Count;i++) Place(node.Children[i],row,parent == null ? i%accents.Length : branch,ref y);
            if (node.HasMore)
            {
                rows.Add(new Row { Node = node,Parent = row,More = true,Branch = branch,Rect = new RectangleF(x+110,y,450,43) }); y += 52;
            }
            if (node.Children.Count > 0 || node.HasMore) y += 7;
        }
        private string Label(MapNode node)
        {
            string counts = node.Entry.IsDirectory && (node.CountComplete || node.TotalFiles+node.TotalFolders > 0)
                ? (L10n.English ? "  (" : "  （")+node.TotalFolders+L10n.T(" 文件夹 · ", node.TotalFolders == 1 ? " folder / " : " folders / ")+node.TotalFiles+L10n.T(" 文件", node.TotalFiles == 1 ? " file" : " files")+(node.CountComplete ? "" : L10n.T("，已读取", ", counted so far"))+(L10n.English ? ")" : "）") : "";
            return node.Entry.Name + counts + (node.Note == null ? "" : "  · "+node.Note);
        }
        public float Zoom { get { return zoom; } }
        public void SetZoom(float value) { zoom = Math.Max(.5f,Math.Min(1.75f,value)); Invalidate(); }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e); var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TranslateTransform(offset.X,offset.Y); g.ScaleTransform(zoom,zoom);
            var visible = new RectangleF(-offset.X/zoom,-offset.Y/zoom,Width/zoom,Height/zoom);
            // A vertical trunk descends from each folder underline; children attach via horizontal lines.
            foreach (var row in rows)
            {
                if (row.Parent == null) continue;
                float trunk = row.Rect.Left;
                using (var p = new Pen(Color.FromArgb(dark ? 190 : 145,Theme.Level(row.Node.Depth,dark)),1.25f))
                    g.DrawLine(p,trunk,row.Parent.LineY,trunk,row.LineY);
            }
            using (var format = new StringFormat { Trimming = StringTrimming.EllipsisCharacter,FormatFlags = StringFormatFlags.NoWrap,LineAlignment = StringAlignment.Center })
            foreach (var row in rows)
            {
                if (!visible.IntersectsWith(row.Rect)) continue;
                var accent = Theme.Level(row.Node.Depth,dark);
                if (selected == row)
                    using (var p = new Pen(Theme.Border(dark))) g.DrawRectangle(p,row.Rect.X,row.Rect.Y,row.Rect.Width,row.Rect.Height);
                if (!row.More)
                {
                    using (var p = new Pen(Color.FromArgb(dark ? 200 : 170,accent),row.Parent == null ? 1.8f : 1.2f))
                        g.DrawLine(p,row.Rect.Left,row.LineY,row.Rect.Right,row.LineY);
                    using (var b = new SolidBrush(row.Node.Entry.IsDirectory ? accent : Theme.Text(dark)))
                        MixedText.Draw(g,Label(row.Node),new RectangleF(row.Rect.X+8,row.Rect.Y,row.Rect.Width-12,38),b.Color);
                }
                else
                {
                    int shown = row.Node.Children.Count;
                    string text = L10n.T("展示更多  +", "Show more  +")+PageSize+L10n.T("  · 已显示 ", "  / Shown: ")+shown+L10n.T(" 项", " items");
                    using (var b = new SolidBrush(accent)) MixedText.Draw(g,text,new RectangleF(row.Rect.X+8,row.Rect.Y,row.Rect.Width-12,38),b.Color);
                }
            }
            if (rows.Count == 0)
                using (var b = new SolidBrush(Theme.Text(dark))) MixedText.Draw(g,Message,new RectangleF(28,28,Math.Max(10,Width-56),Math.Max(30,Height-56)),b.Color);
        }
        private Row Hit(Point p)
        { var world = new PointF((p.X-offset.X)/zoom,(p.Y-offset.Y)/zoom); return rows.Find(r => r.Rect.Contains(world)); }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e); Focus();
            if (e.Button == MouseButtons.Middle) { dragging = true; dragStart = e.Location; dragOffset = offset; Capture = true; Cursor = Cursors.SizeAll; tip.Hide(this); return; }
            selected = Hit(e.Location); Invalidate();
            if (e.Button == MouseButtons.Left && selected != null)
            {
                if (selected.More) ShowMore?.Invoke(selected.Node);
                else if ((ModifierKeys & Keys.Control) != 0) OpenNode?.Invoke(selected.Node);
            }
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (dragging) { offset = new PointF(dragOffset.X+e.X-dragStart.X,dragOffset.Y+e.Y-dragStart.Y); Invalidate(); return; }
            var row = Hit(e.Location); if (hovered == row) return; hovered = row;
            tip.SetToolTip(this,row == null ? L10n.T("中键拖动 · Ctrl+滚轮缩放", "Middle-drag to pan; Ctrl+wheel to zoom") : row.Node.Entry.FullPath+"\n"+Label(row.Node)); Cursor = row == null ? Cursors.Default : Cursors.Hand;
        }
        protected override void OnMouseUp(MouseEventArgs e)
        { base.OnMouseUp(e); if (e.Button == MouseButtons.Middle) { dragging = false; Capture = false; Cursor = Cursors.Default; } }
        protected override void OnMouseCaptureChanged(EventArgs e)
        { base.OnMouseCaptureChanged(e); if (!Capture) { dragging = false; Cursor = Cursors.Default; } }
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if ((ModifierKeys & Keys.Control) != 0)
            {
                float old = zoom; SetZoom(zoom+(e.Delta>0 ? .1f : -.1f));
                offset = new PointF(e.X-(e.X-offset.X)*zoom/old,e.Y-(e.Y-offset.Y)*zoom/old);
            }
            else if ((ModifierKeys & Keys.Shift) != 0) offset.X += e.Delta/2;
            else offset.Y += e.Delta/2;
            Invalidate();
        }
        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e); var row = Hit(e.Location);
            if (e.Button != MouseButtons.Left || row == null || row.More || (ModifierKeys & Keys.Control) != 0) return;
            if (row.Node.Entry.IsDirectory) FocusNode?.Invoke(row.Node); else OpenNode?.Invoke(row.Node);
        }
        protected override bool IsInputKey(Keys keyData)
        { var k = keyData & Keys.KeyCode; return k == Keys.Up || k == Keys.Down || k == Keys.Home || base.IsInputKey(keyData); }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e); if (rows.Count == 0) return;
            if (e.KeyCode == Keys.Home) { offset = PointF.Empty; Invalidate(); e.Handled = true; }
            if (e.KeyCode == Keys.Down || e.KeyCode == Keys.Up)
            {
                int index = selected == null ? -1 : rows.IndexOf(selected);
                selected = rows[Math.Max(0,Math.Min(rows.Count-1,index+(e.KeyCode == Keys.Down ? 1 : -1)))];
                float sy = selected.Rect.Y*zoom+offset.Y;
                if (sy < 10 || sy > Height-40) offset.Y = 25-selected.Rect.Y*zoom;
                float sx = selected.Rect.X*zoom+offset.X;
                if (sx < 0 || sx > Width-100) offset.X = 25-selected.Rect.X*zoom;
                Invalidate(); e.Handled = true;
            }
            if (e.KeyCode == Keys.Enter && selected != null)
            {
                if (selected.More) ShowMore?.Invoke(selected.Node);
                else if (e.Control || !selected.Node.Entry.IsDirectory) OpenNode?.Invoke(selected.Node);
                else FocusNode?.Invoke(selected.Node);
                e.Handled = true;
            }
        }
        protected override void Dispose(bool disposing) { if (disposing) { tip.Dispose(); menu.Dispose(); } base.Dispose(disposing); }
    }
}



