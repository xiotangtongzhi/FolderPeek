using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace FolderPeek
{
    public sealed class AppSettings
    {
        public bool Collapsed { get; set; }
        public bool DarkMode { get; set; }
        public string RootPath { get; set; }
        public int PageSize { get; set; } = 5;
        public int ExpandDepth { get; set; } = 5;
        public bool FollowExplorer { get; set; } = true;
        public bool ShowFiles { get; set; } = true;
        public bool AlwaysOnTop { get; set; }
        public bool HasBounds { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; } = 860;
        public int Height { get; set; } = 590;
    }
    internal static class SettingsStore
    {
        public static AppSettings Load(string file)
        {
            try
            {
                if (!File.Exists(file)) return new AppSettings();
                using (var reader = XmlReader.Create(file, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 65536 }))
                    return (AppSettings)new XmlSerializer(typeof(AppSettings)).Deserialize(reader);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is InvalidOperationException || ex is XmlException || ex is System.Security.SecurityException)
            { return new AppSettings(); }
        }
        public static bool Save(string file, AppSettings settings)
        {
            string temporary = file + ".tmp";
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(file));
                using (var writer = XmlWriter.Create(temporary, new XmlWriterSettings { Indent = true }))
                    new XmlSerializer(typeof(AppSettings)).Serialize(writer, settings);
                if (File.Exists(file)) File.Replace(temporary, file, null);
                else File.Move(temporary, file);
                return true;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is InvalidOperationException || ex is System.Security.SecurityException)
            { return false; }
            finally { try { if (File.Exists(temporary)) File.Delete(temporary); } catch { } }
        }
    }
}




