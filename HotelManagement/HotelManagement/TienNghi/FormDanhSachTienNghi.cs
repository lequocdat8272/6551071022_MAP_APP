using HotelManagement.Common;
using HotelManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.TienNghi;
public sealed class FormDanhSachTienNghi : FormDanhSach
{
    public FormDanhSachTienNghi() : base("Danh sách tiện nghi", ("MaTN", "Mã tiện nghi"), ("TenTN", "Tên tiện nghi")) { }
    protected override async Task LoadRowsAsync()
    {
        await using var db = new HotelManagementContext();
        var rows = await db.TienNghis.AsNoTracking().Where(x => x.DaXoa != true).OrderBy(x => x.MaTn).ToListAsync(Lifetime);
        if (IsDisposed) return;
        Grid.Rows.Clear();
        foreach (var x in rows) Grid.Rows.Add(x.MaTn, x.TenTn);
    }
    protected override Form CreateEditor(string? id) => id == null ? new FormThemTienNghi() : new FormSuaTienNghi(id);
    protected override async Task DeleteAsync(string id)
    {
        await using var db = new HotelManagementContext();
        var x = await db.TienNghis.SingleAsync(x => x.MaTn == id, Lifetime);
        x.DaXoa = true;
        await db.SaveChangesAsync(Lifetime);
    }
}
