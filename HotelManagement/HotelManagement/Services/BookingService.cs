using HotelManagement.Models;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace HotelManagement.Services;

public sealed record BookingCustomer(string Name, string Identity, string Phone, string Nationality, string Gender);
public sealed record BookingRoom(string RoomId, int Guests, DateTime CheckIn, DateTime CheckOut);

public static class BookingService
{
    public static void ValidateCustomer(BookingCustomer customer)
    {
        if (string.IsNullOrWhiteSpace(customer.Name) || customer.Name.Length > 50
            || string.IsNullOrWhiteSpace(customer.Identity) || customer.Identity.Length > 12
            || string.IsNullOrWhiteSpace(customer.Nationality) || customer.Nationality.Length > 30
            || customer.Gender is not ("Nam" or "Nữ" or "Khác"))
            throw new InvalidOperationException("Vui lòng nhập đầy đủ thông tin khách hàng trong giới hạn cho phép.");
        Lab2Input.Phone(customer.Phone);
    }

    public static void ValidateRooms(IReadOnlyList<BookingRoom> rooms, DateTime now)
    {
        if (rooms.Count == 0) throw new InvalidOperationException("Vui lòng chọn ít nhất một phòng.");
        if (rooms.Select(r => r.RoomId).Distinct(StringComparer.OrdinalIgnoreCase).Count() != rooms.Count)
            throw new InvalidOperationException("Mỗi phòng chỉ được chọn một lần trong phiếu.");
        DateTime currentMinute = new(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0);
        foreach (var room in rooms)
        {
            if (string.IsNullOrWhiteSpace(room.RoomId) || room.Guests <= 0)
                throw new InvalidOperationException("Phòng hoặc số người không hợp lệ.");
            if (room.CheckIn < currentMinute || room.CheckOut.Date <= room.CheckIn.Date)
                throw new InvalidOperationException("Ngày nhận không được ở quá khứ; ngày trả phải sau ngày nhận.");
            if (room.CheckOut > new DateTime(2079, 6, 6, 23, 59, 0)
                || room.CheckIn.Second != 0 || room.CheckOut.Second != 0
                || room.CheckIn.Ticks % TimeSpan.TicksPerMinute != 0 || room.CheckOut.Ticks % TimeSpan.TicksPerMinute != 0)
                throw new InvalidOperationException("Thời gian phải có độ chính xác theo phút và trong giới hạn SMALLDATETIME.");
        }
    }

    public static async Task<string> SaveAsync(BookingCustomer customer, IReadOnlyList<BookingRoom> rooms,
        string employeeId, CancellationToken cancellationToken = default)
    {
        ValidateCustomer(customer);
        ValidateRooms(rooms, DateTime.Now);
        await using var db = new HotelManagementContext();
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        if (!await db.NhanViens.AnyAsync(n => n.MaNv == employeeId && n.DaXoa != true, cancellationToken))
            throw new InvalidOperationException("Nhân viên đăng nhập không còn hợp lệ. Vui lòng đăng nhập lại.");

        // Đọc lại trạng thái và lịch ngay trong giao dịch; không tin danh sách phòng đã tải trên UI.
        foreach (var selection in rooms)
        {
            var room = await db.Phongs.Include(p => p.MaLphNavigation)
                .AvailableDuring(selection.CheckIn, selection.CheckOut)
                .SingleOrDefaultAsync(p => p.MaPh == selection.RoomId, cancellationToken)
                ?? throw new InvalidOperationException($"Phòng {selection.RoomId} không còn trống trong thời gian đã chọn.");
            if (selection.Guests > room.MaLphNavigation.SoNguoiToiDa)
                throw new InvalidOperationException($"Số người của phòng {selection.RoomId} vượt sức chứa.");
        }

        var guest = await db.KhachHangs.FirstOrDefaultAsync(k => k.CccdPassport == customer.Identity, cancellationToken);
        if (guest == null)
        {
            guest = new Models.KhachHang
            {
                MaKh = Lab2Input.NextId(await db.KhachHangs.Select(k => k.MaKh).ToListAsync(cancellationToken), "KH", 3, 5),
                CccdPassport = customer.Identity, TenKh = customer.Name, Sdt = customer.Phone,
                QuocTich = customer.Nationality, GioiTinh = customer.Gender, DaXoa = false
            };
            db.KhachHangs.Add(guest);
        }
        else if (guest.DaXoa == true)
        {
            guest.TenKh = customer.Name; guest.Sdt = customer.Phone;
            guest.QuocTich = customer.Nationality; guest.GioiTinh = customer.Gender; guest.DaXoa = false;
        }

        string reservationId = Lab2Input.NextId(await db.PhieuThues.Select(p => p.MaPt).ToListAsync(cancellationToken), "PT", 3, 5);
        db.PhieuThues.Add(new PhieuThue { MaPt = reservationId, NgPt = DateTime.Now, MaKhNavigation = guest, MaNv = employeeId, DaXoa = false });
        await db.SaveChangesAsync(cancellationToken);
        var detailIds = await db.Ctdps.Select(c => c.MaCtdp).ToListAsync(cancellationToken);
        foreach (var selection in rooms)
        {
            var room = db.Phongs.Local.Single(p => p.MaPh == selection.RoomId);
            string detailId = Lab2Input.NextId(detailIds, "CTDP", 3, 7);
            detailIds.Add(detailId);
            var price = room.MaLphNavigation.GiaNgay;
            var total = price * (selection.CheckOut.Date - selection.CheckIn.Date).Days;
            if (total > RoomTypeInput.MaxMoney) throw new InvalidOperationException("Thành tiền vượt giới hạn MONEY.");
            db.Ctdps.Add(new Ctdp
            {
                MaCtdp = detailId, MaPt = reservationId, MaPh = selection.RoomId, SoNguoi = selection.Guests,
                CheckIn = selection.CheckIn, CheckOut = selection.CheckOut, TrangThai = "Đã đặt",
                DonGia = price, ThanhTien = total, DaXoa = false, TheoGio = false
            });
            // Trigger CapNhatGiaCTDP của CSDL mẫu chỉ xử lý một dòng mỗi lần.
            await db.SaveChangesAsync(cancellationToken);
            room.Ttph = "Đã đặt";
        }
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return reservationId;
    }
}
