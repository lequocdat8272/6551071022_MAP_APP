using HotelManagement.Controls;
using HotelManagement.Models;
using HotelManagement.Services;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace HotelManagement.KhachHang;

public abstract class KhachHangEditor : NhapLieuForm
{
    private readonly string? _recordId;
    private readonly TextBox _id, _name, _identity, _phone, _nationality;
    private readonly ComboBox _gender;

    protected KhachHangEditor(string? id) : base(id == null ? "Thêm khách hàng" : "Cập nhật khách hàng")
    {
        _recordId = id;
        _id = TextField("Mã khách hàng", "txtMaKH", 5, true);
        _name = TextField("Họ và tên", "txtHoVaTen", 50);
        _identity = TextField("CCCD/Passport", "txtCCCD", 12);
        _phone = TextField("Số điện thoại", "txtSoDienThoai", 10);
        _nationality = TextField("Quốc tịch", "txtQuocTich", 30);
        _gender = ChoiceField("Giới tính", "cbGioiTinh", "Nam", "Nữ", "Khác");
        _gender.SelectedIndex = 0;
    }

    protected override async Task LoadDataAsync()
    {
        await using var db = new HotelManagementContext();
        if (_recordId == null)
        {
            _id.Text = Lab2Input.NextId(await db.KhachHangs.Select(k => k.MaKh).ToListAsync(Lifetime.Token), "KH", 3, 5);
            return;
        }
        var k = await db.KhachHangs.AsNoTracking().FirstOrDefaultAsync(k => k.MaKh == _recordId && k.DaXoa != true, Lifetime.Token)
            ?? throw new InvalidOperationException("Không tìm thấy khách hàng.");
        _id.Text = k.MaKh; _name.Text = k.TenKh; _identity.Text = k.CccdPassport;
        _phone.Text = k.Sdt; _nationality.Text = k.QuocTich; _gender.Text = k.GioiTinh;
    }

    protected override async Task SaveDataAsync()
    {
        string name = Required(_name, "Họ và tên", 50), identity = Required(_identity, "CCCD/Passport", 12);
        string nationality = Required(_nationality, "Quốc tịch", 30), gender = Required(_gender, "Giới tính", 10);
        string phone = _phone.Text.Trim();
        Lab2Input.Phone(phone, false);
        await using var db = new HotelManagementContext();
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, Lifetime.Token);
        if (await db.KhachHangs.AnyAsync(k => k.CccdPassport == identity && k.MaKh != _recordId, Lifetime.Token))
            throw new InvalidOperationException("CCCD/Passport đã tồn tại.");
        Models.KhachHang entity;
        if (_recordId == null)
        {
            string id = Lab2Input.NextId(await db.KhachHangs.Select(k => k.MaKh).ToListAsync(Lifetime.Token), "KH", 3, 5);
            entity = new Models.KhachHang { MaKh = id, DaXoa = false }; db.KhachHangs.Add(entity);
        }
        else entity = await db.KhachHangs.FirstOrDefaultAsync(k => k.MaKh == _recordId && k.DaXoa != true, Lifetime.Token)
                ?? throw new InvalidOperationException("Khách hàng đã bị xóa.");
        entity.TenKh = name; entity.CccdPassport = identity; entity.Sdt = phone.Length == 0 ? null : phone;
        entity.QuocTich = nationality; entity.GioiTinh = gender;
        await db.SaveChangesAsync(Lifetime.Token);
        await transaction.CommitAsync(Lifetime.Token);
    }
}
