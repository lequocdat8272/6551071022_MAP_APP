using HotelManagement.Models;

namespace HotelManagement.Services;

public static class RoomQueries
{
    // Khoảng nửa mở [bắt đầu, kết thúc): trả phòng đúng đầu khoảng không gây trùng.
    public static IQueryable<Models.Phong> AvailableDuring(
        this IQueryable<Models.Phong> rooms, DateTime start, DateTime end)
    {
        if (end <= start) throw new ArgumentException("Thời gian kết thúc phải sau thời gian bắt đầu.");
        return rooms.Where(p => p.DaXoa != true && p.Ttph == "Bình thường"
            && !p.Ctdps.Any(c => c.DaXoa != true && c.MaPtNavigation.DaXoa != true
                && c.TrangThai != "Đã hủy" && c.TrangThai != "Hủy"
                && c.CheckIn < end && c.CheckOut > start));
    }

    public static IQueryable<Ctdp> PendingReservations(this IQueryable<Ctdp> bookings, string roomId, DateTime now)
        => bookings.Where(c => c.MaPh == roomId && c.DaXoa != true
            && c.MaPtNavigation.DaXoa != true && c.CheckOut > now
            && c.TrangThai != "Đã xong" && c.TrangThai != "Đã hủy" && c.TrangThai != "Hủy");
}
