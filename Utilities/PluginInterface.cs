using System.Net;
using System.Collections.Generic;

namespace Utilities
{
    public interface PluginInterface
    {
        string Name { get; }
        bool Enabled { get; }
        bool IsLoggedIn { get; }

        void SetLoggerDelegate(LoggerDelegate loggerDelegate);
        void InitializePlugin();
        void FinalizePlugin();
        void LoadSettings();
        void SaveSettings();
        void ShowPluginForm();
        bool Login(bool force = false);
        CookieCollection GetCookieCollection();
        bool IsLogoutUrl(string url);
        bool IsParseUrl(string url);
        List<UrlContainer.UrlContainer> GetImageUrlList(UrlContainer.UrlContainer uc, string[] format);
    }
}
