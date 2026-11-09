using HotelManagement.Controls;
using HotelManagement.Models;
using HotelManagement.Services;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.PhieuThue;

public sealed class FormDanhSachDichVuPhongDaDung : DanhSachForm
{
    private readonly string _detailId;
    private readonly Label _summary = new() { Dock = DockStyle.Top, Height = 46, Padding = new Padding(14, 12, 10, 0) };

    public FormDanhSachDichVuPhongDaDung(string detailId) : base("Dịch vụ phòng đã dùng", detailsOnly: true)
    {
        _detailId = detailId;
        Controls.Add(_summary);
        AddButton.Text = "Thêm dịch vụ";
        AddButton.Visible = false;
        Columns(Properties.Resources.DichVu, ("MaDV", "Mã dịch vụ"), ("Ten", "Tên dịch vụ"),
            ("Gia", "Đơn giá"), ("SL", "Số lượng"), ("Tien", "Thành tiền"));
        Grid.Columns["Sua"]!.Visible = false;
        Grid.Columns["Gia"]!.DefaultCellStyle.Format = Grid.Columns["Tien"]!.DefaultCellStyle.Format = "N0";
    }

    protected override async Task LoadRowsAsync()
    {
        await using var db = new HotelManagementContext();
        var stay = await StayService.ActiveDetails(db).AsNoTracking().Where(c => c.MaCtdp == _detailId)
            .Select(c => new { c.MaPh, c.TrangThai }).SingleAsync(Lifetime.Token);
        var rows = await StayService.UsedServices(db, _detailId).AsNoTracking().OrderBy(v => v.MaDv)
            .Select(v => new { v.MaDv, v.MaDvNavigation.TenDv, v.Sl, Price = v.DonGia ?? v.MaDvNavigation.DonGia,
                Total = v.ThanhTien ?? v.Sl * (v.DonGia ?? v.MaDvNavigation.DonGia) }).ToListAsync(Lifetime.Token);
        if (IsDisposed) return;
        AddButton.Visible = stay.TrangThai == StayService.Occupied;
        _summary.Text = $"Phòng {stay.MaPh}   •   Chi tiết {_detailId}   •   Tổng dịch vụ: {rows.Sum(v => v.Total):N0} VNĐ";
        Grid.Rows.Clear();
        foreach (var row in rows)
            Row(Properties.Resources.DichVu, row.MaDv, row.TenDv, row.Price, row.Sl, row.Total);
    }

    protected override Form CreateEditor(string? id) => new FormThemDichVuPhongDaDung(_detailId);
}
