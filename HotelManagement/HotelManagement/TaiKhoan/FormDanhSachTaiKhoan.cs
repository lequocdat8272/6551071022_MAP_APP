using HotelManagement.Controls;
using HotelManagement.DangNhap;
using HotelManagement.Models;
using HotelManagement.Services;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.TaiKhoan;

public sealed class FormDanhSachTaiKhoan : DanhSachForm
{
    public FormDanhSachTaiKhoan() : base("Danh sách tài khoản")
    {
        Columns(Properties.Resources.TaiKhoan, ("TenTK", "Tên tài khoản"), ("TenNV", "Nhân viên"), ("Quyen", "Cấp quyền"));
    }

    protected override async Task LoadRowsAsync()
    {
        await using var db = new HotelManagementContext();
        await AccountService.RequireRoleAsync(db, FormDangNhap.TenTK_DangNhap, 3, Lifetime.Token);
        var rows = await db.TaiKhoans.AsNoTracking().Where(t => t.DaXoa != true).OrderBy(t => t.TenTk)
            .Select(t => new { t.TenTk, t.CapDoQuyen, t.MaNvNavigation.TenNv }).ToListAsync(Lifetime.Token);
        if (IsDisposed) return;
        Grid.Rows.Clear();
        foreach (var row in rows)
            Row(Properties.Resources.TaiKhoan, row.TenTk, row.TenNv, AccountService.RoleName(row.CapDoQuyen));
    }

    protected override Form CreateEditor(string? id) => new AccountEditor(id);

    protected override Task DeleteAsync(string id) => AccountService.DeleteAsync(FormDangNhap.TenTK_DangNhap, id, Lifetime.Token);
}
