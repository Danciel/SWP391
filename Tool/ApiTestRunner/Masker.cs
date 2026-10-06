using System.Text.RegularExpressions;

namespace ApiTestRunner;

/// <summary>Keeps passwords and full JWTs out of log files.</summary>
public static class Masker
{
    private static readonly Regex Password =
        new(@"(""password""\s*:\s*"")[^""]*("")", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex Token =
        new(@"(""accessToken""\s*:\s*"")([^""]{1,12})[^""]*("")", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>Set from plan "maskSecrets", --mask/--no-mask, or the UI checkbox. When false, logs show real values.</summary>
    public static bool Enabled { get; set; } = false;

    public static string Mask(string text)
    {
        if (!Enabled) return text;
        text = Password.Replace(text, "$1***$2");
        return Token.Replace(text, "$1$2...$3");
    }
}
