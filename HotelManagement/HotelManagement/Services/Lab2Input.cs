using System.Net.Mail;
using System.Text.RegularExpressions;

namespace HotelManagement.Services;

public static class Lab2Input
{
    public static string Required(string value, string label, int maxLength)
    {
        value = value.Trim();
        if (value.Length == 0 || value.Length > maxLength)
            throw new ArgumentException($"{label} bắt buộc, tối đa {maxLength} ký tự.");
        return value;
    }
    public static bool Digits(string value) => value.Length > 0 && value.All(c => c >= '0' && c <= '9');
    public static string Phone(string value, bool required = true)
    {
        value = value.Trim();
        if (!required && value.Length == 0) return value;
        if (value.Length != 10 || !Digits(value)) throw new ArgumentException("Số điện thoại phải gồm 10 chữ số.");
        return value;
    }
    public static void Customer(string name, string passport, string phone, string nationality, string gender, bool phoneRequired = false)
    {
        Required(name, "Họ tên", 50);
        Required(passport, "CCCD/Passport", 12);
        if ((phoneRequired && phone.Length == 0) || phone.Length > 10 || (phone.Length > 0 && !Digits(phone)))
            throw new ArgumentException("Số điện thoại khách hàng chỉ gồm chữ số, tối đa 10 ký tự.");
        Required(nationality, "Quốc tịch", 30);
        if (gender != "Nam" && gender != "Nữ") throw new ArgumentException("Vui lòng chọn giới tính.");
    }
    public static void Employee(string name, string role, decimal salary, string phone, string identity, DateTime birth, string gender, string address, string email)
    {
        Required(name, "Họ tên", 50);
        Required(role, "Chức vụ", 50);
        if (salary <= 0 || salary > 922337203685477m) throw new ArgumentException("Lương phải lớn hơn 0 và nằm trong giới hạn MONEY.");
        Phone(phone);
        if (identity.Length != 12 || !Digits(identity)) throw new ArgumentException("CCCD phải gồm 12 chữ số.");
        if (birth.Date > DateTime.Today.AddYears(-18)) throw new ArgumentException("Nhân viên phải đủ 18 tuổi.");
        if (gender != "Nam" && gender != "Nữ") throw new ArgumentException("Vui lòng chọn giới tính.");
        Required(address, "Địa chỉ", 100);
        Required(email, "Email", 100);
        if (!MailAddress.TryCreate(email, out var parsed) || parsed.Address != email || !email.Contains('.'))
            throw new ArgumentException("Email không đúng định dạng.");
    }
    public static string NextId(IEnumerable<string> ids, string prefix, int digits, int maxLength)
    {
        int max = ids.Where(x => x.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Select(x => int.TryParse(x[prefix.Length..], out int n) ? n : 0).DefaultIfEmpty().Max();
        string next = prefix + checked(max + 1).ToString("D" + digits);
        if (next.Length > maxLength) throw new InvalidOperationException($"Đã hết mã có tiền tố {prefix}.");
        return next;
    }
    public static string Id(string id, string label)
    {
        id = Required(id, label, 5).ToUpperInvariant();
        if (!Regex.IsMatch(id, "^[A-Z0-9]+$")) throw new ArgumentException(label + " chỉ gồm chữ cái không dấu và chữ số.");
        return id;
    }
}
