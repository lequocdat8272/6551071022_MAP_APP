using HotelManagement.Controls;
using HotelManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.NhanVien;

public class FormDanhSachNhanVien : DanhSachForm
{
    public FormDanhSachNhanVien() : base("Danh sách nhân viên")
    {
        Grid.Name = "dtgDanhSachNhanVien";
        Columns(Properties.Resources.NhanVien, ("MaNV", "Mã NV"), ("TenNV", "Họ và tên"), ("ChucVu", "Chức vụ"),
            ("Luong", "Lương"), ("SDT", "Điện thoại"), ("CCCD", "CCCD"), ("NgaySinh", "Ngày sinh"),
            ("GioiTinh", "Giới tính"), ("DiaChi", "Địa chỉ"), ("Email", "Email"));
        Grid.Columns["Luong"]!.DefaultCellStyle.Format = "#,##0.####' VNĐ'";
        Grid.Columns["NgaySinh"]!.DefaultCellStyle.Format = "dd/MM/yyyy";
        foreach (DataGridViewColumn c in Grid.Columns)
            if (c is not DataGridViewImageColumn) c.MinimumWidth = 100;
    }

    protected override async Task LoadRowsAsync()
    {
        await using var db = new HotelManagementContext();
        var items = await db.NhanViens.AsNoTracking().Where(n => n.DaXoa != true).OrderBy(n => n.MaNv).ToListAsync(Lifetime.Token);
        if (IsDisposed) return;
        Grid.Rows.Clear();
        foreach (var n in items) Row(Properties.Resources.NhanVien, n.MaNv, n.TenNv, n.ChucVu, n.Luong, n.Sdt,
            n.Cccd, n.NgaySinh.ToDateTime(TimeOnly.MinValue), n.GioiTinh, n.DiaChi, n.Email);
    }

    protected override Form CreateEditor(string? id) => id == null ? new FormThemNhanVien() : new FormSuaNhanVien(id);

    protected override async Task DeleteAsync(string id)
    {
        if (id == DangNhap.FormDangNhap.MaNV_DangNhap) throw new InvalidOperationException("Không thể xóa nhân viên đang đăng nhập.");
        await using var db = new HotelManagementContext();
        await db.NhanViens.Where(n => n.MaNv == id && n.DaXoa != true)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.DaXoa, true), Lifetime.Token);
    }
}
