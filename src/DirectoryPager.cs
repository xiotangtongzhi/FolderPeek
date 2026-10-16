using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace FolderPeek
{
    internal sealed class Entry
    {
        public string Name { get; private set; }
        public string FullPath { get; private set; }
        public bool IsDirectory { get; private set; }
        public bool IsReparsePoint { get; private set; }
        public Entry(string name, string path, bool directory, bool reparse)
        { Name = name; FullPath = path; IsDirectory = directory; IsReparsePoint = reparse; }
    }

    internal sealed class DirectoryPage
    {
        public List<Entry> Entries { get; } = new List<Entry>();
        public bool HasMore { get; internal set; }
        public string Error { get; internal set; }
    }

    // Only ReadPage/Dispose touch the enumerator, and callers serialize them off the UI thread.
    // There is no recursive scan, full-list sort or directory watcher.
    internal sealed class DirectoryPager : IDisposable
    {
        private readonly string path;
        private readonly bool showFiles;
        private IEnumerator<FileSystemInfo> enumerator;
        private Entry lookahead;
        private int phase;
        private bool ended;
        public DirectoryPager(string path, bool showFiles) { this.path = path; this.showFiles = showFiles; }

        public DirectoryPage ReadPage(int pageSize, CancellationToken token)
        {
            if (pageSize < 1 || pageSize > 2000) throw new ArgumentOutOfRangeException(nameof(pageSize));
            var result = new DirectoryPage();
            token.ThrowIfCancellationRequested();
            if (ended) return result;
            try
            {
                while (result.Entries.Count < pageSize)
                {
                    token.ThrowIfCancellationRequested();
                    Entry next = lookahead ?? Next(token);
                    lookahead = null;
                    if (next == null) break;
                    result.Entries.Add(next);
                }
                token.ThrowIfCancellationRequested();
                lookahead = Next(token);
                result.HasMore = lookahead != null;
            }
            catch (OperationCanceledException) { Dispose(); throw; }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is System.Security.SecurityException || ex is ArgumentException || ex is NotSupportedException)
            {
                result.Error = FriendlyError(ex);
                Dispose();
            }
            return result;
        }

        private Entry Next(CancellationToken token)
        {
            while (!ended)
            {
                token.ThrowIfCancellationRequested();
                if (enumerator == null)
                {
                    var directory = new DirectoryInfo(path);
                    enumerator = phase == 0
                        ? directory.EnumerateDirectories().GetEnumerator()
                        : directory.EnumerateFiles().GetEnumerator();
                }
                if (enumerator.MoveNext())
                {
                    token.ThrowIfCancellationRequested();
                    FileSystemInfo item = enumerator.Current;
                    FileAttributes attributes = item.Attributes;
                    bool isDirectory = phase == 0;
                    // Attribute metadata is cached by DirectoryInfo enumeration. Reparse points
                    // are shown normally but never traversed until the user expands that node.
                    return new Entry(item.Name, item.FullName, isDirectory, (attributes & FileAttributes.ReparsePoint) != 0);
                }
                enumerator.Dispose();
                enumerator = null;
                if (phase == 0 && showFiles) phase = 1;
                else ended = true;
            }
            return null;
        }

        public void Dispose()
        {
            ended = true;
            lookahead = null;
            if (enumerator != null) { enumerator.Dispose(); enumerator = null; }
        }

        internal static string FriendlyError(Exception ex)
        {
            if (ex is UnauthorizedAccessException || ex is System.Security.SecurityException) return L10n.T("无权访问此目录", "Access to this folder is denied");
            if (ex is DirectoryNotFoundException || ex is FileNotFoundException) return L10n.T("目录已移动、删除或暂时不可用", "Folder moved, deleted or temporarily unavailable");
            if (ex is PathTooLongException) return L10n.T("路径太长，当前系统无法读取", "The path is too long for this system");
            if (ex is IOException) return L10n.T("读取失败：设备或网络位置可能不可用", "Cannot read: device or network location may be unavailable");
            return L10n.T("无法读取此路径", "Cannot read this path");
        }
    }

    internal sealed class GenerationGate
    {
        private int current;
        public int Current => Volatile.Read(ref current);
        public int Advance() => Interlocked.Increment(ref current);
        public bool IsCurrent(int generation) => Current == generation;
    }
}
