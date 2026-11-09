using HotelManagement.Models;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Security.Cryptography;

namespace HotelManagement.Services;

internal static class AccountService
{
    private const string PasswordPrefix = "PBKDF2-SHA256$";
    private const int PasswordIterations = 600_000;

    internal static string RoleName(int role) => role switch
    {
        1 => "Cấp 1 - Nhân viên",
        2 => "Cấp 2 - Quản lý",
        3 => "Cấp 3 - Admin",
        _ => $"Cấp {role}"
    };

    internal static string HashPassword(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(16);
        byte[] key = Rfc2898DeriveBytes.Pbkdf2(password, salt, PasswordIterations, HashAlgorithmName.SHA256, 32);
        return PasswordPrefix + Convert.ToBase64String(salt) + "$" + Convert.ToBase64String(key);
    }

    internal static bool VerifyPassword(string stored, string password)
    {
        // Tài khoản từ dữ liệu mẫu vẫn đăng nhập được trước khi đổi mật khẩu.
        if (!stored.StartsWith(PasswordPrefix, StringComparison.Ordinal))
            return string.Equals(stored, password, StringComparison.Ordinal);
        string[] parts = stored[PasswordPrefix.Length..].Split('$');
        if (parts.Length != 2) return false;
        try
        {
            byte[] salt = Convert.FromBase64String(parts[0]);
            byte[] expected = Convert.FromBase64String(parts[1]);
            if (salt.Length != 16 || expected.Length != 32) return false;
            byte[] actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, PasswordIterations, HashAlgorithmName.SHA256, 32);
            return CryptographicOperations.FixedTimeEquals(expected, actual);
        }
        catch (FormatException) { return false; }
    }

    internal static async Task RequireRoleAsync(HotelManagementContext db, string actor, int minRole, CancellationToken token)
    {
        if (!await db.TaiKhoans.AnyAsync(t => t.TenTk == actor && t.CapDoQuyen >= minRole && t.CapDoQuyen <= 3
            && t.DaXoa != true && t.MaNvNavigation.DaXoa != true, token))
            throw new InvalidOperationException("Tài khoản không có quyền thực hiện thao tác này.");
    }

    internal static async Task SaveAsync(string actor, string? originalName, string name, string password,
        string employeeId, int role, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 50 || role is < 1 or > 3)
            throw new InvalidOperationException("Tên tài khoản hoặc cấp quyền không hợp lệ.");
        if ((originalName == null && string.IsNullOrWhiteSpace(password)) || password.Length > 100
            || (password.Length > 0 && string.IsNullOrWhiteSpace(password)))
            throw new InvalidOperationException("Mật khẩu phải có từ 1 đến 100 ký tự và không được chỉ chứa khoảng trắng.");
        if (string.Equals(actor, originalName, StringComparison.OrdinalIgnoreCase) &&
            (!string.Equals(name, originalName, StringComparison.Ordinal) || role != 3))
            throw new InvalidOperationException("Không thể đổi tên hoặc hạ quyền của tài khoản đang đăng nhập.");

        string? newHash = password.Length == 0 ? null : HashPassword(password);
        await using var db = new HotelManagementContext();
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
        await RequireRoleAsync(db, actor, 3, token);

        if (originalName == null)
        {
            if (!await db.NhanViens.AnyAsync(n => n.MaNv == employeeId && n.DaXoa != true, token))
                throw new InvalidOperationException("Nhân viên không còn hoạt động.");
            var account = await db.TaiKhoans.SingleOrDefaultAsync(t => t.TenTk == name, token);
            if (account is { DaXoa: not true })
                throw new InvalidOperationException("Tên tài khoản đã tồn tại.");
            if (account == null)
            {
                account = new Models.TaiKhoan { TenTk = name };
                db.TaiKhoans.Add(account);
            }
            account.MaNv = employeeId;
            account.CapDoQuyen = role;
            account.Password = newHash!;
            account.DaXoa = false;
            await db.SaveChangesAsync(token);
        }
        else
        {
            if (await db.TaiKhoans.AnyAsync(t => t.TenTk == name && t.TenTk != originalName, token))
                throw new InvalidOperationException("Tên tài khoản đã tồn tại.");
            int changed = await db.TaiKhoans.Where(t => t.TenTk == originalName && t.DaXoa != true)
                .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.TenTk, name)
                    .SetProperty(t => t.Password, t => newHash ?? t.Password)
                    .SetProperty(t => t.CapDoQuyen, role), token);
            if (changed == 0) throw new InvalidOperationException("Tài khoản không còn tồn tại.");
        }
        await transaction.CommitAsync(token);
    }

    internal static async Task DeleteAsync(string actor, string name, CancellationToken token)
    {
        if (string.Equals(actor, name, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Không thể xóa tài khoản đang đăng nhập.");
        await using var db = new HotelManagementContext();
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
        await RequireRoleAsync(db, actor, 3, token);
        int changed = await db.TaiKhoans.Where(t => t.TenTk == name && t.DaXoa != true)
            .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.DaXoa, true), token);
        if (changed == 0) throw new InvalidOperationException("Tài khoản không còn tồn tại.");
        await transaction.CommitAsync(token);
    }

    internal static async Task ChangePasswordAsync(string actor, string oldPassword, string newPassword, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length > 100)
            throw new InvalidOperationException("Mật khẩu mới phải có từ 1 đến 100 ký tự và không được chỉ chứa khoảng trắng.");
        await using var db = new HotelManagementContext();
        var account = await db.TaiKhoans.AsNoTracking()
            .SingleOrDefaultAsync(t => t.TenTk == actor && t.DaXoa != true && t.MaNvNavigation.DaXoa != true, token);
        if (account == null || !VerifyPassword(account.Password, oldPassword))
            throw new InvalidOperationException("Mật khẩu hiện tại không đúng hoặc tài khoản đã ngừng hoạt động.");

        string newHash = HashPassword(newPassword);
        int changed = await db.TaiKhoans.Where(t => t.TenTk == actor && t.DaXoa != true && t.MaNvNavigation.DaXoa != true
                && EF.Functions.Collate(t.Password, "Latin1_General_100_BIN2") == account.Password)
            .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.Password, newHash), token);
        if (changed == 0) throw new InvalidOperationException("Thông tin tài khoản đã thay đổi. Vui lòng đăng nhập lại.");
    }
}
