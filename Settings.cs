using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;

internal static class Settings
{
    internal static readonly string Folder = AppDomain.CurrentDomain.BaseDirectory;
    private static Dictionary<string, string> values = new Dictionary<string, string>();

    internal static void Load()
    {
        var path = Path.Combine(Folder, "workbench.local.json");
        if (!File.Exists(path)) return;
        var data = new JavaScriptSerializer().Deserialize<Dictionary<string, string>>(File.ReadAllText(path));
        if (data == null) throw new FormatException("workbench.local.json 必须是 JSON 对象。");
        values = data;
    }

    internal static string Get(string name, string fallback = "")
    {
        string value;
        return values.TryGetValue(name, out value) && !string.IsNullOrWhiteSpace(value) ? value.Trim() : fallback;
    }

    internal static string Outputs
    {
        get
        {
            var fallback = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AICreationWorkbench", "output");
            return Path.GetFullPath(Get("output_dir", fallback));
        }
    }

    internal static Uri Web(string name, string fallback, bool local)
    {
        Uri url;
        if (!Uri.TryCreate(Get(name, fallback), UriKind.Absolute, out url) ||
            (url.Scheme != Uri.UriSchemeHttp && url.Scheme != Uri.UriSchemeHttps) ||
            url.UserInfo.Length > 0 || (local && (!url.IsLoopback || url.Query.Length > 0 || url.Fragment.Length > 0)))
            throw new FormatException("请在 workbench.local.json 中配置 " + name +
                (local ? "（本机 HTTP(S) 地址）。" : "（HTTP(S) 地址）。"));
        return url;
    }
}
