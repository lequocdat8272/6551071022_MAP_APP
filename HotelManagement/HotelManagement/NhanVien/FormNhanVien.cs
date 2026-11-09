using HotelManagement.Common;
using HotelManagement.Models;
using HotelManagement.Services;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.NhanVien;
public abstract class FormNhanVien : FormNhapLieu
{
    private readonly string? _key;
    private readonly TextBox _id, _name, _phone, _identity, _address, _email;
    private readonly ComboBox _role, _gender;
    private readonly NumericUpDown _salary;
    private readonly DateTimePicker _birth;
    protected FormNhanVien(string? id) : base(id == null ? "Thêm mới nhân viên" : "Cập nhật nhân viên")
    {
        _key = id;
        ClientSize = new Size(650, 660);
        _id = TextField("Mã nhân viên", 5); _id.ReadOnly = id != null;
        _name = TextField("Họ tên", 50);
        _role = Choice("Chức vụ", "Quản lý", "Lễ tân", "Kế toán", "Bảo vệ", "Buồng phòng", "Nhân viên");
        _role.DropDownStyle = ComboBoxStyle.DropDown; _role.MaxLength = 50;
        _salary = Field("Lương (VNĐ)", new NumericUpDown { Maximum = 922337203685477m, ThousandsSeparator = true, DecimalPlaces = 2 });
        _phone = TextField("Số điện thoại", 10);
        _identity = TextField("CCCD", 12);
        _birth = Field("Ngày sinh", new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy", Value = DateTime.Today.AddYears(-18) });
        _gender = Choice("Giới tính", "Nam", "Nữ");
        _address = TextField("Địa chỉ", 100);
        _email = TextField("Email", 100);
    }
    protected override async Task LoadDataAsync()
    {
        if (_key == null) return;
        await using var db = new HotelManagementContext();
        var x = await db.NhanViens.AsNoTracking().SingleOrDefaultAsync(x => x.MaNv == _key && x.DaXoa != true)
            ?? throw new InvalidOperationException("Không tìm thấy nhân viên.");
        _id.Text = x.MaNv; _name.Text = x.TenNv; SelectValue(_role, x.ChucVu); _salary.Value = x.Luong;
        _phone.Text = x.Sdt; _identity.Text = x.Cccd; _birth.Value = x.NgaySinh.ToDateTime(TimeOnly.MinValue);
        SelectValue(_gender, x.GioiTinh); _address.Text = x.DiaChi; _email.Text = x.Email;
    }
    protected override async Task SaveDataAsync()
    {
        string id = Lab2Input.Id(_id.Text, "Mã nhân viên");
        string name = _name.Text.Trim(), role = _role.Text.Trim(), phone = _phone.Text.Trim(), identity = _identity.Text.Trim(),
            address = _address.Text.Trim(), email = _email.Text.Trim(), gender = _gender.Text;
        Lab2Input.Employee(name, role, _salary.Value, phone, identity, _birth.Value, gender, address, email);
        await using var db = new HotelManagementContext();
        var x = await db.NhanViens.SingleOrDefaultAsync(x => x.MaNv == id);
        if (_key == null && x != null) throw new ArgumentException("Mã nhân viên đã tồn tại (kể cả nhân viên đã xóa).");
        if (_key != null && (x == null || x.DaXoa == true)) throw new InvalidOperationException("Nhân viên không còn tồn tại.");
        if (await db.NhanViens.AnyAsync(x => x.MaNv != id && x.Sdt == phone)) throw new ArgumentException("Số điện thoại đã tồn tại.");
        if (await db.NhanViens.AnyAsync(x => x.MaNv != id && x.Cccd == identity)) throw new ArgumentException("CCCD đã tồn tại.");
        if (await db.NhanViens.AnyAsync(x => x.MaNv != id && x.Email == email)) throw new ArgumentException("Email đã tồn tại.");
        if (x == null) { x = new Models.NhanVien { MaNv = id }; db.NhanViens.Add(x); }
        x.TenNv = name; x.ChucVu = role; x.Luong = _salary.Value; x.Sdt = phone; x.Cccd = identity;
        x.NgaySinh = DateOnly.FromDateTime(_birth.Value); x.GioiTinh = gender; x.DiaChi = address; x.Email = email; x.DaXoa = false;
        await db.SaveChangesAsync();
    }
}
