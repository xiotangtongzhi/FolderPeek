using System;

namespace FolderPeek
{
    internal static class PathRules
    {
        public static bool IsWindowsFileSystemPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || path.IndexOf('\0') >= 0 || path.IndexOf('"') >= 0) return false;
            if (path.StartsWith(@"\\?\", StringComparison.Ordinal) || path.StartsWith(@"\\.\", StringComparison.Ordinal)) return false;
            if (path.Length >= 3 && char.IsLetter(path[0]) && path[1] == ':' && (path[2] == '\\' || path[2] == '/')) return true;
            if (path.StartsWith(@"\\", StringComparison.Ordinal))
            {
                var pieces = path.Substring(2).Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
                return pieces.Length >= 2 && pieces[0] != "." && pieces[0] != "?";
            }
            return false;
        }

        public static bool SamePath(string first, string second)
        { return string.Equals(first?.TrimEnd('\\', '/'), second?.TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase); }

    }
    internal enum NavigationAction { RejectChangedDirectory, ExploreDirectory, SelectFile }
    internal static class NavigationPolicy
    {
        public static NavigationAction Decide(bool expectedDirectory, bool actualDirectory)
        {
            if (expectedDirectory && !actualDirectory) return NavigationAction.RejectChangedDirectory;
            return actualDirectory ? NavigationAction.ExploreDirectory : NavigationAction.SelectFile;
        }
    }
}
