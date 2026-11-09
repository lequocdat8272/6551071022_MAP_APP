using HotelManagement.Common;
using HotelManagement.Models;
using HotelManagement.Services;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.TienNghi;
public abstract class FormTienNghi : FormNhapLieu
{
    private readonly string? _key;
    private readonly TextBox _id, _name;
    protected FormTienNghi(string? id) : base(id == null ? "Thêm mới tiện nghi" : "Cập nhật tiện nghi")
    {
        _key = id;
        ClientSize = new Size(600, 300);
        _id = TextField("Mã tiện nghi", 5); _id.ReadOnly = id != null;
        _name = TextField("Tên tiện nghi", 50);
    }
    protected override async Task LoadDataAsync()
    {
        if (_key == null) return;
        await using var db = new HotelManagementContext();
        var x = await db.TienNghis.AsNoTracking().SingleOrDefaultAsync(x => x.MaTn == _key && x.DaXoa != true)
            ?? throw new InvalidOperationException("Không tìm thấy tiện nghi.");
        _id.Text = x.MaTn; _name.Text = x.TenTn;
    }
    protected override async Task SaveDataAsync()
    {
        string id = Lab2Input.Id(_id.Text, "Mã tiện nghi");
        string name = Lab2Input.Required(_name.Text, "Tên tiện nghi", 50);
        await using var db = new HotelManagementContext();
        var x = await db.TienNghis.SingleOrDefaultAsync(x => x.MaTn == id);
        if (_key == null && x != null) throw new ArgumentException("Mã tiện nghi đã tồn tại (kể cả tiện nghi đã xóa).");
        if (_key != null && (x == null || x.DaXoa == true)) throw new InvalidOperationException("Tiện nghi không còn tồn tại.");
        if (x == null) { x = new Models.TienNghi { MaTn = id }; db.TienNghis.Add(x); }
        x.TenTn = name; x.DaXoa = false;
        await db.SaveChangesAsync();
    }
}
