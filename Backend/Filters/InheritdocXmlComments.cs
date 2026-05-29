using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace Backend.Filters;

internal sealed class InheritdocXmlComments
{
    private static readonly ConcurrentDictionary<string, InheritdocXmlComments> Cache =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly IReadOnlyDictionary<string, XElement> _members;

    private InheritdocXmlComments(string xmlPath)
    {
        if (string.IsNullOrWhiteSpace(xmlPath) || !File.Exists(xmlPath))
        {
            _members = new Dictionary<string, XElement>(StringComparer.OrdinalIgnoreCase);
            return;
        }

        var document = XDocument.Load(xmlPath);
        _members = document.Descendants("member")
            .Select(member => new
            {
                Name = member.Attribute("name")?.Value,
                Element = member
            })
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Name))
            .ToDictionary(entry => entry.Name!, entry => entry.Element, StringComparer.OrdinalIgnoreCase);
    }

    public static InheritdocXmlComments GetOrCreate(string xmlPath)
    {
        return Cache.GetOrAdd(xmlPath, path => new InheritdocXmlComments(path));
    }

    public string? GetSummary(string memberName)
    {
        if (!_members.TryGetValue(memberName, out var member))
            return null;

        var summaryElement = member.Element("summary");
        if (summaryElement != null)
            return Normalize(summaryElement.Value);

        var inheritdocElement = member.Element("inheritdoc");
        var cref = inheritdocElement?.Attribute("cref")?.Value;
        if (string.IsNullOrWhiteSpace(cref) || string.Equals(cref, memberName, StringComparison.Ordinal))
            return null;

        return GetSummary(cref);
    }

    public static string? GetMemberName(Type type)
    {
        if (type.IsGenericType && !type.IsGenericTypeDefinition)
            type = type.GetGenericTypeDefinition();

        return type.FullName == null ? null : $"T:{type.FullName.Replace('+', '.')}";
    }

    public static string? GetMemberName(PropertyInfo property)
    {
        var declaringType = property.DeclaringType;
        if (declaringType == null)
            return null;

        if (declaringType.IsGenericType && !declaringType.IsGenericTypeDefinition)
            declaringType = declaringType.GetGenericTypeDefinition();

        return declaringType.FullName == null
            ? null
            : $"P:{declaringType.FullName.Replace('+', '.')}.{property.Name}";
    }

    private static string Normalize(string value)
    {
        return string.Join(" ", value.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
    }
}
