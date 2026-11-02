using HotelManagement.Controls;
using HotelManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.TienNghi;

public class FormDanhSachTienNghi : DanhSachForm
{
    public FormDanhSachTienNghi() : base("Danh sách tiện nghi")
    {
        Grid.Name = "dtgDanhSachTienNghi";
        Columns(Properties.Resources.TienNghi, ("MaTN", "Mã tiện nghi"), ("TenTN", "Tên tiện nghi"));
    }

    protected override async Task LoadRowsAsync()
    {
        await using var db = new HotelManagementContext();
        var items = await db.TienNghis.AsNoTracking().Where(t => t.DaXoa != true).OrderBy(t => t.MaTn).ToListAsync(Lifetime.Token);
        if (IsDisposed) return;
        Grid.Rows.Clear();
        foreach (var t in items) Row(Properties.Resources.TienNghi, t.MaTn, t.TenTn);
    }

    protected override Form CreateEditor(string? id) => id == null ? new FormThemTienNghi() : new FormSuaTienNghi(id);

    protected override async Task DeleteAsync(string id)
    {
        await using var db = new HotelManagementContext();
        await db.TienNghis.Where(t => t.MaTn == id && t.DaXoa != true)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.DaXoa, true), Lifetime.Token);
    }
}
