using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows.Forms;
namespace FolderPeek
{
    internal static class MixedText
    {
        internal static readonly Font English = new Font("Times New Roman",13.5f);
        internal static readonly Font Chinese = new Font("SimSun",13.5f);
        internal sealed class Run { public int Start,Length; public bool Cjk; }
        internal static bool IsChinese(int c) { return (c>=0x2E80&&c<=0x9FFF)||(c>=0xF900&&c<=0xFAFF)||(c>=0xFE30&&c<=0xFE4F)||(c>=0xFF01&&c<=0xFF60)||(c>=0x20000&&c<=0x323AF); }
        internal static IEnumerable<Run> Runs(string text)
        {
            int start=0;bool previous=false;
            for(int i=0;i<text.Length;)
            {
                int length=char.IsHighSurrogate(text[i])&&i+1<text.Length&&char.IsLowSurrogate(text[i+1])?2:1;
                int scalar=length==2?char.ConvertToUtf32(text[i],text[i+1]):text[i];bool chinese=IsChinese(scalar);
                if(i>start&&chinese!=previous){yield return new Run{Start=start,Length=i-start,Cjk=previous};start=i;}
                previous=chinese;i+=length;
            }
            if(start<text.Length)yield return new Run{Start=start,Length=text.Length-start,Cjk=previous};
        }
        private static StringFormat Format() { var f=(StringFormat)StringFormat.GenericTypographic.Clone();f.FormatFlags|=StringFormatFlags.MeasureTrailingSpaces|StringFormatFlags.NoWrap;return f; }
        private static float Width(Graphics g,string text)
        {
            float total=0;using(var f=Format())foreach(var r in Runs(text))total+=g.MeasureString(text.Substring(r.Start,r.Length),r.Cjk?Chinese:English,int.MaxValue,f).Width;return total;
        }
        internal static Size Measure(string text)
        {
            using(var g=Graphics.FromHwnd(IntPtr.Zero)){float max=0;var lines=(text??"").Split('\n');foreach(var line in lines)max=Math.Max(max,Width(g,line.TrimEnd('\r')));return new Size((int)Math.Ceiling(max)+6,(int)Math.Ceiling(Math.Max(English.GetHeight(g),Chinese.GetHeight(g)))*lines.Length+4);}
        }
        internal static void Draw(Graphics g,string text,RectangleF bounds,Color color,bool center=false)
        {
            text=text??"";var state=g.Save();g.SetClip(bounds,CombineMode.Intersect);
            float height=Math.Max(English.GetHeight(g),Chinese.GetHeight(g));var lines=text.Split('\n');float y=bounds.Y+(lines.Length==1?Math.Max(0,(bounds.Height-height)/2):0);
            using(var f=Format())using(var brush=new SolidBrush(color))foreach(var source in lines)
            {
                string line=source.TrimEnd('\r');float width=Width(g,line);
                if(width>bounds.Width){int[] starts=StringInfo.ParseCombiningCharacters(line);int lo=0,hi=starts.Length;while(lo<hi){int mid=(lo+hi+1)/2;string prefix=mid==starts.Length?line:line.Substring(0,starts[mid]);if(Width(g,prefix+"…")<=bounds.Width)lo=mid;else hi=mid-1;}line=(lo==starts.Length?line:line.Substring(0,starts[lo]))+"…";width=Width(g,line);}
                float x=bounds.X+(center?Math.Max(0,(bounds.Width-width)/2):0);
                foreach(var r in Runs(line)){var font=r.Cjk?Chinese:English;string part=line.Substring(r.Start,r.Length);g.DrawString(part,font,brush,new PointF(x,y),f);x+=g.MeasureString(part,font,int.MaxValue,f).Width;}
                y+=height;
            }
            g.Restore(state);
        }
    }
    internal sealed class MixedPathBox : RichTextBox
    {
        private bool formatting;
        [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hwnd,int msg,IntPtr wparam,IntPtr lparam);
        [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hwnd,int msg,IntPtr wparam,ref Point point);
        public MixedPathBox() { Multiline=false;WordWrap=false;ScrollBars=RichTextBoxScrollBars.None;DetectUrls=false;Font=MixedText.English;BorderStyle=BorderStyle.FixedSingle; }
        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);if(formatting||IsDisposed)return;formatting=true;
            int start=SelectionStart,length=SelectionLength;bool modified=Modified;var scroll=Point.Empty;bool suspendRedraw=IsHandleCreated&&Visible;
            if(IsHandleCreated){SendMessage(Handle,0x4DD,IntPtr.Zero,ref scroll);if(suspendRedraw)SendMessage(Handle,0xB,IntPtr.Zero,IntPtr.Zero);}
            try{SelectAll();SelectionFont=MixedText.English;foreach(var run in MixedText.Runs(Text))if(run.Cjk){Select(run.Start,run.Length);SelectionFont=MixedText.Chinese;}Select(Math.Min(start,TextLength),Math.Min(length,TextLength-Math.Min(start,TextLength)));Modified=modified;}
            finally{if(IsHandleCreated){SendMessage(Handle,0x4DE,IntPtr.Zero,ref scroll);if(suspendRedraw)SendMessage(Handle,0xB,new IntPtr(1),IntPtr.Zero);Invalidate();}formatting=false;}
        }
        protected override bool IsInputKey(Keys keyData){if((keyData&Keys.KeyCode)==Keys.Enter)return true;return base.IsInputKey(keyData);}
    }
}

