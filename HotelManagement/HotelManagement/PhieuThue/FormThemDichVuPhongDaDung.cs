using HotelManagement.Controls;
using HotelManagement.Models;
using HotelManagement.Services;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.PhieuThue;

public sealed class FormThemDichVuPhongDaDung : NhapLieuForm
{
    private readonly string _detailId;
    private readonly ComboBox _services;
    private readonly NumericUpDown _quantity = new() { Minimum = 1, Maximum = 999999, Value = 1 };
    private readonly Label _price = new() { AutoSize = true };
    private readonly Label _stock = new() { AutoSize = true };
    private readonly Label _total = new() { AutoSize = true };

    public FormThemDichVuPhongDaDung(string detailId) : base("Thêm dịch vụ cho phòng")
    {
        _detailId = detailId;
        TextField("Mã chi tiết", "MaCTDP", 7, true).Text = detailId;
        _services = ChoiceField("Dịch vụ", "MaDV");
        Field("Đơn giá", _price);
        Field("Còn lại", _stock);
        Field("Số lượng", _quantity);
        Field("Thành tiền", _total);
        _services.SelectedIndexChanged += (_, _) => UpdateAmount();
        _quantity.ValueChanged += (_, _) => UpdateAmount();
        SaveButton.Text = "Thêm dịch vụ";
    }

    protected override async Task LoadDataAsync()
    {
        await using var db = new HotelManagementContext();
        var services = await db.DichVus.AsNoTracking().Where(d => d.DaXoa != true).OrderBy(d => d.TenDv)
            .Select(d => new ServiceOption(d.MaDv, d.TenDv, d.DonGia, d.SlconLai)).ToListAsync(Lifetime.Token);
        _services.DisplayMember = nameof(ServiceOption.Name);
        _services.DataSource = services;
        UpdateAmount();
    }

    private void UpdateAmount()
    {
        var service = _services.SelectedItem as ServiceOption;
        _price.Text = service == null ? "—" : $"{service.Price:N0} VNĐ";
        _stock.Text = service == null ? "—" : service.Stock is >= 0 ? service.Stock.Value.ToString() : "Không giới hạn";
        _total.Text = service == null ? "—" : $"{service.Price * _quantity.Value:N0} VNĐ";
    }

    protected override Task SaveDataAsync()
    {
        if (_services.SelectedItem is not ServiceOption service) throw new InvalidOperationException("Hãy chọn dịch vụ.");
        return StayService.AddServiceAsync(_detailId, service.Id, (int)_quantity.Value, Lifetime.Token);
    }

    private sealed record ServiceOption(string Id, string Name, decimal Price, int? Stock);
}
