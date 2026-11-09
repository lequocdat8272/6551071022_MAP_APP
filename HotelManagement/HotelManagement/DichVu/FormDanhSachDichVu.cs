using HotelManagement.Common;
using HotelManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.DichVu;

public sealed class FormDanhSachDichVu : FormDanhSach
{
    public FormDanhSachDichVu() : base("Danh sách dịch vụ", ("MaDV", "Mã dịch vụ"), ("TenDV", "Tên dịch vụ"),
        ("DonGia", "Đơn giá (VNĐ)"), ("SLConLai", "Số lượng còn lại"), ("LoaiDV", "Loại dịch vụ"))
    { Grid.Columns["DonGia"]!.DefaultCellStyle.Format = "N0"; }

    protected override async Task LoadRowsAsync()
    {
        await using var db = new HotelManagementContext();
        var rows = await db.DichVus.AsNoTracking().Where(x => x.DaXoa != true).OrderBy(x => x.MaDv).ToListAsync(Lifetime);
        if (IsDisposed) return;
        Grid.Rows.Clear();
        foreach (var x in rows) Grid.Rows.Add(x.MaDv, x.TenDv, x.DonGia, x.SlconLai is null or < 0 ? "Không giới hạn" : x.SlconLai, x.LoaiDv);
    }
    protected override Form CreateEditor(string? id) => id == null ? new FormThemDichVu() : new FormSuaDichVu { MaDichVu = id };
    protected override async Task DeleteAsync(string id)
    {
        await using var db = new HotelManagementContext();
        var x = await db.DichVus.SingleAsync(x => x.MaDv == id, Lifetime);
        x.DaXoa = true;
        await db.SaveChangesAsync(Lifetime);
    }
}
