using System.Collections.Concurrent;
using System.Reflection;

namespace Backend.Services.MailServices;

/// <summary>
/// Loads mail template HTML files that are embedded as resources under Services/MailServices/Templates, and performs simple {{Token}} substitution.
/// </summary>
internal static class MailTemplateLoader
{
    private static readonly ConcurrentDictionary<string, string> _templateCache = new();

    /// <summary>
    /// Loads the raw contents of a template, e.g. "nl/OutstandingPayment.html" or "Layout.html", relative to the Templates folder.
    /// </summary>
    private static string Load(string relativePath)
    {
        return _templateCache.GetOrAdd(relativePath, path =>
        {
            string resourceName = $"Backend.Services.MailServices.Templates.{path.Replace('/', '.')}";
            Assembly assembly = Assembly.GetExecutingAssembly();

            using Stream stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new FileNotFoundException($"Mail template '{resourceName}' was not found as an embedded resource.");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        });
    }

    /// <summary>
    /// Loads a template and replaces every "{{Key}}" placeholder with the matching value from <paramref name="tokens"/>.
    /// </summary>
    public static string Render(string relativePath, IReadOnlyDictionary<string, string> tokens)
    {
        string template = Load(relativePath);

        foreach (var (key, value) in tokens)
        {
            template = template.Replace("{{" + key + "}}", value);
        }

        return template;
    }
}
