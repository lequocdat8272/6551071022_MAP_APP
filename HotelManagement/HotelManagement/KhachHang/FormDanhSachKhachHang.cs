using HotelManagement.Controls;
using HotelManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.KhachHang;

public class FormDanhSachKhachHang : DanhSachForm
{
    public FormDanhSachKhachHang() : base("Danh sách khách hàng")
    {
        Grid.Name = "dtgDanhSachKhachHang";
        Columns(Properties.Resources.KhachHang, ("MaKH", "Mã KH"), ("TenKH", "Họ và tên"), ("CCCD", "CCCD/Passport"),
            ("SDT", "Điện thoại"), ("QuocTich", "Quốc tịch"), ("GioiTinh", "Giới tính"));
    }

    protected override async Task LoadRowsAsync()
    {
        await using var db = new HotelManagementContext();
        var items = await db.KhachHangs.AsNoTracking().Where(k => k.DaXoa != true).OrderBy(k => k.MaKh).ToListAsync(Lifetime.Token);
        if (IsDisposed) return;
        Grid.Rows.Clear();
        foreach (var k in items) Row(Properties.Resources.KhachHang, k.MaKh, k.TenKh, k.CccdPassport, k.Sdt, k.QuocTich, k.GioiTinh);
    }

    protected override Form CreateEditor(string? id) => id == null ? new FormThemKhachHang() : new FormSuaKhachHang(id);

    protected override async Task DeleteAsync(string id)
    {
        await using var db = new HotelManagementContext();
        await db.KhachHangs.Where(k => k.MaKh == id && k.DaXoa != true)
            .ExecuteUpdateAsync(s => s.SetProperty(k => k.DaXoa, true), Lifetime.Token);
    }
}
