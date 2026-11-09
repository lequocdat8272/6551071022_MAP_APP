using HotelManagement.Common;
using HotelManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.KhachHang;
public sealed class FormDanhSachKhachHang : FormDanhSach
{
    public FormDanhSachKhachHang() : base("Danh sách khách hàng", ("MaKH", "Mã khách hàng"), ("TenKH", "Họ tên"),
        ("CCCD", "CCCD/Passport"), ("SDT", "Số điện thoại"), ("QuocTich", "Quốc tịch"), ("GioiTinh", "Giới tính")) { }
    protected override async Task LoadRowsAsync()
    {
        await using var db = new HotelManagementContext();
        var rows = await db.KhachHangs.AsNoTracking().Where(x => x.DaXoa != true).OrderBy(x => x.MaKh).ToListAsync(Lifetime);
        if (IsDisposed) return;
        Grid.Rows.Clear();
        foreach (var x in rows) Grid.Rows.Add(x.MaKh, x.TenKh, x.CccdPassport, x.Sdt ?? "", x.QuocTich, x.GioiTinh);
    }
    protected override Form CreateEditor(string? id) => id == null ? new FormThemKhachHang() : new FormSuaKhachHang(id);
    protected override async Task DeleteAsync(string id)
    {
        await using var db = new HotelManagementContext();
        var x = await db.KhachHangs.SingleAsync(x => x.MaKh == id, Lifetime);
        x.DaXoa = true;
        await db.SaveChangesAsync(Lifetime);
    }
}
