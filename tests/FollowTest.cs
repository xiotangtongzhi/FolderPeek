using System;using System.Collections.Generic;using System.Diagnostics;using System.IO;using System.Reflection;using System.Runtime.InteropServices;using System.Threading;using System.Windows.Forms;using FolderPeek;
class FollowTest
{
 [DllImport("user32.dll")]static extern bool ShowWindow(IntPtr h,int command);
 [DllImport("user32.dll")]static extern bool PostMessage(IntPtr h,uint m,IntPtr w,IntPtr l);
 [DllImport("user32.dll")]static extern bool SetWindowPos(IntPtr h,IntPtr after,int x,int y,int cx,int cy,uint flags);
 static IntPtr OpenOwned(string path,HashSet<IntPtr> before,List<IntPtr> owned)
 {
  Process.Start("explorer.exe","/n,\""+path+"\"");var end=DateTime.UtcNow.AddSeconds(15);
  while(DateTime.UtcNow<end){Pump();object shell=null,windows=null;try{
   shell=Activator.CreateInstance(Type.GetTypeFromProgID("Shell.Application"));windows=((dynamic)shell).Windows();
   for(int i=0;i<(int)((dynamic)windows).Count;i++){object w=null;try{w=((dynamic)windows).Item(i);var h=new IntPtr((long)((dynamic)w).HWND);Uri uri;
    if(!before.Contains(h)&&!owned.Contains(h)&&Uri.TryCreate((string)((dynamic)w).LocationURL,UriKind.Absolute,out uri)&&uri.IsFile&&PathRules.SamePath(uri.LocalPath,path)){
     owned.Add(h);ShowWindow(h,9);SetWindowPos(h,IntPtr.Zero,0,0,0,0,3);return h;
    }
   }finally{Release(w);}}
  }finally{Release(windows);Release(shell);}}
  throw new Exception("Cannot find owned Explorer window");
 }
 static object Field(object x,string name){return x.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(x);}
 static void Pump(){Application.DoEvents();Thread.Sleep(30);}
 static void Wait(MainForm f,string path){var end=DateTime.UtcNow.AddSeconds(12);while(DateTime.UtcNow<end){Pump();var snapshot=(MapSnapshot)Field(Field(f,"map"),"snapshot");if(PathRules.SamePath((string)Field(f,"rootPath"),path)&&snapshot!=null&&PathRules.SamePath(snapshot.Root.Entry.FullPath,path))return;}throw new Exception("Follow timeout: expected="+path+" actual="+Field(f,"rootPath"));}
 static void Release(object x){if(x!=null&&Marshal.IsComObject(x))Marshal.ReleaseComObject(x);}
 static void Navigate(IntPtr handle,string path){object shell=null,windows=null;try{shell=Activator.CreateInstance(Type.GetTypeFromProgID("Shell.Application"));windows=((dynamic)shell).Windows();for(int i=0;i<(int)((dynamic)windows).Count;i++){object w=null;try{w=((dynamic)windows).Item(i);if(new IntPtr((long)((dynamic)w).HWND)==handle){((dynamic)w).Navigate2(path);return;}}finally{Release(w);}}throw new Exception("Owned test window disappeared");}finally{Release(windows);Release(shell);}}
 [STAThread]static int Main()
 {
  string root=Path.Combine(Path.GetTempPath(),"FolderPeek-Follow12-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
  var paths=new[]{Path.Combine(root,"中文 A"),Path.Combine(root,"文件夹 B"),Path.Combine(root,"目录 C")};foreach(var p in paths){Directory.CreateDirectory(p);File.WriteAllText(Path.Combine(p,"示例.txt"),"sample");}
  Directory.CreateDirectory(Path.Combine(paths[0],"子目录"));
  var before=new HashSet<IntPtr>();Native.EnumWindows((h,p)=>{before.Add(h);return true;},IntPtr.Zero);var owned=new List<IntPtr>();
  Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);int result=0;
  using(var form=new MainForm(Path.Combine(root,"settings.xml")))
  try
  {
   form.TopMost=true;form.Show();
   var first=OpenOwned(paths[0],before,owned);Wait(form,paths[0]);
   Console.WriteLine("PASS background follower updates live form while floating window is topmost");
   for(int n=0;n<6;n++){string path=paths[(n+1)%3];Navigate(first,path);Wait(form,path);Console.WriteLine("PASS same-window navigation "+(n+1)+": "+Path.GetFileName(path));}
   var canvas=(MapCanvas)Field(form,"map");var snap=(MapSnapshot)Field(canvas,"snapshot");canvas.FocusNode(snap.Root.Children.Find(x=>x.Entry.IsDirectory));Wait(form,Path.Combine(paths[0],"子目录"));
   if(!((CheckBox)Field(form,"follow")).Checked)throw new Exception("Manual focus silently disabled follow");
   Navigate(first,paths[1]);Wait(form,paths[1]);Console.WriteLine("PASS manual branch focus retains follow on next Explorer navigation");
   ShowWindow(first,6);var second=OpenOwned(paths[2],before,owned);Wait(form,paths[2]);
   Console.WriteLine("PASS second Explorer window follows");
   ShowWindow(second,6);ShowWindow(first,9);Wait(form,paths[1]);Console.WriteLine("PASS minimized top window falls back to earlier Explorer");
   ShowWindow(first,6);ShowWindow(second,9);Wait(form,paths[2]);Console.WriteLine("PASS restored second Explorer follows again");
   ((Button)Field(form,"collapseButton")).PerformClick();Navigate(second,paths[0]);Wait(form,paths[0]);
   if(!((Control)Field(form,"map")).Visible || ((Control)Field(form,"pathBox")).Visible || !PathRules.SamePath(((RichTextBox)Field(form,"pathBox")).Text,paths[0]))throw new Exception("Header-hidden follow failed: tree="+((Control)Field(form,"map")).Visible+" pathVisible="+((Control)Field(form,"pathBox")).Visible+" actual="+((RichTextBox)Field(form,"pathBox")).Text.Replace("\\r","[r]").Replace("\\n","[n]"));
   Console.WriteLine("PASS header-hidden mode follows Explorer and keeps path hidden");
   ((Button)Field(form,"collapseButton")).PerformClick();var latest=(MapSnapshot)Field(Field(form,"map"),"snapshot");if(!PathRules.SamePath(latest.Root.Entry.FullPath,paths[0]))throw new Exception("Expanded tree stale");
   Console.WriteLine("PASS restoring header shows latest followed directory");
  }
  catch(Exception e){Console.WriteLine(e);result=1;}
  finally{form.Close();foreach(var h in owned)PostMessage(h,0x10,IntPtr.Zero,IntPtr.Zero);var end=DateTime.UtcNow.AddMilliseconds(600);while(DateTime.UtcNow<end)Pump();Directory.Delete(root,true);}
  return result;
 }
}



