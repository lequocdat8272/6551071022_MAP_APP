using HotelManagement.Controls;
using HotelManagement.DangNhap;
using HotelManagement.Models;
using HotelManagement.Services;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.PhieuThue;

public sealed class FormChiTietPhieuThue : DanhSachForm
{
    private readonly string _rentalId;
    private readonly Label _summary = new() { Dock = DockStyle.Top, Height = 68, Padding = new Padding(14, 10, 10, 4) };
    private readonly Button _checkIn = new() { Text = "Nhận phòng", AutoSize = true };
    private readonly Button _cancel = new() { Text = "Hủy đặt phòng", AutoSize = true };
    private readonly Button _checkout = new() { Text = "Trả phòng / Lập hóa đơn", AutoSize = true };

    public FormChiTietPhieuThue(string rentalId) : base($"Chi tiết phiếu thuê {rentalId}", detailsOnly: true)
    {
        _rentalId = rentalId;
        ClientSize = new Size(1080, 600);
        Controls.Add(_summary);
        AddButton.Visible = false;
        Search.Width = 155;
        Search.PlaceholderText = "Mã phòng / trạng thái";
        Toolbar.Controls.AddRange([_checkIn, _cancel, _checkout]);
        Columns(Properties.Resources.Phong, ("MaCTDP", "Mã chi tiết"), ("Phong", "Phòng"), ("Den", "Ngày nhận"),
            ("Di", "Ngày trả"), ("TrangThai", "Trạng thái"), ("Tien", "Tiền phòng"));
        Grid.Columns["Sua"]!.HeaderText = "Dịch vụ";
        Grid.Columns["Den"]!.DefaultCellStyle.Format = Grid.Columns["Di"]!.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
        Grid.Columns["Tien"]!.DefaultCellStyle.Format = "N0";
        Grid.SelectionChanged += (_, _) => UpdateActions();
        Grid.EnabledChanged += (_, _) => UpdateActions();
        _checkIn.Click += async (_, _) =>
        {
            if (SelectedStay() is not { } row || row.Status != StayService.Reserved) return;
            using var editor = new FormSuaChiTietPhieuThue(row.Id, row.Room);
            if (editor.ShowDialog(this) == DialogResult.OK) await ReloadAsync();
        };
        _cancel.Click += async (_, _) =>
        {
            if (SelectedStay() is not { } row || row.Status != StayService.Reserved) return;
            if (MessageBox.Show(this, $"Hủy đặt phòng {row.Room}?", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            await RunAsync(() => StayService.CancelAsync(row.Id, Lifetime.Token));
            await ReloadAsync();
        };
        _checkout.Click += async (_, _) =>
        {
            if (SelectedStay() is not { } row || row.Status != StayService.Occupied) return;
            if (MessageBox.Show(this, $"Trả phòng {row.Room} và lập hóa đơn?", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            string? invoiceId = null;
            await RunAsync(async () => invoiceId = await StayService.CheckoutAsync(row.Id, FormDangNhap.MaNV_DangNhap, Lifetime.Token));
            await ReloadAsync();
            if (invoiceId != null && !IsDisposed)
            {
                using var invoice = new HoaDon.FormHoaDon(invoiceId);
                invoice.ShowDialog(this);
            }
        };
        UpdateActions();
    }

    private StayRow? SelectedStay() => Grid.Enabled ? Grid.CurrentRow?.Tag as StayRow : null;

    private void UpdateActions()
    {
        var row = SelectedStay();
        _checkIn.Enabled = _cancel.Enabled = row?.Status == StayService.Reserved;
        _checkout.Enabled = row?.Status == StayService.Occupied;
    }

    protected override async Task LoadRowsAsync()
    {
        await using var db = new HotelManagementContext();
        var header = await db.PhieuThues.AsNoTracking().Where(p => p.MaPt == _rentalId && p.DaXoa != true)
            .Select(p => new { p.NgPt, p.MaKhNavigation.TenKh, p.MaNvNavigation.TenNv }).SingleAsync(Lifetime.Token);
        var rows = await StayService.ActiveDetails(db).AsNoTracking().Where(c => c.MaPt == _rentalId)
            .OrderBy(c => c.MaCtdp)
            .Select(c => new StayRow(c.MaCtdp, c.MaPh, c.CheckIn, c.CheckOut, c.TrangThai, c.ThanhTien ?? 0m))
            .ToListAsync(Lifetime.Token);
        if (IsDisposed) return;
        _summary.Text = $"Phiếu {_rentalId}   •   Ngày lập {header.NgPt:dd/MM/yyyy HH:mm}\nKhách: {header.TenKh}   •   Nhân viên: {header.TenNv}";
        Grid.Rows.Clear();
        foreach (var row in rows)
        {
            Row(Properties.Resources.Phong, row.Id, row.Room, row.Start, row.End, row.Status, row.Amount);
            Grid.Rows[^1].Tag = row;
        }
    }

    protected override Form CreateEditor(string? id) => new FormDanhSachDichVuPhongDaDung(id!);

    private sealed record StayRow(string Id, string Room, DateTime Start, DateTime End, string Status, decimal Amount);
}
