using HotelManagement.Controls;
using HotelManagement.Models;
using HotelManagement.Services;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace HotelManagement.DichVu;

public abstract class DichVuEditor : NhapLieuForm
{
    protected string? RecordId;
    private readonly bool _editing;
    private readonly TextBox _id, _name, _price, _stock;
    private readonly ComboBox _type;

    protected DichVuEditor(bool editing) : base(editing ? "Cập nhật dịch vụ" : "Thêm dịch vụ")
    {
        _editing = editing;
        _id = TextField("Mã dịch vụ", "txtMaDV", 5, editing);
        _id.CharacterCasing = CharacterCasing.Upper;
        _name = TextField("Tên dịch vụ", "txtTenDV", 20);
        _price = TextField("Đơn giá (dấu chấm thập phân)", "txtDonGia", 20);
        _type = ChoiceField("Loại dịch vụ", "cbLoaiDV");
        _type.DropDownStyle = ComboBoxStyle.DropDown;
        _type.MaxLength = 20;
        _stock = TextField("Số lượng (trống: không giới hạn)", "txtSoLuongConLai", 10);
    }

    protected override async Task LoadDataAsync()
    {
        await using var db = new HotelManagementContext();
        var types = await db.DichVus.AsNoTracking().Select(d => d.LoaiDv).Distinct().ToListAsync(Lifetime.Token);
        _type.Items.AddRange(new[] { "Thức uống", "Đồ ăn", "Dịch vụ", "Giặt ủi", "Đưa đón", "Spa", "Nhà hàng" }
            .Concat(types).Distinct(StringComparer.OrdinalIgnoreCase).Cast<object>().ToArray());
        if (!_editing) return;
        var d = await db.DichVus.AsNoTracking().FirstOrDefaultAsync(d => d.MaDv == RecordId && d.DaXoa != true, Lifetime.Token)
            ?? throw new InvalidOperationException("Không tìm thấy dịch vụ.");
        _id.Text = d.MaDv; _name.Text = d.TenDv; _price.Text = d.DonGia.ToString(CultureInfo.InvariantCulture);
        _type.Text = d.LoaiDv; _stock.Text = d.SlconLai is null or < 0 ? "" : d.SlconLai.Value.ToString();
    }

    protected override async Task SaveDataAsync()
    {
        string id = Required(_id, "Mã dịch vụ", 5).ToUpperInvariant();
        string name = Required(_name, "Tên dịch vụ", 20);
        string type = Required(_type, "Loại dịch vụ", 20);
        if (!RoomTypeInput.TryPrice(_price.Text, out var price)) throw new InvalidOperationException("Đơn giá phải lớn hơn 0, tối đa 4 số lẻ và trong giới hạn MONEY.");
        int stock = -1;
        if (_stock.Text.Trim().Length > 0 && (!int.TryParse(_stock.Text.Trim(), out stock) || stock < 0))
            throw new InvalidOperationException("Số lượng phải là số nguyên không âm, hoặc để trống nếu không giới hạn.");
        await using var db = new HotelManagementContext();
        var entity = await db.DichVus.FirstOrDefaultAsync(d => d.MaDv == id, Lifetime.Token);
        if (_editing && (entity == null || entity.DaXoa == true)) throw new InvalidOperationException("Dịch vụ đã bị xóa.");
        if (!_editing && entity != null)
        {
            if (entity.DaXoa != true) throw new InvalidOperationException("Mã dịch vụ đã tồn tại.");
            if (MessageBox.Show(this, $"Khôi phục mã {id} với thông tin vừa nhập?", "Khôi phục dịch vụ", MessageBoxButtons.YesNo) != DialogResult.Yes)
                throw new InvalidOperationException("Chưa khôi phục. Vui lòng chọn mã khác.");
        }
        if (entity == null) { entity = new Models.DichVu { MaDv = id }; db.DichVus.Add(entity); }
        entity.TenDv = name; entity.DonGia = price; entity.LoaiDv = type; entity.SlconLai = stock; entity.DaXoa = false;
        await db.SaveChangesAsync(Lifetime.Token);
    }
}
