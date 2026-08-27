namespace Backend.Utils;

/// <summary>
/// Provides utility methods for handling CSV data, including escaping fields to prevent CSV Injection and formatting lines.
/// </summary>
public static class CsvUtils
{
    private static readonly char[] _formulaTriggers = { '=', '+', '-', '@', '\t', '\r' };

    /// <summary>
    /// Escapes a single CSV field to prevent CSV Injection (Formula Injection) 
    /// and column breakage caused by delimiters or newlines.
    /// </summary>
    public static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "";
        }

        // Prevent Formula Injection in Excel/Calc
        if (_formulaTriggers.Contains(value[0]))
        {
            value = "'" + value;
        }

        // Escape double quotes by doubling them
        var escaped = value.Replace("\"", "\"\"");

        // Wrap in quotes if it contains delimiter (;), double quotes, or newlines
        if (escaped.Contains(';') || escaped.Contains('"') || escaped.Contains('\n') || escaped.Contains('\r') || escaped.Contains(','))
        {
            return $"\"{escaped}\"";
        }

        return escaped;
    }

    /// <summary>
    /// Formats a full CSV line using semicolon (;) delimiter.
    /// </summary>
    public static string FormatLine(params object?[] fields)
    {
        return string.Join(";", fields.Select(f =>
        {
            if (f == null) return "";
            if (f is decimal dec) return dec.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
            if (f is double dbl) return dbl.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
            return Escape(f.ToString());
        }));
    }
}
