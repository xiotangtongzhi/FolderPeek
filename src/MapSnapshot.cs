using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace FolderPeek
{
    internal sealed class MapNode
    {
        public Entry Entry;
        public string Note;
        public int Depth, TotalFiles, TotalFolders;
        public bool CountComplete, HasMore;
        public readonly List<MapNode> Children = new List<MapNode>();
    }
    internal sealed class MapSnapshot
    {
        public MapNode Root;
        public int Count;
        public bool Limited;
        private sealed class EntryOrder : IComparer<Entry>
        {
            public int Compare(Entry a, Entry b)
            {
                int c = b.IsDirectory.CompareTo(a.IsDirectory);
                if (c == 0) c = StringComparer.CurrentCultureIgnoreCase.Compare(a.Name,b.Name);
                return c == 0 ? StringComparer.Ordinal.Compare(a.FullPath,b.FullPath) : c;
            }
        }
        public static MapSnapshot Read(string path, bool files, int depth, int budget, CancellationToken token)
        { return Read(path,files,depth,budget,5,new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase),token); }
        public static MapSnapshot Read(string path, bool files, int depth, int budget, int pageSize, IDictionary<string,int> limits, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            var name = new DirectoryInfo(path).Name;
            var result = new MapSnapshot { Root = new MapNode { Entry = new Entry(string.IsNullOrEmpty(name) ? path : name,path,true,false) }, Count = 1 };
            var queue = new Queue<MapNode>(); queue.Enqueue(result.Root);
            while (queue.Count > 0)
            {
                token.ThrowIfCancellationRequested();
                var node = queue.Dequeue();
                if (node.Entry.IsReparsePoint) { node.Note = L10n.T("链接 / 云目录：双击查看", "Link / cloud folder: double-click to view"); continue; }
                if (result.Count >= budget) { node.Note = L10n.T("分支较多，双击聚焦此文件夹", "Many branches; double-click to focus this folder"); result.Limited = true; continue; }
                int requested;
                if (!limits.TryGetValue(node.Entry.FullPath,out requested)) requested = pageSize;
                int keep = node.Depth >= depth ? 0 : Math.Max(0,Math.Min(requested,budget-result.Count));
                // Count only this folder, retain only the visible prefix. Hidden branches are never traversed.
                var first = new SortedSet<Entry>(new EntryOrder());
                using (var pager = new DirectoryPager(node.Entry.FullPath,true))
                {
                    DirectoryPage page;
                    do
                    {
                        page = pager.ReadPage(200,token);
                        foreach (var entry in page.Entries)
                        {
                            if (entry.IsDirectory) node.TotalFolders++; else node.TotalFiles++;
                            if (keep == 0 || (!files && !entry.IsDirectory)) continue;
                            first.Add(entry);
                            if (first.Count > keep) first.Remove(first.Max);
                        }
                        if (page.Error != null) { node.Note = page.Error; break; }
                    } while (page.HasMore);
                    node.CountComplete = page.Error == null;
                }
                foreach (var entry in first)
                {
                    var child = new MapNode { Entry = entry, Depth = node.Depth + 1 };
                    node.Children.Add(child); result.Count++;
                    if (entry.IsDirectory) queue.Enqueue(child);
                }
                int total = node.TotalFolders + (files ? node.TotalFiles : 0);
                if (node.Depth >= depth && total > 0) { node.Note = node.Note ?? L10n.T("已到展开深度，双击聚焦", "Depth limit reached; double-click to focus"); result.Limited = true; }
                else if (total > node.Children.Count)
                { node.HasMore = true; result.Limited = true; }
            }
            return result;
        }
    }
}
