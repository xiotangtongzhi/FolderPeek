using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using FolderPeek;

internal static class Program
{
    private static int passed;
    private static string root;
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static void Test(string name, Action action) { action(); passed++; Console.WriteLine("PASS " + name); }
    private static List<Entry> All(DirectoryPager pager, int pageSize)
    {
        var entries = new List<Entry>();
        DirectoryPage page;
        int pages = 0;
        do { page = pager.ReadPage(pageSize, CancellationToken.None); Check(page.Error == null, page.Error); entries.AddRange(page.Entries); Check(++pages < 100, "Unexpected endless paging"); } while (page.HasMore);
        return entries;
    }
    public static int Main()
    {
        root = Path.Combine(Path.GetTempPath(), "folder-peek-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            Test("empty directory", () => { using var p = new DirectoryPager(root, false); var x = p.ReadPage(200, CancellationToken.None); Check(x.Entries.Count == 0 && !x.HasMore && x.Error == null, "Expected empty"); });
            string large = Path.Combine(root, "large"); Directory.CreateDirectory(large);
            for (int i = 0; i < 451; i++) Directory.CreateDirectory(Path.Combine(large, "目录 " + i.ToString("D4")));
            for (int i = 0; i < 9; i++) File.WriteAllText(Path.Combine(large, "file " + i + ".cmd"), "not executed");
            Test("451 directories split 200 + 200 + 51; no duplicates or drops", () =>
            {
                using var p = new DirectoryPager(large, false);
                var a = p.ReadPage(200, CancellationToken.None); var b = p.ReadPage(200, CancellationToken.None); var c = p.ReadPage(200, CancellationToken.None);
                Check(a.Entries.Count == 200 && a.HasMore && b.Entries.Count == 200 && b.HasMore && c.Entries.Count == 51 && !c.HasMore, "Page sizes");
                var all = a.Entries.Concat(b.Entries).Concat(c.Entries).ToArray();
                Check(all.Select(x => x.FullPath).Distinct().Count() == 451 && all.All(x => x.IsDirectory), "Missing/duplicate/file");
                Check(p.ReadPage(200, CancellationToken.None).Entries.Count == 0, "Read beyond end");
            });
            Test("file-mode enumeration includes files, with folders first", () =>
            { using var p = new DirectoryPager(large, true); var all = All(p, 200); Check(all.Count == 460 && all.Take(451).All(x => x.IsDirectory) && all.Skip(451).All(x => !x.IsDirectory), "File mode ordering"); });
            Test("exact page boundary", () =>
            { using var p = new DirectoryPager(large, false); var page = p.ReadPage(451, CancellationToken.None); Check(page.Entries.Count == 451 && !page.HasMore, "False extra page"); });
            Test("one-item pages keep every entry", () =>
            { string d = Path.Combine(root, "ones"); Directory.CreateDirectory(d); for (int i = 0; i < 7; i++) Directory.CreateDirectory(Path.Combine(d, i.ToString())); using var p = new DirectoryPager(d, false); Check(All(p, 1).Select(x => x.FullPath).Distinct().Count() == 7, "Page drop"); });
            Test("no recursive preloading", () =>
            { string d = Path.Combine(root, "nested"); Directory.CreateDirectory(Path.Combine(d, "child", "grandchild")); using var p = new DirectoryPager(d, false); var all = All(p, 200); Check(all.Count == 1 && all[0].Name == "child", "Recursed"); });
            Test("deleted/nonexistent directory gives readable error", () =>
            { string d = Path.Combine(root, "deleted"); Directory.CreateDirectory(d); using var p = new DirectoryPager(d, true); Directory.Delete(d); var page = p.ReadPage(200, CancellationToken.None); Check(page.Error != null && !page.HasMore, "Missing error"); });
            Test("file supplied as root gives error", () =>
            { using var p = new DirectoryPager(Path.Combine(large, "file 0.cmd"), false); Check(p.ReadPage(200, CancellationToken.None).Error != null, "File was accepted as directory"); });
            Test("cancellation before read", () =>
            { using var p = new DirectoryPager(large, false); using var c = new CancellationTokenSource(); c.Cancel(); bool thrown = false; try { p.ReadPage(200, c.Token); } catch (OperationCanceledException) { thrown = true; } Check(thrown, "Cancellation ignored"); });
            Test("cancellation between pages", () =>
            { using var p = new DirectoryPager(large, false); p.ReadPage(200, CancellationToken.None); using var c = new CancellationTokenSource(); c.Cancel(); bool thrown = false; try { p.ReadPage(200, c.Token); } catch (OperationCanceledException) { thrown = true; } Check(thrown, "Cancellation ignored"); });
            Test("stale generation results are rejected", () =>
            { var gate = new GenerationGate(); int old = gate.Advance(); int next = gate.Advance(); Check(!gate.IsCurrent(old) && gate.IsCurrent(next), "Stale accepted"); });
            Test("page-size validation", () =>
            { using var p = new DirectoryPager(root, false); bool thrown = false; try { p.ReadPage(0, CancellationToken.None); } catch (ArgumentOutOfRangeException) { thrown = true; } Check(thrown, "Invalid size"); });
            if (!OperatingSystem.IsWindows())
            {
                Test("inaccessible directory produces error without crashing", () =>
                {
                    if (OperatingSystem.IsWindows()) return;
                    string d = Path.Combine(root, "denied"); Directory.CreateDirectory(d);
                    var old = File.GetUnixFileMode(d); File.SetUnixFileMode(d, UnixFileMode.None);
                    try { using var p = new DirectoryPager(d, false); Check(p.ReadPage(200, CancellationToken.None).Error != null, "Permission test requires non-root execution"); }
                    finally { File.SetUnixFileMode(d, old); }
                });
                Test("directory symlink cycle is listed, never automatically traversed", () =>
                { string d = Path.Combine(root, "links"); Directory.CreateDirectory(d); Directory.CreateSymbolicLink(Path.Combine(d, "back to root"), d); using var p = new DirectoryPager(d, false); var all = All(p, 200); Check(all.Count == 1 && all[0].IsDirectory && all[0].IsReparsePoint, "Link metadata/recursion"); });
            }
            Test("filesystem-path detection accepts Unicode, punctuation and UNC", () =>
            { foreach (string p in new[] { @"C:\", @"D:\中文\a & b,100%#", @"\\server\share\目录", "C:/forward/path" }) Check(PathRules.IsWindowsFileSystemPath(p), p); });
            Test("filesystem-path detection rejects URLs, shell names and device paths", () =>
            { foreach (string p in new[] { "https://example.com", "shell:Home", "::{GUID}", @"\\?\C:\Work", @"\\.\pipe\x", @"C:relative", "relative", @"\\server", "" }) Check(!PathRules.IsWindowsFileSystemPath(p), p); });
            Test("path equivalence ignores case and trailing slash", () =>
            { Check(PathRules.SamePath(@"C:\Work\", @"c:\work"), "Expected equality"); Check(!PathRules.SamePath(@"C:\Work", @"C:\Workspace"), "False equality"); });
            Test("navigation policy rejects a directory replaced by a file", () =>
            { Check(NavigationPolicy.Decide(true, false) == NavigationAction.RejectChangedDirectory, "Changed directory accepted"); Check(NavigationPolicy.Decide(true, true) == NavigationAction.ExploreDirectory, "Folder action"); });
            Test("file navigation policy selects instead of executes", () =>
            { Check(NavigationPolicy.Decide(false, false) == NavigationAction.SelectFile, "File action"); Check(NavigationPolicy.Decide(false, true) == NavigationAction.ExploreDirectory, "Changed file action"); });
            Test("settings missing and corrupted recover with safe defaults", () =>
            { string f = Path.Combine(root, "settings.xml"); Check(SettingsStore.Load(f).FollowExplorer, "Default follow disabled"); File.WriteAllText(f, "not xml"); Check(SettingsStore.Load(f).ShowFiles, "Bad XML recovery"); File.WriteAllText(f, "<!DOCTYPE x [<!ENTITY y SYSTEM 'file:///etc/passwd'>]><AppSettings>&y;</AppSettings>"); Check(SettingsStore.Load(f).FollowExplorer, "DTD allowed"); });
            Test("settings roundtrip and atomic replacement", () =>
            { string f = Path.Combine(root, "settings.xml"); var x = new AppSettings { RootPath = @"C:\中文 & 空格", FollowExplorer = true, ShowFiles = true, Width = 440, X = -120 }; Check(SettingsStore.Save(f, x), "Save failed"); var y = SettingsStore.Load(f); Check(y.RootPath == x.RootPath && y.FollowExplorer && y.ShowFiles && y.X == -120, "Roundtrip failed"); x.Width = 500; Check(SettingsStore.Save(f, x) && SettingsStore.Load(f).Width == 500, "Replace failed"); });
            Test("v1.1 double width and automatic defaults", () => { var x = new AppSettings(); Check(x.Width == 860 && x.FollowExplorer && x.ShowFiles,"New defaults"); });
            Test("map automatically includes grandchildren and files", () => {
                var d = Path.Combine(root,"map"); Directory.CreateDirectory(Path.Combine(d,"child","grandchild")); File.WriteAllText(Path.Combine(d,"child","grandchild","说明.txt"),"hello");
                var x = MapSnapshot.Read(d,true,5,2500,CancellationToken.None); Check(x.Count == 4 && x.Root.Children[0].Children[0].Children[0].Entry.Name == "说明.txt","Missing descendants");
            });
            Test("map depth boundary is explicit", () => { var x = MapSnapshot.Read(Path.Combine(root,"map"),true,1,2500,CancellationToken.None); Check(x.Count == 2 && x.Limited && x.Root.Children[0].Note != null,"Depth boundary"); });
            Test("map budget never overflows and marks omitted content", () => { var x = MapSnapshot.Read(large,true,5,25,100,new Dictionary<string,int>(),CancellationToken.None); Check(x.Count == 25 && x.Limited && x.Root.HasMore,"Budget exceeded"); });
            Test("map folder-only mode", () => { var x = MapSnapshot.Read(Path.Combine(root,"map"),false,5,2500,CancellationToken.None); Check(x.Count == 3,"Files visible"); });
            Test("map cancellation", () => { using var c = new CancellationTokenSource(); c.Cancel(); bool caught = false; try { MapSnapshot.Read(root,true,5,2500,c.Token); } catch(OperationCanceledException) { caught = true; } Check(caught,"Ignored cancellation"); });
            Test("map missing root reports error", () => { var x = MapSnapshot.Read(Path.Combine(root,"missing"),true,5,2500,CancellationToken.None); Check(x.Root.Note != null && x.Count == 1,"Missing error"); });
            Test("map avoids automatic reparse recursion", () => {
                var d = Path.Combine(root,"links-map"); Directory.CreateDirectory(d); Directory.CreateSymbolicLink(Path.Combine(d,"back"),d);
                var x = MapSnapshot.Read(d,true,12,2500,CancellationToken.None); Check(x.Count == 2 && x.Root.Children[0].Entry.IsReparsePoint && x.Root.Children[0].Children.Count == 0,"Recursed link");
            });
            string sixty = Path.Combine(root,"sixty"); Directory.CreateDirectory(sixty);
            for (int i=59;i>=0;i--) File.WriteAllText(Path.Combine(sixty,"文件 "+i.ToString("D2")+".txt"),"sample");
            Test("60 files: accurate total and only first five visible", () => {
                var x = MapSnapshot.Read(sixty,true,5,2500,CancellationToken.None);
                Check(x.Root.TotalFiles == 60 && x.Root.TotalFolders == 0 && x.Root.CountComplete && x.Root.Children.Count == 5 && x.Root.HasMore,"Wrong count or visible page");
                Check(x.Root.Children[0].Entry.Name == "文件 00.txt","Not sorted");
            });
            Test("successive five-item pages preserve every previous item", () => {
                var limits = new Dictionary<string,int>(); string[] previous = Array.Empty<string>();
                for(int limit=5;limit<=65;limit+=5) {
                    limits[sixty]=limit; var x=MapSnapshot.Read(sixty,true,5,2500,5,limits,CancellationToken.None);
                    var names=x.Root.Children.Select(n=>n.Entry.Name).ToArray();
                    Check(names.Length == Math.Min(limit,60) && names.Distinct().Count()==names.Length,"Page gap or duplicate");
                    Check(previous.SequenceEqual(names.Take(previous.Length)),"Prior page changed");
                    Check(x.Root.HasMore==(limit<60),"Incorrect more marker"); previous=names;
                }
            });
            Test("hidden files still counted and no false show-more", () => {
                var x=MapSnapshot.Read(sixty,false,5,2500,CancellationToken.None);
                Check(x.Root.TotalFiles==60 && x.Root.Children.Count==0 && !x.Root.HasMore,"Hidden mode counts");
            });
            Test("page sizes configurable without changing totals", () => {
                var x=MapSnapshot.Read(sixty,true,5,2500,10,new Dictionary<string,int>(),CancellationToken.None);
                Check(x.Root.Children.Count==10 && x.Root.TotalFiles==60,"Page size ignored");
            });
            Test("expanding one folder does not expand its siblings", () => {
                string d=Path.Combine(root,"siblings"); Directory.CreateDirectory(d);
                foreach(var n in new[]{"A","B"}) { Directory.CreateDirectory(Path.Combine(d,n)); for(int i=0;i<17;i++) File.WriteAllText(Path.Combine(d,n,i.ToString("D2")),""); }
                var l=new Dictionary<string,int>{{Path.Combine(d,"A"),10}};
                var x=MapSnapshot.Read(d,true,5,2500,5,l,CancellationToken.None);
                Check(x.Root.Children[0].Children.Count==10 && x.Root.Children[1].Children.Count==5,"Sibling pagination leaked");
            });
            Test("pagination settings survive save and reload", () => {
                string f=Path.Combine(root,"paging-settings.xml");var x=new AppSettings{PageSize=10,ExpandDepth=3};SettingsStore.Save(f,x);var y=SettingsStore.Load(f);Check(y.PageSize==10 && y.ExpandDepth==3,"Settings lost");
            });
            Console.WriteLine("\n" + passed + " core tests passed. Windows GUI/COM runtime tests were NOT run.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine("FAIL " + ex); return 1; }
        finally { try { Directory.Delete(root, true); } catch { } }
    }
}


