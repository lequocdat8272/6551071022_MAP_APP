using HotelManagement.Common;
using HotelManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.ChiTietTienNghi;
public abstract class FormChiTietTienNghi : FormNhapLieu
{
    private readonly string _roomType;
    private readonly string? _key;
    private readonly ComboBox _amenity;
    private readonly NumericUpDown _quantity;
    private sealed record AmenityOption(string Id, string Name);
    protected FormChiTietTienNghi(string roomType, string? amenity) : base(amenity == null ? "Thêm tiện nghi cho loại phòng" : "Cập nhật số lượng tiện nghi")
    {
        _roomType = roomType; _key = amenity;
        ClientSize = new Size(600, 300);
        _amenity = Choice("Tiện nghi"); _amenity.Enabled = amenity == null;
        _quantity = Field("Số lượng", new NumericUpDown { Minimum = 0, Maximum = 9999, Value = 1 });
    }
    protected override async Task LoadDataAsync()
    {
        await using var db = new HotelManagementContext();
        if (!await db.LoaiPhongs.AnyAsync(x => x.MaLph == _roomType)) throw new InvalidOperationException("Không tìm thấy loại phòng.");
        var options = await db.TienNghis.AsNoTracking().Where(x => x.DaXoa != true && (_key != null ? x.MaTn == _key
            : !x.Cttns.Any(c => c.MaLph == _roomType && c.DaXoa != true)))
            .OrderBy(x => x.TenTn).Select(x => new AmenityOption(x.MaTn, x.TenTn)).ToListAsync();
        if (options.Count == 0) throw new InvalidOperationException("Không còn tiện nghi để thêm hoặc tiện nghi đã bị xóa.");
        _amenity.DisplayMember = nameof(AmenityOption.Name); _amenity.ValueMember = nameof(AmenityOption.Id);
        _amenity.DataSource = options;
        if (_key != null)
        {
            var item = await db.Cttns.AsNoTracking().SingleOrDefaultAsync(x => x.MaLph == _roomType && x.MaTn == _key && x.DaXoa != true)
                ?? throw new InvalidOperationException("Không tìm thấy chi tiết tiện nghi.");
            _quantity.Value = Math.Clamp(item.Sl ?? 0, 0, 9999);
        }
    }
    protected override async Task SaveDataAsync()
    {
        string id = _amenity.SelectedValue?.ToString() ?? throw new ArgumentException("Vui lòng chọn tiện nghi.");
        await using var db = new HotelManagementContext();
        if (!await db.TienNghis.AnyAsync(x => x.MaTn == id && x.DaXoa != true)) throw new InvalidOperationException("Tiện nghi không còn tồn tại.");
        var item = await db.Cttns.SingleOrDefaultAsync(x => x.MaLph == _roomType && x.MaTn == id);
        if (_key != null && (item == null || item.DaXoa == true)) throw new InvalidOperationException("Chi tiết tiện nghi không còn tồn tại.");
        if (_key == null && item != null && item.DaXoa != true) throw new ArgumentException("Tiện nghi đã được thêm vào loại phòng này.");
        if (item == null) { item = new Cttn { MaLph = _roomType, MaTn = id }; db.Cttns.Add(item); }
        item.Sl = (int)_quantity.Value; item.DaXoa = false;
        await db.SaveChangesAsync();
    }
}
