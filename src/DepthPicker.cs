using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
namespace FolderPeek
{
    internal sealed class DepthPicker : Control
    {
        public readonly List<object> Items = new List<object>();
        private int index = -1;
        private ContextMenuStrip popup;
        public event EventHandler SelectedIndexChanged;
        public int SelectedIndex { get { return index; } set { if(index==value)return;index=value;Invalidate();SelectedIndexChanged?.Invoke(this,EventArgs.Empty); } }
        public DepthPicker() { SetStyle(ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.Selectable,true);Height=40;TabStop=true;Cursor=Cursors.Hand;AccessibleName=L10n.T("展开深度", "Depth"); }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);using(var p=new Pen(ForeColor)){e.Graphics.DrawRectangle(p,0,0,Width-1,Height-1);e.Graphics.DrawLines(p,new[]{new Point(Width-21,Height/2-3),new Point(Width-15,Height/2+3),new Point(Width-9,Height/2-3)});}
            if(index>=0&&index<Items.Count)MixedText.Draw(e.Graphics,Items[index].ToString(),new Rectangle(5,0,Width-32,Height),ForeColor);
        }
        private void Open()
        {
            if(popup!=null)popup.Dispose();popup=new ContextMenuStrip();
            for(int i=0;i<Items.Count;i++){int item=i;popup.Items.Add(Items[i].ToString(),null,(s,e)=>SelectedIndex=item);}
            Theme.Menu(popup,BackColor==Color.Black,Font);popup.Show(this,new Point(0,Height));
        }
        protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);Focus();if(e.Button==MouseButtons.Left)Open();}
        protected override void OnKeyDown(KeyEventArgs e){base.OnKeyDown(e);if(e.KeyCode==Keys.Space||e.KeyCode==Keys.Enter||e.KeyCode==Keys.Down){Open();e.Handled=true;}}
        protected override void Dispose(bool disposing){if(disposing&&popup!=null)popup.Dispose();base.Dispose(disposing);}
    }
}

