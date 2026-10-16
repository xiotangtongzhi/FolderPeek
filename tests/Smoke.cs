using System;using System.Collections;using System.IO;using System.Reflection;using System.Drawing;using System.Drawing.Imaging;using System.Runtime.InteropServices;using System.Threading;using System.Windows.Forms;using FolderPeek;
class Smoke
{
 [DllImport("user32.dll")]static extern int GetWindowLong(IntPtr hwnd,int index);
 static object Field(object x,string name){return x.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public).GetValue(x);}
 static void Invoke(object obj,string method,params object[] args){obj.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(obj,args);}
 static void Wait(MapCanvas map,Func<MapSnapshot,bool> predicate){DateTime end=DateTime.UtcNow.AddSeconds(15);while(DateTime.UtcNow<end){Application.DoEvents();Thread.Sleep(30);var s=(MapSnapshot)Field(map,"snapshot");if(s!=null&&predicate(s))return;}throw new Exception("UI read timed out");}
 static void Check(bool condition,string message){if(!condition)throw new Exception(message);Console.WriteLine("PASS "+message);}
 [STAThread]static int Main(string[] args){try{return Run(args);}catch(Exception e){Console.WriteLine("FAIL TYPE "+e.GetType().FullName+" MESSAGE "+e.Message);return 1;}}
 static int Run(string[] args)
 {
  string data=Path.Combine(Path.GetTempPath(),"FolderPeek-Smoke12-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(data);
  string demo=Path.Combine(data,"示例项目");Directory.CreateDirectory(Path.Combine(demo,"01 设计","品牌图标"));Directory.CreateDirectory(Path.Combine(demo,"02 文档"));Directory.CreateDirectory(Path.Combine(demo,"03 交付"));
  File.WriteAllText(Path.Combine(demo,"01 设计","品牌图标","02_层叠视窗.svg"),"sample");File.WriteAllText(Path.Combine(demo,"项目概览.txt"),"sample");
  for(int i=1;i<=60;i++)File.WriteAllText(Path.Combine(demo,"02 文档","资料 "+i.ToString("D2")+".md"),"sample");
  Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
  try{using(var f=new MainForm(Path.Combine(data,"settings.xml")))
  {
   ((CheckBox)Field(f,"follow")).Checked=false;f.TopMost=true;f.Show();Invoke(f,"SetRoot",demo);var map=(MapCanvas)Field(f,"map");Wait(map,s=>s.Root.Entry.Name=="示例项目");
   using(var bmp=new Bitmap(f.Width,f.Height)){f.Activate();f.Refresh();Application.DoEvents();Thread.Sleep(160);using(var capture=Graphics.FromImage(bmp))capture.CopyFromScreen(f.Location,Point.Empty,bmp.Size);bmp.Save(args[0],ImageFormat.Png);}
   Check(Math.Abs(f.Font.SizeInPoints-13.5f)<.01f && Math.Abs(map.Font.SizeInPoints-13.5f)<.01f,"text enlarged to 1.5x");
   Invoke(f,"ToggleMaximize");Application.DoEvents();Check(f.WindowState==FormWindowState.Maximized,"custom titlebar maximizes");Invoke(f,"ToggleMaximize");Application.DoEvents();Check(f.WindowState==FormWindowState.Normal && Math.Abs(f.Width-860*f.DeviceDpi/96f)<3,"custom titlebar restores size");
   var screen=f.PointToScreen(new Point(2,2));var hit=Message.Create(f.Handle,0x84,IntPtr.Zero,new IntPtr((screen.Y<<16)|(screen.X&65535)));object[] hitArgs={hit};typeof(MainForm).GetMethod("WndProc",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(f,hitArgs);Check(((Message)hitArgs[0]).Result.ToInt32()==13,"window corner remains resizable");
   Console.WriteLine("Fonts: "+MixedText.English.Name+" / "+MixedText.Chinese.Name);Check(MixedText.English.Name=="Times New Roman" && (MixedText.Chinese.Name=="SimSun" || MixedText.Chinese.Name=="宋体"),"requested font families are installed and selected");
   var pathControl=(RichTextBox)Field(f,"pathBox");string originalPath=pathControl.Text;pathControl.Text=@"C:\中文\Project 2026\说明.md";
   pathControl.Select(6,7);Check(pathControl.SelectionFont!=null && pathControl.SelectionFont.Name=="Times New Roman","English in editable mixed path uses Times New Roman");
   pathControl.Select(3,2);Check(pathControl.SelectionFont!=null && (pathControl.SelectionFont.Name=="SimSun" || pathControl.SelectionFont.Name=="宋体"),"Chinese in editable mixed path uses SimSun");
   pathControl.Text=originalPath;
   var toggle=(Button)Field(f,"themeButton");toggle.PerformClick();Application.DoEvents();
   Check(f.BackColor==Color.Black && map.BackColor==Color.Black && ((RichTextBox)Field(f,"pathBox")).BackColor==Color.Black,"night backgrounds are black");
   Check(((RichTextBox)Field(f,"pathBox")).ForeColor==Color.White,"night path text is white");
   Check(Theme.Level(0,true)!=Theme.Level(1,true) && Theme.Level(1,true)!=Theme.Level(2,true),"folder colors distinguish levels");
   var colors=new System.Collections.Generic.HashSet<Color>();for(int level=0;level<=12;level++)colors.Add(Theme.Level(level,true));Check(colors.Count==13,"all supported folder depths have distinct colors");
   var stack=new System.Collections.Generic.Stack<Control>();stack.Push(f);while(stack.Count>0){var c=stack.Pop();if(c.BackColor!=Color.Black || Math.Abs(c.Font.SizeInPoints-13.5f)>.01f)throw new Exception("Theme/font mismatch: "+c.GetType().Name);foreach(Control child in c.Controls)stack.Push(child);}Check(true,"all main-window controls share black backgrounds and 13.5pt font");
   var beforeSwitch=(MapSnapshot)Field(map,"snapshot");
   using(var bmp=new Bitmap(f.Width,f.Height)){f.Activate();f.Refresh();Application.DoEvents();Thread.Sleep(160);using(var capture=Graphics.FromImage(bmp))capture.CopyFromScreen(f.Location,Point.Empty,bmp.Size);bmp.Save(Path.Combine(Path.GetDirectoryName(args[0]),"夜间模式预览.png"),ImageFormat.Png);}
   toggle.PerformClick();Application.DoEvents();Check(Object.ReferenceEquals(beforeSwitch,Field(map,"snapshot")),"theme switching preserves directory data");
   Check(Math.Abs(f.Width-860*f.DeviceDpi/96f)<3,"default width remains 860");
   Check((GetWindowLong(map.Handle,-16)&0x300000)==0,"no horizontal or vertical scrollbar styles");
   var snapshot=(MapSnapshot)Field(map,"snapshot");var docs=snapshot.Root.Children.Find(n=>n.Entry.Name=="02 文档");Check(docs.TotalFiles==60&&docs.Children.Count==5,"GUI starts with five of sixty files");
   // Click the rendered More row, rather than invoking the model directly.
   object marker=null;foreach(var r in (IList)Field(map,"rows"))if((bool)Field(r,"More")&&((MapNode)Field(r,"Node")).Entry.Name=="02 文档")marker=r;
   var rect=(RectangleF)Field(marker,"Rect");Invoke(map,"OnMouseDown",new MouseEventArgs(MouseButtons.Left,1,(int)rect.X+12,(int)rect.Y+12,0));
   Wait(map,s=>s.Root.Children.Find(n=>n.Entry.Name=="02 文档").Children.Count==10);Check(true,"click show-more adds exactly next five");
   Invoke(map,"OnMouseDown",new MouseEventArgs(MouseButtons.Middle,1,220,150,0));Invoke(map,"OnMouseMove",new MouseEventArgs(MouseButtons.Middle,0,290,195,0));Invoke(map,"OnMouseUp",new MouseEventArgs(MouseButtons.Middle,1,290,195,0));
   var pan=(PointF)Field(map,"offset");Check(pan.X==70&&pan.Y==45&&!map.Capture,"middle-button drag moves canvas and releases capture");
   snapshot=(MapSnapshot)Field(map,"snapshot");map.ShowMore(snapshot.Root.Children.Find(n=>n.Entry.Name=="02 文档"));Wait(map,s=>s.Root.Children.Find(n=>n.Entry.Name=="02 文档").Children.Count==15);
   Check((PointF)Field(map,"offset")==pan,"loading more preserves canvas position");
   var fold=(Button)Field(f,"collapseButton");int expandedHeight=f.Height;var treeBeforeFold=Field(map,"snapshot");fold.PerformClick();Application.DoEvents();
   Check(map.Visible && !((Control)Field(f,"toolbar")).Visible && !pathControl.Visible && !((Control)Field(f,"titlePanel")).Visible && fold.Visible,"collapse hides title toolbar and path while tree remains visible");
   Check(f.Height==expandedHeight && fold.PointToScreen(Point.Empty).Y-f.Top<60,"header-hidden mode retains window size with triangle at top");
   using(var bmp=new Bitmap(f.Width,f.Height)){f.Activate();f.Refresh();Application.DoEvents();Thread.Sleep(160);using(var capture=Graphics.FromImage(bmp))capture.CopyFromScreen(f.Location,Point.Empty,bmp.Size);bmp.Save(Path.Combine(Path.GetDirectoryName(args[0]),"收起状态预览.png"),ImageFormat.Png);}
   fold.PerformClick();Application.DoEvents();Check(f.Height==expandedHeight && map.Visible && ((Control)Field(f,"toolbar")).Visible,"expand restores previous height and controls");
   Check(Object.ReferenceEquals(treeBeforeFold,Field(map,"snapshot")) && (PointF)Field(map,"offset")==pan,"folding preserves loaded tree and panned position");
   for(int n=0;n<4;n++){fold.PerformClick();fold.PerformClick();}Check(f.Height==expandedHeight,"repeated folding does not accumulate size drift");
   Invoke(f,"ToggleMaximize");fold.PerformClick();Check(f.WindowState==FormWindowState.Maximized,"hiding header keeps maximized window");fold.PerformClick();Check(f.WindowState==FormWindowState.Maximized,"restoring header keeps maximized window");Invoke(f,"ToggleMaximize");
   map.SetZoom(.1f);Check(map.Zoom==.5f,"minimum zoom bound");map.SetZoom(4);Check(map.Zoom==1.75f,"maximum zoom bound");map.SetZoom(1);
   Invoke(f,"SetRoot",demo);Invoke(f,"SetRoot",Path.Combine(demo,"03 交付"));Wait(map,s=>s.Root.Entry.Name=="03 交付");Check(true,"rapid root changes discard stale result");
   map.PageSizeChanged(10);Wait(map,s=>s.Root.Entry.Name=="03 交付");toggle.PerformClick();fold.PerformClick();f.Close();Application.DoEvents();Check(SettingsStore.Load(Path.Combine(data,"settings.xml")).PageSize==10,"context-menu page-size preference persists");
   Check(SettingsStore.Load(Path.Combine(data,"settings.xml")).DarkMode,"night preference persists");
   var saved=SettingsStore.Load(Path.Combine(data,"settings.xml"));Check(saved.Collapsed && saved.Height==(int)Math.Round(expandedHeight*96f/f.DeviceDpi),"save hidden-header state and full height");
   using(var reopened=new MainForm(Path.Combine(data,"settings.xml"))){reopened.Show();Application.DoEvents();Check((bool)Field(reopened,"collapsed") && ((Control)Field(reopened,"map")).Visible && !((Control)Field(reopened,"pathBox")).Visible,"relaunch restores hidden header with tree visible");((Button)Field(reopened,"collapseButton")).PerformClick();Check(reopened.Height==expandedHeight,"relaunch expand restores full height");reopened.Close();}
  }}finally{Directory.Delete(data,true);}return 0;
 }
}








