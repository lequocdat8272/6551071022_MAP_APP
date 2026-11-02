using System.Globalization;

namespace HotelManagement.Services;

public static class Lab2Input
{
    public static bool Digits(string text) => text.Length > 0 && text.All(char.IsAsciiDigit);

    public static string NextId(IEnumerable<string> ids, string prefix, int digits, int maxLength)
    {
        int max = ids.Where(id => id.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Select(id => int.TryParse(id.AsSpan(prefix.Length), out int n) ? n : 0).DefaultIfEmpty().Max();
        string next = prefix + (max + 1).ToString("D" + digits, CultureInfo.InvariantCulture);
        if (next.Length > maxLength) throw new InvalidOperationException($"Đã hết mã {prefix} (tối đa {maxLength} ký tự).");
        return next;
    }

    public static void Phone(string value, bool required = true)
    {
        if (!required && value.Length == 0) return;
        if (!Digits(value) || value.Length is < 9 or > 10)
            throw new InvalidOperationException("Số điện thoại phải gồm 9 đến 10 chữ số.");
    }
}
