using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
namespace FolderPeek
{
    internal static class Theme
    {
        public static Color Background(bool dark) { return dark ? Color.Black : Color.White; }
        public static Color Text(bool dark) { return dark ? Color.White : Color.FromArgb(40,55,69); }
        public static Color Border(bool dark) { return dark ? Color.FromArgb(115,125,137) : Color.FromArgb(190,204,216); }
        private static readonly Color[] light = { Color.FromArgb(34,73,114),Color.FromArgb(0,112,100),Color.FromArgb(121,69,152),Color.FromArgb(147,91,0),Color.FromArgb(165,60,80),Color.FromArgb(0,104,145),Color.FromArgb(112,105,18),Color.FromArgb(161,72,31),Color.FromArgb(75,82,160),Color.FromArgb(138,58,111),Color.FromArgb(58,113,49),Color.FromArgb(113,76,51),Color.FromArgb(43,102,116) };
        private static readonly Color[] night = { Color.FromArgb(139,194,255),Color.FromArgb(111,224,195),Color.FromArgb(208,167,255),Color.FromArgb(255,210,122),Color.FromArgb(255,158,184),Color.FromArgb(111,217,255),Color.FromArgb(212,224,122),Color.FromArgb(255,173,125),Color.FromArgb(167,180,255),Color.FromArgb(246,165,226),Color.FromArgb(163,230,143),Color.FromArgb(225,188,155),Color.FromArgb(146,214,220) };
        public static Color Level(int depth,bool dark) { var colors = dark ? night : light; return colors[Math.Max(0,depth)%colors.Length]; }
        public static void Apply(Control c,bool dark)
        {
            c.BackColor = Background(dark); c.ForeColor = Text(dark);
            var button = c as Button; if (button != null) { button.UseVisualStyleBackColor = false; button.FlatAppearance.BorderColor = Border(dark); button.FlatAppearance.MouseOverBackColor = Background(dark); button.FlatAppearance.MouseDownBackColor = Background(dark); }
            foreach(Control child in c.Controls) Apply(child,dark);
            c.Invalidate();
        }
        public static void Menu(ToolStrip menu,bool dark,Font font)
        {
            menu.Font = font; menu.BackColor = Background(dark); menu.ForeColor = Text(dark); menu.Renderer = new ThemeMenuRenderer(dark);
            var popup = menu as ToolStripDropDownMenu; if (popup != null) { popup.ShowImageMargin = false; popup.ShowCheckMargin = false; }
            foreach(ToolStripItem item in menu.Items) { item.ForeColor = Text(dark); item.BackColor = Background(dark); var drop = item as ToolStripDropDownItem; if (drop != null) Menu(drop.DropDown,dark,font); }
        }
        public static void Tooltip(ToolTip tip,Func<bool> dark,Func<Font> font)
        {
            tip.OwnerDraw = true;
            tip.Popup += (s,e) => { var size = MixedText.Measure(tip.GetToolTip(e.AssociatedControl)); e.ToolTipSize = new Size(size.Width+18,size.Height+12); };
            tip.Draw += (s,e) => { using(var b = new SolidBrush(Background(dark()))) e.Graphics.FillRectangle(b,e.Bounds); using(var p = new Pen(Border(dark()))) e.Graphics.DrawRectangle(p,0,0,e.Bounds.Width-1,e.Bounds.Height-1); MixedText.Draw(e.Graphics,e.ToolTipText,new Rectangle(8,5,e.Bounds.Width-16,e.Bounds.Height-10),Text(dark())); };
        }
        [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(IntPtr hwnd,int attr,ref int value,int size);
        public static void Caption(IntPtr hwnd,bool dark)
        {
            int enabled=dark?1:0, background=dark?0:0xFFFFFF, foreground=dark?0xFFFFFF:0x453728;
            DwmSetWindowAttribute(hwnd,20,ref enabled,4); DwmSetWindowAttribute(hwnd,35,ref background,4); DwmSetWindowAttribute(hwnd,36,ref foreground,4);
        }
    }
    internal sealed class ThemeMenuRenderer : ToolStripProfessionalRenderer
    {
        private readonly bool dark;
        public ThemeMenuRenderer(bool dark) { this.dark=dark; RoundedEdges=false; }
        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e) { e.Graphics.Clear(Theme.Background(dark)); }
        protected override void OnRenderImageMargin(ToolStripRenderEventArgs e) { }
        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e) { using(var b=new SolidBrush(Theme.Background(dark)))e.Graphics.FillRectangle(b,new Rectangle(Point.Empty,e.Item.Size)); if(e.Item.Selected)using(var p=new Pen(Theme.Border(dark)))e.Graphics.DrawRectangle(p,0,0,e.Item.Width-1,e.Item.Height-1); }
        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e) { MixedText.Draw(e.Graphics,e.Text,e.TextRectangle,Theme.Text(dark)); }
        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e) { e.ArrowColor=Theme.Text(dark); base.OnRenderArrow(e); }
        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e) { using(var p=new Pen(Theme.Border(dark)))e.Graphics.DrawRectangle(p,0,0,e.ToolStrip.Width-1,e.ToolStrip.Height-1); }
        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e) { using(var p=new Pen(Theme.Border(dark)))e.Graphics.DrawLine(p,4,e.Item.Height/2,e.Item.Width-4,e.Item.Height/2); }
    }
    internal sealed class SquareButton : Button
    {
        public bool ThemeSymbol, Dark, CollapseSymbol, Collapsed;
        public override Size GetPreferredSize(Size proposedSize) { var s=MixedText.Measure(Text);return new Size(s.Width+Padding.Horizontal+18,Math.Max(40,s.Height+Padding.Vertical+8)); }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);
            using(var p=new Pen(Focused?ForeColor:FlatAppearance.BorderColor))e.Graphics.DrawRectangle(p,0,0,Width-1,Height-1);
            if(CollapseSymbol) { float x=Width/2f,y=Height/2f;using(var b=new SolidBrush(ForeColor))e.Graphics.FillPolygon(b,Collapsed?new[]{new PointF(x-6,y-3),new PointF(x+6,y-3),new PointF(x,y+4)}:new[]{new PointF(x-6,y+3),new PointF(x+6,y+3),new PointF(x,y-4)});return; }
            if(!ThemeSymbol) { MixedText.Draw(e.Graphics,Text,ClientRectangle,ForeColor,true);return; }
            e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
            float cx=Width/2f,cy=Height/2f;
            if(Dark) { using(var b=new SolidBrush(ForeColor))e.Graphics.FillEllipse(b,cx-10,cy-11,22,22);using(var b=new SolidBrush(BackColor))e.Graphics.FillEllipse(b,cx-3,cy-15,20,22); }
            else using(var p=new Pen(ForeColor,1.8f)) { e.Graphics.DrawEllipse(p,cx-6,cy-6,12,12);for(int i=0;i<8;i++){double a=i*Math.PI/4;e.Graphics.DrawLine(p,cx+(float)Math.Cos(a)*9,cy+(float)Math.Sin(a)*9,cx+(float)Math.Cos(a)*13,cy+(float)Math.Sin(a)*13);} }
        }
    }
    internal sealed class SquareCheckBox : CheckBox
    {
        public SquareCheckBox() { FlatStyle=FlatStyle.Flat; }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);int size=Math.Max(16,Font.Height-5),y=(Height-size)/2;
            using(var p=new Pen(ForeColor,1))e.Graphics.DrawRectangle(p,0,y,size,size);
            if(Checked)using(var p=new Pen(ForeColor,2)) { e.Graphics.DrawLines(p,new[]{new Point(3,y+size/2),new Point(size/2-1,y+size-4),new Point(size-3,y+4)}); }
            MixedText.Draw(e.Graphics,Text,new Rectangle(size+7,0,Width-size-7,Height),ForeColor);
            if(Focused)ControlPaint.DrawFocusRectangle(e.Graphics,new Rectangle(size+5,0,Width-size-6,Height-1),ForeColor,BackColor);
        }
        public override Size GetPreferredSize(Size proposedSize) { var text=MixedText.Measure(Text);return new Size(text.Width+Math.Max(16,Font.Height-5)+9,Math.Max(text.Height,Font.Height)+8); }
    }
}



