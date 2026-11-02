using HotelManagement.Controls;
using HotelManagement.Models;
using HotelManagement.Services;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using System.Globalization;

namespace HotelManagement.NhanVien;

public abstract class NhanVienEditor : NhapLieuForm
{
    private readonly string? _recordId;
    private readonly TextBox _id, _name, _salary, _phone, _identity, _address, _email;
    private readonly ComboBox _role, _gender;
    private readonly DateTimePicker _birthday = new();

    protected NhanVienEditor(string? id) : base(id == null ? "Thêm nhân viên" : "Cập nhật nhân viên")
    {
        _recordId = id;
        ClientSize = new Size(720, 740);
        _id = TextField("Mã nhân viên", "txtMaNV", 5, id != null);
        _id.CharacterCasing = CharacterCasing.Upper;
        _name = TextField("Họ và tên", "txtHoTenNV", 50);
        _role = ChoiceField("Chức vụ", "cbChucVu", "Quản lý", "Tiếp tân", "Bảo vệ", "Nhân viên vệ sinh", "Kế toán", "Nhân viên dịch vụ");
        _salary = TextField("Lương (dấu chấm thập phân)", "txtLuong", 20);
        _phone = TextField("Điện thoại", "txtSoDT", 10);
        _identity = TextField("CCCD", "txtSoCCCD", 12);
        _birthday.Name = "dtNgaySinh";
        _birthday.Format = DateTimePickerFormat.Custom;
        _birthday.CustomFormat = "dd/MM/yyyy";
        _birthday.MaxDate = DateTime.Today;
        _birthday.Value = DateTime.Today.AddYears(-18);
        Field("Ngày sinh", _birthday);
        _gender = ChoiceField("Giới tính", "cbGioiTinh", "Nam", "Nữ", "Khác");
        _gender.SelectedIndex = 0;
        _address = TextField("Địa chỉ", "txtDiaChi", 100);
        _email = TextField("Email", "txtEmail", 100);
    }

    protected override async Task LoadDataAsync()
    {
        if (_recordId == null) return;
        await using var db = new HotelManagementContext();
        var n = await db.NhanViens.AsNoTracking().FirstOrDefaultAsync(n => n.MaNv == _recordId && n.DaXoa != true, Lifetime.Token)
            ?? throw new InvalidOperationException("Không tìm thấy nhân viên.");
        _id.Text = n.MaNv; _name.Text = n.TenNv;
        if (!_role.Items.Contains(n.ChucVu)) _role.Items.Add(n.ChucVu);
        _role.Text = n.ChucVu; _salary.Text = n.Luong.ToString(CultureInfo.InvariantCulture);
        _phone.Text = n.Sdt; _identity.Text = n.Cccd; _birthday.Value = n.NgaySinh.ToDateTime(TimeOnly.MinValue);
        _gender.Text = n.GioiTinh; _address.Text = n.DiaChi; _email.Text = n.Email;
    }

    protected override async Task SaveDataAsync()
    {
        string id = Required(_id, "Mã nhân viên", 5).ToUpperInvariant(), name = Required(_name, "Họ và tên", 50);
        string role = Required(_role, "Chức vụ", 50), gender = Required(_gender, "Giới tính", 10);
        string address = Required(_address, "Địa chỉ", 100), email = Required(_email, "Email", 100);
        string phone = _phone.Text.Trim(), identity = _identity.Text.Trim();
        Lab2Input.Phone(phone);
        if (!Lab2Input.Digits(identity) || identity.Length != 12) throw new InvalidOperationException("CCCD phải gồm đúng 12 chữ số.");
        if (!MailAddress.TryCreate(email, out var parsed) || parsed.Address != email || !parsed.Host.Contains('.'))
            throw new InvalidOperationException("Email không đúng định dạng.");
        if (!RoomTypeInput.TryPrice(_salary.Text, out var salary)) throw new InvalidOperationException("Lương phải lớn hơn 0 và trong giới hạn MONEY, tối đa 4 số lẻ.");
        if (_birthday.Value.Date > DateTime.Today.AddYears(-18)) throw new InvalidOperationException("Nhân viên phải đủ 18 tuổi.");
        await using var db = new HotelManagementContext();
        if (await db.NhanViens.AnyAsync(n => n.MaNv != _recordId && (n.Sdt == phone || n.Cccd == identity || n.Email == email), Lifetime.Token))
            throw new InvalidOperationException("Điện thoại, CCCD hoặc email đã tồn tại (kể cả nhân viên đã xóa).");
        Models.NhanVien entity;
        if (_recordId == null)
        {
            if (await db.NhanViens.AnyAsync(n => n.MaNv == id, Lifetime.Token)) throw new InvalidOperationException("Mã nhân viên đã tồn tại.");
            entity = new Models.NhanVien { MaNv = id, DaXoa = false }; db.NhanViens.Add(entity);
        }
        else entity = await db.NhanViens.FirstOrDefaultAsync(n => n.MaNv == _recordId && n.DaXoa != true, Lifetime.Token)
                ?? throw new InvalidOperationException("Nhân viên đã bị xóa.");
        entity.TenNv = name; entity.ChucVu = role; entity.Luong = salary; entity.Sdt = phone; entity.Cccd = identity;
        entity.NgaySinh = DateOnly.FromDateTime(_birthday.Value); entity.GioiTinh = gender; entity.DiaChi = address; entity.Email = email;
        await db.SaveChangesAsync(Lifetime.Token);
        if (id == DangNhap.FormDangNhap.MaNV_DangNhap) DangNhap.FormDangNhap.TenNV_DangNhap = name;
    }
}
