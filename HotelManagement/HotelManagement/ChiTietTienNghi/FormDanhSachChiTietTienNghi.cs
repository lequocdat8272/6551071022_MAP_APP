using HotelManagement.Common;
using HotelManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.ChiTietTienNghi;
public sealed class FormDanhSachChiTietTienNghi : FormDanhSach
{
    private readonly string _roomType;
    public FormDanhSachChiTietTienNghi(string maLPH, string tenLoaiPhong) : base("Tiện nghi của loại phòng: " + tenLoaiPhong,
        ("MaTN", "Mã tiện nghi"), ("TenTN", "Tên tiện nghi"), ("SL", "Số lượng"), ("GhiChu", "Ghi chú"))
    { _roomType = maLPH; }
    protected override async Task LoadRowsAsync()
    {
        await using var db = new HotelManagementContext();
        var rows = await db.Cttns.AsNoTracking().Where(x => x.MaLph == _roomType && x.DaXoa != true && x.MaTnNavigation.DaXoa != true)
            .OrderBy(x => x.MaTn).Select(x => new { x.MaTn, x.MaTnNavigation.TenTn, x.Sl }).ToListAsync(Lifetime);
        if (IsDisposed) return;
        Grid.Rows.Clear();
        foreach (var x in rows)
        {
            int row = Grid.Rows.Add(x.MaTn, x.TenTn, x.Sl ?? 0, x.Sl >= 1 ? "Có thể sử dụng tốt" : "Đang không sử dụng được");
            if (x.Sl is null or < 1) Grid.Rows[row].Cells["GhiChu"].Style.ForeColor = Color.Red;
        }
    }
    protected override Form CreateEditor(string? id) => id == null ? new FormThemChiTietTienNghi(_roomType) : new FormSuaChiTietTienNghi(_roomType, id);
    protected override async Task DeleteAsync(string id)
    {
        await using var db = new HotelManagementContext();
        var x = await db.Cttns.SingleAsync(x => x.MaLph == _roomType && x.MaTn == id, Lifetime);
        db.Cttns.Remove(x);
        await db.SaveChangesAsync(Lifetime);
    }
}
