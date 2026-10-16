using System;
using System.Globalization;
namespace FolderPeek
{
    internal static class L10n
    {
#if ENGLISH
        public static bool English = true;
#else
        public static bool English = false;
#endif
        public static string T(string chinese, string english) { return English ? english : chinese; }
        public static void Initialize()
        {
            var culture = CultureInfo.GetCultureInfo(English ? "en-US" : "zh-CN");
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
        }
    }
}
