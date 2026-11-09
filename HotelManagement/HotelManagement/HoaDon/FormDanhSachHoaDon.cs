using HotelManagement.Controls;
using HotelManagement.Models;
using HotelManagement.Services;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.HoaDon;

public sealed class FormDanhSachHoaDon : DanhSachForm
{
    private readonly DateTimePicker _date = new()
    {
        Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy", ShowCheckBox = true,
        Checked = false, Width = 140, Margin = new Padding(4, 4, 3, 4)
    };

    public FormDanhSachHoaDon() : base("Danh sách hóa đơn", detailsOnly: true)
    {
        AddButton.Visible = false;
        Search.PlaceholderText = "Mã HĐ, tên khách, CCCD";
        Toolbar.Controls.Add(new Label { Text = "Ngày lập", AutoSize = true, Margin = new Padding(12, 8, 0, 0) });
        Toolbar.Controls.Add(_date);
        _date.ValueChanged += (_, _) => Filter();
        Columns(Properties.Resources.HoaDon, ("MaHD", "Mã hóa đơn"), ("Ngay", "Ngày lập"), ("Khach", "Khách hàng"),
            ("NV", "Nhân viên lập"), ("MaCTDP", "Mã chi tiết"), ("Tien", "Trị giá"), ("TrangThai", "Trạng thái"));
        Grid.Columns["Ngay"]!.DefaultCellStyle.Format = "dd/MM/yyyy";
        Grid.Columns["Tien"]!.DefaultCellStyle.Format = "#,##0.####";
        Grid.Columns["Tien"]!.DefaultCellStyle.FormatProvider = InvoiceData.Vietnamese;
    }

    protected override bool MatchesRow(DataGridViewRow row, string keyword)
    {
        if (_date.Checked && (row.Cells["Ngay"].Value is not DateTime date || date.Date != _date.Value.Date))
            return false;
        return base.MatchesRow(row, keyword) || (row.Tag is string identity && identity.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    protected override async Task LoadRowsAsync()
    {
        await using var db = new HotelManagementContext();
        var rows = await db.HoaDons.AsNoTracking().Where(h => h.DaXoa != true)
            .OrderByDescending(h => h.NgHd).ThenByDescending(h => h.MaHd)
            .Select(h => new
            {
                h.MaHd, h.NgHd, h.MaCtdp, h.TrangThai,
                Guest = h.MaCtdpNavigation.MaPtNavigation.MaKhNavigation.TenKh,
                Identity = h.MaCtdpNavigation.MaPtNavigation.MaKhNavigation.CccdPassport,
                Employee = h.MaNvNavigation != null ? h.MaNvNavigation.TenNv : "",
                Amount = (h.MaCtdpNavigation.ThanhTien ?? 0m) + h.MaCtdpNavigation.Ctdvs.Where(v => v.DaXoa != true)
                    .Sum(v => v.ThanhTien ?? v.Sl * (v.DonGia ?? v.MaDvNavigation.DonGia))
            }).ToListAsync(Lifetime.Token);
        if (IsDisposed) return;
        Grid.Rows.Clear();
        foreach (var row in rows)
        {
            Row(Properties.Resources.HoaDon, row.MaHd, row.NgHd, row.Guest, row.Employee, row.MaCtdp, row.Amount, row.TrangThai);
            Grid.Rows[^1].Tag = row.Identity;
        }
    }

    protected override Form CreateEditor(string? id) => new FormHoaDon(id!);
}
