using System.Data;
using HotelManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Services;

public sealed record BookingCustomer(string Name, string Passport, string Phone, string Nationality, string Gender);
public sealed record BookingRoom(string RoomId, int Guests, DateTime CheckIn, DateTime CheckOut);
public sealed record BookingRequest(string EmployeeId, BookingCustomer Customer, IReadOnlyList<BookingRoom> Rooms);

public sealed class BookingService
{
    private readonly Func<HotelManagementContext> _createContext;
    public BookingService(Func<HotelManagementContext>? createContext = null) => _createContext = createContext ?? (() => new HotelManagementContext());

    public static void ValidateRooms(IReadOnlyList<BookingRoom> rooms)
    {
        if (rooms.Count == 0) throw new ArgumentException("Vui lòng chọn ít nhất một phòng.");
        if (rooms.Select(x => x.RoomId).Distinct(StringComparer.OrdinalIgnoreCase).Count() != rooms.Count)
            throw new ArgumentException("Mỗi phòng chỉ được chọn một lần trong phiếu thuê.");
        foreach (var room in rooms)
        {
            Lab2Input.Required(room.RoomId, "Mã phòng", 5);
            if (room.Guests < 1) throw new ArgumentException("Số người phải lớn hơn 0.");
            if (room.CheckIn.Date < DateTime.Today) throw new ArgumentException("Ngày nhận phòng không được trước hôm nay.");
            if (room.CheckOut.Date <= room.CheckIn.Date) throw new ArgumentException("Ngày trả phòng phải sau ngày nhận phòng.");
            if (room.CheckOut > new DateTime(2079, 6, 6, 23, 59, 0)) throw new ArgumentException("Ngày trả phòng vượt giới hạn SMALLDATETIME (06/06/2079).");
            if (room.CheckIn.Second != 0 || room.CheckOut.Second != 0 || room.CheckIn.Millisecond != 0 || room.CheckOut.Millisecond != 0)
                throw new ArgumentException("Giờ đặt phòng phải có độ chính xác theo phút.");
        }
    }

    public async Task<string> SaveAsync(BookingRequest request, CancellationToken cancellation = default)
    {
        ValidateRooms(request.Rooms);
        var input = request.Customer;
        Lab2Input.Customer(input.Name, input.Passport, input.Phone, input.Nationality, input.Gender, phoneRequired: true);
        await using var db = _createContext();
        // Khóa phạm vi đọc để hai lượt đặt không cùng chấp nhận một khoảng thời gian.
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellation);
        if (!await db.NhanViens.AnyAsync(x => x.MaNv == request.EmployeeId && x.DaXoa != true, cancellation))
            throw new InvalidOperationException("Nhân viên đăng nhập không hợp lệ. Vui lòng đăng nhập lại.");

        // Kiểm tra tất cả các phòng trước khi ghi khách hàng/phiếu thuê.
        var selected = new List<Models.Phong>();
        foreach (var room in request.Rooms.OrderBy(x => x.RoomId, StringComparer.Ordinal))
        {
            var entity = await db.Phongs.Include(x => x.MaLphNavigation).Where(x => x.MaPh == room.RoomId)
                .AvailableDuring(room.CheckIn, room.CheckOut).SingleOrDefaultAsync(cancellation)
                ?? throw new InvalidOperationException($"Phòng {room.RoomId} không còn trống trong khoảng thời gian đã chọn.");
            if (room.Guests > entity.MaLphNavigation.SoNguoiToiDa)
                throw new ArgumentException($"Phòng {room.RoomId} chỉ cho phép tối đa {entity.MaLphNavigation.SoNguoiToiDa} người.");
            selected.Add(entity);
        }

        var customer = await db.KhachHangs.SingleOrDefaultAsync(x => x.CccdPassport == input.Passport.Trim(), cancellation);
        if (customer == null)
        {
            customer = new Models.KhachHang
            {
                MaKh = Lab2Input.NextId(await db.KhachHangs.Select(x => x.MaKh).ToListAsync(cancellation), "KH", 3, 5),
                CccdPassport = input.Passport.Trim()
            };
            db.KhachHangs.Add(customer);
        }
        if (db.Entry(customer).State == EntityState.Added || customer.DaXoa == true)
        {
            customer.TenKh = input.Name.Trim(); customer.Sdt = input.Phone.Trim();
            customer.QuocTich = input.Nationality.Trim(); customer.GioiTinh = input.Gender; customer.DaXoa = false;
        }
        string id = Lab2Input.NextId(await db.PhieuThues.Select(x => x.MaPt).ToListAsync(cancellation), "PT", 3, 5);
        db.PhieuThues.Add(new Models.PhieuThue
        {
            MaPt = id, MaKh = customer.MaKh, MaNv = request.EmployeeId,
            NgPt = DateTime.Now, DaXoa = false
        });
        await db.SaveChangesAsync(cancellation);
        var detailIds = await db.Ctdps.Select(x => x.MaCtdp).ToListAsync(cancellation);
        foreach (var room in request.Rooms)
        {
            var entity = selected.Single(x => x.MaPh == room.RoomId);
            string detailId = Lab2Input.NextId(detailIds, "CTDP", 3, 7);
            detailIds.Add(detailId);
            db.Ctdps.Add(new Ctdp
            {
                MaCtdp = detailId, MaPt = id, MaPh = room.RoomId, SoNguoi = room.Guests,
                CheckIn = room.CheckIn, CheckOut = room.CheckOut, TrangThai = "Đã đặt",
                DonGia = entity.MaLphNavigation.GiaNgay,
                ThanhTien = Math.Max(1, (room.CheckOut.Date - room.CheckIn.Date).Days) * entity.MaLphNavigation.GiaNgay,
                DaXoa = false, TheoGio = false
            });
            // Trigger trong Database.sql xử lý từng bản ghi trong inserted.
            await db.SaveChangesAsync(cancellation);
            if (entity.Ttph != "Đang thuê") entity.Ttph = "Đã đặt";
        }
        await db.SaveChangesAsync(cancellation);
        await transaction.CommitAsync(cancellation);
        return id;
    }
}
