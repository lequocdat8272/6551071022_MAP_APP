using HotelManagement.Controls;
using HotelManagement.Models;
using HotelManagement.SoDoPhong;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.PhieuThue;

public sealed class FormDanhSachPhieuThue : DanhSachForm
{
    public FormDanhSachPhieuThue() : base("Danh sách phiếu thuê", detailsOnly: true)
    {
        AddButton.Text = "Đặt phòng";
        Search.PlaceholderText = "Mã phiếu hoặc tên khách";
        Columns(Properties.Resources.MenuDatPhong, ("MaPT", "Mã phiếu"), ("Khach", "Khách hàng"),
            ("Ngay", "Ngày lập"), ("NhanVien", "Nhân viên lập"));
        Grid.Columns["Ngay"]!.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
    }

    protected override async Task LoadRowsAsync()
    {
        await using var db = new HotelManagementContext();
        var rows = await db.PhieuThues.AsNoTracking().Where(p => p.DaXoa != true)
            .OrderByDescending(p => p.NgPt).ThenBy(p => p.MaPt)
            .Select(p => new { p.MaPt, p.NgPt, p.MaKhNavigation.TenKh, p.MaNvNavigation.TenNv })
            .ToListAsync(Lifetime.Token);
        if (IsDisposed) return;
        Grid.Rows.Clear();
        foreach (var row in rows)
            Row(Properties.Resources.MenuDatPhong, row.MaPt, row.TenKh, row.NgPt, row.TenNv);
    }

    protected override Form CreateEditor(string? id) => id == null ? new FormDatPhong() : new FormChiTietPhieuThue(id);
}
