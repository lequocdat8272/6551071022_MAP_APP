using HotelManagement.Common;
using HotelManagement.DangNhap;
using HotelManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.NhanVien;
public sealed class FormDanhSachNhanVien : FormDanhSach
{
    public FormDanhSachNhanVien() : base("Danh sách nhân viên", ("MaNV", "Mã nhân viên"), ("TenNV", "Họ tên"),
        ("ChucVu", "Chức vụ"), ("Luong", "Lương (VNĐ)"), ("SDT", "Điện thoại"), ("CCCD", "CCCD"),
        ("NgaySinh", "Ngày sinh"), ("GioiTinh", "Giới tính"), ("DiaChi", "Địa chỉ"), ("Email", "Email"))
    {
        Grid.Columns["Luong"]!.DefaultCellStyle.Format = "N0";
        Grid.Columns["NgaySinh"]!.DefaultCellStyle.Format = "dd/MM/yyyy";
        foreach (DataGridViewColumn column in Grid.Columns) column.MinimumWidth = 90;
        Grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
    }
    protected override async Task LoadRowsAsync()
    {
        await using var db = new HotelManagementContext();
        var rows = await db.NhanViens.AsNoTracking().Where(x => x.DaXoa != true).OrderBy(x => x.MaNv).ToListAsync(Lifetime);
        if (IsDisposed) return;
        Grid.Rows.Clear();
        foreach (var x in rows) Grid.Rows.Add(x.MaNv, x.TenNv, x.ChucVu, x.Luong, x.Sdt, x.Cccd,
            x.NgaySinh.ToDateTime(TimeOnly.MinValue), x.GioiTinh, x.DiaChi, x.Email ?? "");
    }
    protected override Form CreateEditor(string? id) => id == null ? new FormThemNhanVien() : new FormSuaNhanVien(id);
    protected override async Task DeleteAsync(string id)
    {
        if (id == FormDangNhap.MaNV_DangNhap) throw new InvalidOperationException("Không thể xóa nhân viên đang đăng nhập.");
        await using var db = new HotelManagementContext();
        var x = await db.NhanViens.Include(x => x.TaiKhoans).SingleAsync(x => x.MaNv == id, Lifetime);
        x.DaXoa = true;
        foreach (var account in x.TaiKhoans) account.DaXoa = true;
        await db.SaveChangesAsync(Lifetime);
    }
}
