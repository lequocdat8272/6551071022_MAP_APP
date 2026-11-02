using HotelManagement.Controls;
using HotelManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.ChiTietTienNghi;

public class FormDanhSachChiTietTienNghi : DanhSachForm
{
    private readonly string _roomTypeId;

    public FormDanhSachChiTietTienNghi(string maLPH, string tenLPH) : base("Tiện nghi loại phòng: " + tenLPH)
    {
        _roomTypeId = maLPH;
        ClientSize = new Size(760, 520);
        Grid.Name = "dtgDanhSachChiTietTienNghi";
        Columns(Properties.Resources.TienNghi, ("MaTN", "Mã tiện nghi"), ("TenTN", "Tên tiện nghi"), ("SL", "Số lượng"));
        var title = new Label { Text = "DANH SÁCH TIỆN NGHI - " + tenLPH + " (" + maLPH + ")", Dock = DockStyle.Top,
            Height = 45, TextAlign = ContentAlignment.MiddleCenter, ForeColor = Color.Firebrick };
        Controls.Add(title);
    }

    protected override async Task LoadRowsAsync()
    {
        await using var db = new HotelManagementContext();
        var items = await db.Cttns.AsNoTracking().Where(c => c.MaLph == _roomTypeId && c.DaXoa != true && c.MaTnNavigation.DaXoa != true)
            .OrderBy(c => c.MaTn).Select(c => new { c.MaTn, c.MaTnNavigation.TenTn, c.Sl }).ToListAsync(Lifetime.Token);
        if (IsDisposed) return;
        Grid.Rows.Clear();
        foreach (var c in items) Row(Properties.Resources.TienNghi, c.MaTn, c.TenTn, c.Sl is null or < 0 ? "Không giới hạn" : (object)c.Sl.Value);
    }

    protected override Form CreateEditor(string? id) => id == null
        ? new FormThemChiTietTienNghi(_roomTypeId) : new FormSuaChiTietTienNghi(_roomTypeId, id);

    protected override async Task DeleteAsync(string id)
    {
        await using var db = new HotelManagementContext();
        // Xóa liên kết giữa tiện nghi và loại phòng, không xóa danh mục tiện nghi.
        await db.Cttns.Where(c => c.MaLph == _roomTypeId && c.MaTn == id)
            .ExecuteDeleteAsync(Lifetime.Token);
    }
}
