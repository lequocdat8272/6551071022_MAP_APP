using HotelManagement.Controls;
using HotelManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.DichVu;

public class FormDanhSachDichVu : DanhSachForm
{
    public FormDanhSachDichVu() : base("Danh sách dịch vụ")
    {
        Grid.Name = "dtgDanhSachDichVu";
        Columns(Properties.Resources.DichVu, ("MaDV", "Mã DV"), ("TenDV", "Tên dịch vụ"),
            ("DonGia", "Đơn giá"), ("SLConLai", "Số lượng còn lại"), ("LoaiDV", "Loại dịch vụ"));
        Grid.Columns["DonGia"]!.DefaultCellStyle.Format = "#,##0.####' VNĐ'";
    }

    protected override async Task LoadRowsAsync()
    {
        await using var db = new HotelManagementContext();
        var items = await db.DichVus.AsNoTracking().Where(d => d.DaXoa != true).OrderBy(d => d.MaDv).ToListAsync(Lifetime.Token);
        if (IsDisposed) return;
        Grid.Rows.Clear();
        foreach (var d in items) Row(Properties.Resources.DichVu, d.MaDv, d.TenDv, d.DonGia,
            d.SlconLai is null or < 0 ? "Không giới hạn" : (object)d.SlconLai.Value, d.LoaiDv);
    }

    protected override Form CreateEditor(string? id) => id == null ? new FormThemDichVu() : new FormSuaDichVu { MaDichVu = id };

    protected override async Task DeleteAsync(string id)
    {
        await using var db = new HotelManagementContext();
        await db.DichVus.Where(d => d.MaDv == id && d.DaXoa != true)
            .ExecuteUpdateAsync(s => s.SetProperty(d => d.DaXoa, true), Lifetime.Token);
    }
}
