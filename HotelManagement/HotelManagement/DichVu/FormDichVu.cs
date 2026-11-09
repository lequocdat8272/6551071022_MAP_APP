using HotelManagement.Common;
using HotelManagement.Models;
using HotelManagement.Services;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.DichVu;

public abstract class FormDichVu : FormNhapLieu
{
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public string MaDichVu { get; set; } = "";
    private readonly bool _edit;
    private readonly TextBox _id, _name, _quantity;
    private readonly NumericUpDown _price;
    private readonly ComboBox _type;
    protected FormDichVu(bool edit) : base(edit ? "Cập nhật dịch vụ" : "Thêm mới dịch vụ")
    {
        _edit = edit;
        _id = TextField("Mã dịch vụ", 5); _id.ReadOnly = edit;
        _name = TextField("Tên dịch vụ", 20);
        _price = Field("Đơn giá (VNĐ)", new NumericUpDown { Maximum = 922337203685477m, ThousandsSeparator = true, DecimalPlaces = 2 });
        _quantity = TextField("Số lượng (trống: vô hạn)", 10);
        _type = Choice("Loại dịch vụ", "Giặt ủi", "Đưa đón", "Thuê xe máy", "Vé máy bay, du lịch", "Trông trẻ", "Bơi 4 mùa", "Karaoke", "Phục vụ phòng", "Thu đổi ngoại tệ", "Nhà hàng", "Hội họp, văn phòng", "Spa");
        _type.DropDownStyle = ComboBoxStyle.DropDown;
        _type.MaxLength = 20;
    }
    protected override async Task LoadDataAsync()
    {
        await using var db = new HotelManagementContext();
        var types = await db.DichVus.AsNoTracking().Select(x => x.LoaiDv).Distinct().ToListAsync();
        foreach (string type in types) if (!_type.Items.Contains(type)) _type.Items.Add(type);
        if (!_edit) return;
        var x = await db.DichVus.AsNoTracking().SingleOrDefaultAsync(x => x.MaDv == MaDichVu && x.DaXoa != true)
            ?? throw new InvalidOperationException("Không tìm thấy dịch vụ.");
        _id.Text = x.MaDv; _name.Text = x.TenDv; _price.Value = x.DonGia;
        _quantity.Text = x.SlconLai is null or < 0 ? "" : x.SlconLai.ToString();
        _type.Text = x.LoaiDv;
    }
    protected override async Task SaveDataAsync()
    {
        string id = Lab2Input.Id(_id.Text, "Mã dịch vụ");
        string name = Lab2Input.Required(_name.Text, "Tên dịch vụ", 20);
        string type = Lab2Input.Required(_type.Text, "Loại dịch vụ", 20);
        if (_price.Value <= 0) throw new ArgumentException("Đơn giá phải lớn hơn 0.");
        int quantity = -1;
        if (_quantity.Text.Trim().Length > 0 && (!int.TryParse(_quantity.Text.Trim(), out quantity) || quantity < 0))
            throw new ArgumentException("Số lượng phải là số nguyên không âm; để trống nếu không giới hạn.");
        await using var db = new HotelManagementContext();
        var x = await db.DichVus.SingleOrDefaultAsync(x => x.MaDv == id);
        if (_edit && (x == null || x.DaXoa == true)) throw new InvalidOperationException("Dịch vụ không còn tồn tại.");
        if (!_edit && x != null)
        {
            if (x.DaXoa != true) throw new ArgumentException("Mã dịch vụ đã tồn tại.");
            if (MessageBox.Show(this, "Mã này thuộc dịch vụ đã xóa. Khôi phục với thông tin vừa nhập?", "Khôi phục",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) throw new ArgumentException("Chưa khôi phục dịch vụ. Bạn có thể nhập mã khác.");
        }
        if (x == null) { x = new Models.DichVu { MaDv = id }; db.DichVus.Add(x); }
        x.TenDv = name; x.DonGia = _price.Value; x.LoaiDv = type; x.SlconLai = quantity; x.DaXoa = false;
        await db.SaveChangesAsync();
    }
}
