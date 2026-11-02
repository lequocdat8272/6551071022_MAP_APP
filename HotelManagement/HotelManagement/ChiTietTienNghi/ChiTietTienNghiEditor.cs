using HotelManagement.Controls;
using HotelManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.ChiTietTienNghi;

public abstract class ChiTietTienNghiEditor : NhapLieuForm
{
    private readonly string _roomTypeId;
    private readonly string? _amenityId;
    private readonly ComboBox _amenity;
    private readonly TextBox _quantity;
    private readonly Label _roomType = new();

    protected ChiTietTienNghiEditor(string roomTypeId, string? amenityId) : base(amenityId == null ? "Thêm tiện nghi cho loại phòng" : "Cập nhật số lượng tiện nghi")
    {
        _roomTypeId = roomTypeId; _amenityId = amenityId;
        ClientSize = new Size(650, 360);
        _roomType.AutoSize = true;
        Field("Loại phòng", _roomType);
        _amenity = ChoiceField("Tiện nghi", "cbTenTN");
        _amenity.Enabled = amenityId == null;
        _quantity = TextField("Số lượng", "txtSoLuong", 10);
        _quantity.Text = "1";
    }

    protected override async Task LoadDataAsync()
    {
        await using var db = new HotelManagementContext();
        _roomType.Text = await db.LoaiPhongs.Where(l => l.MaLph == _roomTypeId).Select(l => l.TenLph).FirstOrDefaultAsync(Lifetime.Token)
            ?? throw new InvalidOperationException("Không tìm thấy loại phòng.");
        var query = db.TienNghis.AsNoTracking().Where(t => t.DaXoa != true);
        query = _amenityId == null
            ? query.Where(t => !t.Cttns.Any(c => c.MaLph == _roomTypeId && c.DaXoa != true))
            : query.Where(t => t.MaTn == _amenityId);
        var items = await query.OrderBy(t => t.MaTn).ToListAsync(Lifetime.Token);
        _amenity.DisplayMember = nameof(Models.TienNghi.TenTn);
        _amenity.ValueMember = nameof(Models.TienNghi.MaTn);
        _amenity.DataSource = items;
        if (items.Count == 0) throw new InvalidOperationException("Không còn tiện nghi có thể thêm.");
        if (_amenityId != null)
        {
            var c = await db.Cttns.AsNoTracking().FirstOrDefaultAsync(c => c.MaLph == _roomTypeId && c.MaTn == _amenityId && c.DaXoa != true, Lifetime.Token)
                ?? throw new InvalidOperationException("Không tìm thấy chi tiết tiện nghi.");
            _quantity.Text = c.Sl?.ToString() ?? "1";
        }
    }

    protected override async Task SaveDataAsync()
    {
        if (_amenity.SelectedValue is not string id) throw new InvalidOperationException("Vui lòng chọn tiện nghi.");
        if (!int.TryParse(_quantity.Text.Trim(), out int quantity) || quantity <= 0)
            throw new InvalidOperationException("Số lượng phải là số nguyên lớn hơn 0.");
        await using var db = new HotelManagementContext();
        if (!await db.TienNghis.AnyAsync(t => t.MaTn == id && t.DaXoa != true, Lifetime.Token))
            throw new InvalidOperationException("Tiện nghi đã bị xóa.");
        var c = await db.Cttns.FirstOrDefaultAsync(c => c.MaLph == _roomTypeId && c.MaTn == id, Lifetime.Token);
        if (_amenityId == null && c is { DaXoa: not true }) throw new InvalidOperationException("Tiện nghi đã có trong loại phòng này.");
        if (_amenityId != null && (c == null || c.DaXoa == true)) throw new InvalidOperationException("Chi tiết tiện nghi đã bị xóa.");
        if (c == null) { c = new Cttn { MaLph = _roomTypeId, MaTn = id }; db.Cttns.Add(c); }
        c.Sl = quantity; c.DaXoa = false;
        await db.SaveChangesAsync(Lifetime.Token);
    }
}
