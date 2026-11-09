using HotelManagement.Controls;
using HotelManagement.DangNhap;
using HotelManagement.Models;
using HotelManagement.Services;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.TaiKhoan;

internal sealed class AccountEditor : NhapLieuForm
{
    private readonly string? _originalName;
    private readonly TextBox _name;
    private readonly TextBox _password;
    private readonly ComboBox _employee;
    private readonly ComboBox _role;

    internal AccountEditor(string? accountName) : base(accountName == null ? "Thêm tài khoản" : "Sửa tài khoản")
    {
        _originalName = accountName;
        bool isCurrent = string.Equals(accountName, FormDangNhap.TenTK_DangNhap, StringComparison.OrdinalIgnoreCase);
        _name = TextField("Tên tài khoản", "TenTK", 50, isCurrent);
        _password = TextField(accountName == null ? "Mật khẩu" : "Mật khẩu mới", "MatKhau", 100);
        _password.UseSystemPasswordChar = true;
        if (accountName != null) _password.PlaceholderText = "Để trống để giữ mật khẩu hiện tại";
        _employee = ChoiceField("Nhân viên", "NhanVien");
        _employee.Enabled = accountName == null;
        _role = ChoiceField("Cấp quyền", "Quyen", AccountService.RoleName(1), AccountService.RoleName(2), AccountService.RoleName(3));
        _role.Enabled = !isCurrent;
        _role.SelectedIndex = 0;
        ClientSize = new Size(620, 435);
    }

    protected override async Task LoadDataAsync()
    {
        await using var db = new HotelManagementContext();
        await AccountService.RequireRoleAsync(db, FormDangNhap.TenTK_DangNhap, 3, Lifetime.Token);
        _employee.DisplayMember = nameof(EmployeeOption.Label);
        if (_originalName == null)
        {
            _employee.DataSource = await db.NhanViens.AsNoTracking().Where(n => n.DaXoa != true).OrderBy(n => n.MaNv)
                .Select(n => new EmployeeOption(n.MaNv, n.MaNv + " - " + n.TenNv)).ToListAsync(Lifetime.Token);
        }
        else
        {
            var row = await db.TaiKhoans.AsNoTracking().Where(t => t.TenTk == _originalName && t.DaXoa != true)
                .Select(t => new { t.TenTk, t.MaNv, t.MaNvNavigation.TenNv, t.CapDoQuyen }).SingleAsync(Lifetime.Token);
            _name.Text = row.TenTk;
            _employee.DataSource = new[] { new EmployeeOption(row.MaNv, $"{row.MaNv} - {row.TenNv}") };
            _role.SelectedIndex = row.CapDoQuyen is >= 1 and <= 3 ? row.CapDoQuyen - 1 : -1;
        }
    }

    protected override Task SaveDataAsync()
    {
        if (_employee.SelectedItem is not EmployeeOption employee)
            throw new InvalidOperationException("Hãy chọn nhân viên.");
        return AccountService.SaveAsync(FormDangNhap.TenTK_DangNhap, _originalName, Required(_name, "Tên tài khoản", 50),
            _password.Text, employee.Id, _role.SelectedIndex + 1, Lifetime.Token);
    }

    private sealed record EmployeeOption(string Id, string Label);
}
