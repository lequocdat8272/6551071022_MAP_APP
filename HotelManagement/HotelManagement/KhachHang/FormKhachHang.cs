using HotelManagement.Common;
using HotelManagement.Models;
using HotelManagement.Services;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.KhachHang;
public abstract class FormKhachHang : FormNhapLieu
{
    private readonly string? _key;
    private readonly TextBox _id, _name, _passport, _phone, _nationality;
    private readonly ComboBox _gender;
    protected FormKhachHang(string? id) : base(id == null ? "Thêm mới khách hàng" : "Cập nhật khách hàng")
    {
        _key = id;
        _id = TextField("Mã khách hàng", 5); _id.ReadOnly = true;
        _name = TextField("Họ tên", 50);
        _passport = TextField("CCCD/Passport", 12);
        _phone = TextField("Số điện thoại", 10);
        _nationality = TextField("Quốc tịch", 30);
        _gender = Choice("Giới tính", "Nam", "Nữ");
    }
    protected override async Task LoadDataAsync()
    {
        await using var db = new HotelManagementContext();
        if (_key == null)
        {
            _id.Text = Lab2Input.NextId(await db.KhachHangs.Select(x => x.MaKh).ToListAsync(), "KH", 3, 5);
            return;
        }
        var x = await db.KhachHangs.AsNoTracking().SingleOrDefaultAsync(x => x.MaKh == _key && x.DaXoa != true)
            ?? throw new InvalidOperationException("Không tìm thấy khách hàng.");
        _id.Text = x.MaKh; _name.Text = x.TenKh; _passport.Text = x.CccdPassport;
        _phone.Text = x.Sdt; _nationality.Text = x.QuocTich; SelectValue(_gender, x.GioiTinh);
    }
    protected override async Task SaveDataAsync()
    {
        string id = Lab2Input.Id(_id.Text, "Mã khách hàng");
        string name = _name.Text.Trim(), passport = _passport.Text.Trim(), phone = _phone.Text.Trim(), nationality = _nationality.Text.Trim(), gender = _gender.Text;
        Lab2Input.Customer(name, passport, phone, nationality, gender);
        await using var db = new HotelManagementContext();
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        if (_key == null) id = Lab2Input.NextId(await db.KhachHangs.Select(x => x.MaKh).ToListAsync(), "KH", 3, 5);
        var x = await db.KhachHangs.SingleOrDefaultAsync(x => x.MaKh == id);
        if (_key == null && x != null) throw new ArgumentException("Mã khách hàng đã tồn tại (kể cả khách hàng đã xóa).");
        if (_key != null && (x == null || x.DaXoa == true)) throw new InvalidOperationException("Khách hàng không còn tồn tại.");
        if (await db.KhachHangs.AnyAsync(x => x.MaKh != id && x.CccdPassport == passport)) throw new ArgumentException("CCCD/Passport đã tồn tại.");
        if (x == null) { x = new Models.KhachHang { MaKh = id }; db.KhachHangs.Add(x); }
        x.TenKh = name; x.CccdPassport = passport; x.Sdt = phone.Length == 0 ? null : phone; x.QuocTich = nationality; x.GioiTinh = gender; x.DaXoa = false;
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }
}
