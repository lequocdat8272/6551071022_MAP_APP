using HotelManagement.Controls;
using HotelManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.TienNghi;

public abstract class TienNghiEditor : NhapLieuForm
{
    private readonly string? _recordId;
    private readonly TextBox _id, _name;

    protected TienNghiEditor(string? id) : base(id == null ? "Thêm tiện nghi" : "Cập nhật tiện nghi")
    {
        _recordId = id;
        ClientSize = new Size(600, 300);
        _id = TextField("Mã tiện nghi", "txtMaTN", 5, id != null);
        _id.CharacterCasing = CharacterCasing.Upper;
        _name = TextField("Tên tiện nghi", "txtTenTN", 50);
    }

    protected override async Task LoadDataAsync()
    {
        if (_recordId == null) return;
        await using var db = new HotelManagementContext();
        var t = await db.TienNghis.AsNoTracking().FirstOrDefaultAsync(t => t.MaTn == _recordId && t.DaXoa != true, Lifetime.Token)
            ?? throw new InvalidOperationException("Không tìm thấy tiện nghi.");
        _id.Text = t.MaTn; _name.Text = t.TenTn;
    }

    protected override async Task SaveDataAsync()
    {
        string id = Required(_id, "Mã tiện nghi", 5).ToUpperInvariant();
        string name = Required(_name, "Tên tiện nghi", 50);
        await using var db = new HotelManagementContext();
        if (await db.TienNghis.AnyAsync(t => t.MaTn != id && t.DaXoa != true && t.TenTn == name, Lifetime.Token))
            throw new InvalidOperationException("Tên tiện nghi đã tồn tại.");
        Models.TienNghi entity;
        if (_recordId == null)
        {
            if (await db.TienNghis.AnyAsync(t => t.MaTn == id, Lifetime.Token)) throw new InvalidOperationException("Mã tiện nghi đã tồn tại.");
            entity = new Models.TienNghi { MaTn = id, DaXoa = false };
            db.TienNghis.Add(entity);
        }
        else entity = await db.TienNghis.FirstOrDefaultAsync(t => t.MaTn == id && t.DaXoa != true, Lifetime.Token)
                ?? throw new InvalidOperationException("Tiện nghi đã bị xóa.");
        entity.TenTn = name;
        await db.SaveChangesAsync(Lifetime.Token);
    }
}
