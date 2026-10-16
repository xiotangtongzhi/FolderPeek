using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Collections.Generic;
class IconMaker
{
 static Graphics g; static StringBuilder svg; static string color;
 static void Rect(float x,float y,float w,float h,float r,string c) { using(var p=new GraphicsPath()) { if(r==0) p.AddRectangle(new RectangleF(x,y,w,h)); else { float d=r*2; p.AddArc(x,y,d,d,180,90);p.AddArc(x+w-d,y,d,d,270,90);p.AddArc(x+w-d,y+h-d,d,d,0,90);p.AddArc(x,y+h-d,d,d,90,90);p.CloseFigure(); } using(var b=new SolidBrush(ColorTranslator.FromHtml(c))) g.FillPath(b,p); } svg.AppendFormat(System.Globalization.CultureInfo.InvariantCulture,"<rect x='{0}' y='{1}' width='{2}' height='{3}' rx='{4}' fill='{5}'/>",x,y,w,h,r,c); }
 static void Line(float x,float y,float xx,float yy,float width,string c) { using(var p=new Pen(ColorTranslator.FromHtml(c),width)){p.StartCap=p.EndCap=LineCap.Round;g.DrawLine(p,x,y,xx,yy);} svg.AppendFormat(System.Globalization.CultureInfo.InvariantCulture,"<path d='M {0} {1} L {2} {3}' stroke='{4}' stroke-width='{5}' stroke-linecap='round'/>",x,y,xx,yy,c,width); }
 static void Circle(float x,float y,float r,string c){using(var b=new SolidBrush(ColorTranslator.FromHtml(c)))g.FillEllipse(b,x-r,y-r,r*2,r*2);svg.AppendFormat(System.Globalization.CultureInfo.InvariantCulture,"<circle cx='{0}' cy='{1}' r='{2}' fill='{3}'/>",x,y,r,c);}
 static void Folder(float x,float y,float w,float h,string c){Rect(x,y,w*.43f,15,5,c);Rect(x,y+10,w,h-10,7,c);}
 static void Ico(Bitmap src,string path){var images=new List<byte[]>();int[] sizes={16,24,32,48,64,128,256}; foreach(int s in sizes)using(var b=new Bitmap(s,s)) {using(var q=Graphics.FromImage(b)){q.InterpolationMode=InterpolationMode.HighQualityBicubic;q.DrawImage(src,0,0,s,s);}using(var m=new MemoryStream()){b.Save(m,ImageFormat.Png);images.Add(m.ToArray());}}using(var w=new BinaryWriter(File.Create(path))){w.Write((ushort)0);w.Write((ushort)1);w.Write((ushort)sizes.Length);int offset=6+16*sizes.Length;for(int i=0;i<sizes.Length;i++){w.Write((byte)(sizes[i]==256?0:sizes[i]));w.Write((byte)(sizes[i]==256?0:sizes[i]));w.Write((byte)0);w.Write((byte)0);w.Write((ushort)1);w.Write((ushort)32);w.Write(images[i].Length);w.Write(offset);offset+=images[i].Length;}foreach(var data in images)w.Write(data);}}
 static void Main(string[] args){string dir=args[0];Directory.CreateDirectory(dir);string[] names={"林间分支","层叠视窗","目录之眼","路径星图","薄荷档案","琥珀导航","紫色透镜","海蓝节点","石墨门户","珊瑚方格"};string[] colors={"#244E48","#334E70","#363F54","#253B57","#297D72","#8E663B","#65527E","#286780","#343B45","#A85F57"};using(var sheet=new Bitmap(1400,670))using(var sg=Graphics.FromImage(sheet)){sg.Clear(Color.FromArgb(247,249,251));using(var f=new Font("Microsoft YaHei UI",22,FontStyle.Bold))using(var br=new SolidBrush(Color.FromArgb(35,52,69)))sg.DrawString("FOLDER PEEK  /  图标候选",f,br,38,22);for(int i=0;i<10;i++){color=colors[i];using(var b=new Bitmap(256,256)){g=Graphics.FromImage(b);g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(Color.Transparent);svg=new StringBuilder("<svg xmlns='http://www.w3.org/2000/svg' width='256' height='256' viewBox='0 0 256 256'>");Rect(8,8,240,240,52,color);string white="#F4FAFA",light="#9EDBCB";
 switch(i){case 0:Folder(93,49,70,49,white);Line(128,103,128,143,9,light);Line(65,143,191,143,9,light);Line(65,143,65,166,9,light);Line(191,143,191,166,9,light);Folder(41,168,48,36,white);Folder(104,168,48,36,white);Folder(167,168,48,36,white);break;
 case 1:Rect(51,50,135,103,13,"#789AB7");Rect(65,72,140,110,13,"#B9D1E1");Rect(80,94,137,109,13,white);Line(101,122,177,122,7,color);Line(101,143,162,143,7,color);Line(101,165,183,165,7,color);break;
 case 2:Folder(48,61,160,132,"#AABED1");Circle(128,139,45,white);Circle(128,139,25,color);Circle(136,131,8,light);break;
 case 3:Line(128,71,66,169,8,white);Line(128,71,192,169,8,white);Line(66,169,192,169,8,white);Circle(128,67,24,light);Circle(65,171,24,white);Circle(193,171,24,"#D5B882");break;
 case 4:Folder(47,65,163,133,white);Line(128,113,128,171,8,color);Line(90,148,166,148,8,color);Circle(90,148,12,light);Circle(166,148,12,light);Circle(128,113,12,color);break;
 case 5:Circle(128,128,80,"#E9D1AA");Circle(128,128,65,color);Line(128,79,128,177,6,white);Line(79,128,177,128,6,white);Line(109,147,151,105,14,"#F4D08B");Circle(128,128,10,white);break;
 case 6:Rect(51,61,123,123,24,white);Rect(70,81,85,84,13,"#BBA9D5");Line(169,177,207,215,16,white);Line(91,108,134,108,8,color);Line(91,132,120,132,8,color);break;
 case 7:Line(71,128,128,70,10,"#91CDDF");Line(71,128,184,128,10,"#91CDDF");Line(71,128,128,187,10,"#91CDDF");Rect(44,103,51,51,13,white);Rect(111,47,45,45,12,white);Rect(164,106,45,45,12,white);Rect(111,164,45,45,12,white);break;
 case 8:Rect(60,45,137,171,22,white);Rect(80,68,97,148,10,color);Line(107,123,154,123,9,"#B4D5C9");Line(137,105,155,123,9,"#B4D5C9");Line(137,141,155,123,9,"#B4D5C9");break;
 case 9:Folder(45,57,166,144,white);Rect(70,105,48,28,6,color);Rect(134,105,48,28,6,"#D6978D");Rect(70,148,48,28,6,"#D6978D");Rect(134,148,48,28,6,color);break;}
 svg.Append("</svg>");string stem=(i+1).ToString("D2")+"_"+names[i];b.Save(Path.Combine(dir,stem+".png"),ImageFormat.Png);File.WriteAllText(Path.Combine(dir,stem+".svg"),svg.ToString(),Encoding.UTF8);Ico(b,Path.Combine(dir,stem+".ico"));int xx=40+(i%5)*275, yy=100+(i/5)*280;sg.InterpolationMode=InterpolationMode.HighQualityBicubic;sg.DrawImage(b,xx+36,yy,180,180);using(var f=new Font("Microsoft YaHei UI",13))using(var br=new SolidBrush(Color.FromArgb(56,71,85)))sg.DrawString((i+1).ToString("D2")+"  "+names[i],f,br,xx+30,yy+195);g.Dispose();}}
 sheet.Save(Path.Combine(dir,"00_全部图标预览.png"),ImageFormat.Png);}}
}
