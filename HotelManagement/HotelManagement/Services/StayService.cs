using HotelManagement.Models;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace HotelManagement.Services;

internal static class StayService
{
    internal const string Reserved = "Đã đặt";
    internal const string Occupied = "Đang thuê";
    internal const string Cancelled = "Đã hủy";
    internal const string Paid = "Đã thanh toán";
    internal const string AwaitingPayment = "Chờ KH xác nhận";

    internal static IQueryable<Ctdp> ActiveDetails(HotelManagementContext db) =>
        db.Ctdps.Where(c => c.DaXoa != true && c.MaPtNavigation.DaXoa != true);

    internal static IQueryable<Ctdv> UsedServices(HotelManagementContext db, string detailId) =>
        db.Ctdvs.Where(v => v.MaCtdp == detailId && v.DaXoa != true);

    internal static async Task CheckInAsync(string detailId, CancellationToken token)
    {
        await using var db = new HotelManagementContext();
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
        var detail = await LoadDetailAsync(db, detailId, Reserved, token);
        var room = detail.MaPhNavigation;
        DateTime now = DateTime.Now;
        if (room.DaXoa == true || room.Ttph == "Đang sửa chữa" || detail.CheckOut <= now)
            throw new InvalidOperationException("Không thể nhận phòng đã hết lịch hoặc đang sửa chữa.");
        if (await ActiveDetails(db).AnyAsync(c => c.MaPh == detail.MaPh && c.MaCtdp != detailId &&
            (c.TrangThai == Occupied || (c.TrangThai == Reserved && c.CheckOut > now && c.CheckIn < detail.CheckIn)), token))
            throw new InvalidOperationException("Phòng đang có khách hoặc còn lịch đặt trước phiếu này.");

        detail.TrangThai = Occupied;
        room.Ttph = Occupied;
        await db.SaveChangesAsync(token);
        await transaction.CommitAsync(token);
    }

    internal static async Task CancelAsync(string detailId, CancellationToken token)
    {
        await using var db = new HotelManagementContext();
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
        var detail = await LoadDetailAsync(db, detailId, Reserved, token);
        detail.TrangThai = Cancelled;
        await UpdateRoomStatusAsync(db, detail, token);
        await db.SaveChangesAsync(token);
        await transaction.CommitAsync(token);
    }

    internal static async Task AddServiceAsync(string detailId, string serviceId, int quantity, CancellationToken token)
    {
        if (quantity <= 0) throw new InvalidOperationException("Số lượng phải lớn hơn 0.");
        await using var db = new HotelManagementContext();
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
        await LoadDetailAsync(db, detailId, Occupied, token);
        var service = await db.DichVus.SingleOrDefaultAsync(d => d.MaDv == serviceId && d.DaXoa != true, token)
            ?? throw new InvalidOperationException("Dịch vụ đã ngừng cung cấp.");
        if (service.SlconLai is >= 0 and int stock)
        {
            if (stock < quantity) throw new InvalidOperationException($"Dịch vụ chỉ còn {stock} phần.");
            service.SlconLai = stock - quantity;
        }

        var line = await db.Ctdvs.SingleOrDefaultAsync(v => v.MaCtdp == detailId && v.MaDv == serviceId, token);
        if (line == null)
        {
            line = new Ctdv { MaCtdp = detailId, MaDv = serviceId };
            db.Ctdvs.Add(line);
        }
        line.Sl = checked((line.DaXoa == true ? 0 : line.Sl) + quantity);
        line.DonGia = service.DonGia;
        line.ThanhTien = service.DonGia * line.Sl;
        if (line.ThanhTien > RoomTypeInput.MaxMoney)
            throw new InvalidOperationException("Thành tiền dịch vụ vượt giới hạn.");
        line.DaXoa = false;
        await db.SaveChangesAsync(token);
        await transaction.CommitAsync(token);
    }

    internal static async Task<string> CheckoutAsync(string detailId, string employeeId, CancellationToken token)
    {
        await using var db = new HotelManagementContext();
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
        var detail = await LoadDetailAsync(db, detailId, Occupied, token);
        if (!await db.NhanViens.AnyAsync(n => n.MaNv == employeeId && n.DaXoa != true, token))
            throw new InvalidOperationException("Phiên đăng nhập đã hết hiệu lực.");
        if (await db.HoaDons.AnyAsync(h => h.MaCtdp == detailId && h.DaXoa != true, token))
            throw new InvalidOperationException("Phòng đã có hóa đơn.");

        detail.TrangThai = Paid;
        await UpdateRoomStatusAsync(db, detail, token);
        detail.MaPhNavigation.Ttdd = "Chưa dọn dẹp";
        await db.SaveChangesAsync(token);
        // Trigger của CSDL cũ tính lại tiền phòng khi sửa CTDP.
        await db.Entry(detail).ReloadAsync(token);
        decimal serviceTotal = await UsedServices(db, detailId).SumAsync(v => v.ThanhTien ?? v.Sl * (v.DonGia ?? v.MaDvNavigation.DonGia), token);
        decimal total = (detail.ThanhTien ?? 0m) + serviceTotal;
        if (total > RoomTypeInput.MaxMoney) throw new InvalidOperationException("Trị giá hóa đơn vượt giới hạn.");
        string invoiceId = Lab2Input.NextId(await db.HoaDons.Select(h => h.MaHd).ToListAsync(token), "HD", 3, 5);
        db.HoaDons.Add(new Models.HoaDon
        {
            MaHd = invoiceId,
            MaNv = employeeId,
            MaCtdp = detailId,
            NgHd = DateTime.Now,
            TriGia = total,
            TrangThai = AwaitingPayment,
            DaXoa = false
        });
        await db.SaveChangesAsync(token);
        await transaction.CommitAsync(token);
        return invoiceId;
    }

    internal static async Task ConfirmPaymentAsync(string invoiceId, CancellationToken token)
    {
        await using var db = new HotelManagementContext();
        var invoice = await db.HoaDons.SingleOrDefaultAsync(h => h.MaHd == invoiceId && h.DaXoa != true, token)
            ?? throw new InvalidOperationException("Hóa đơn không còn tồn tại.");
        if (invoice.TrangThai == Paid) return;
        if (invoice.TrangThai != AwaitingPayment) throw new InvalidOperationException("Hóa đơn không ở trạng thái chờ xác nhận.");
        invoice.TrangThai = Paid;
        await db.SaveChangesAsync(token);
    }

    private static async Task<Ctdp> LoadDetailAsync(HotelManagementContext db, string detailId, string expected, CancellationToken token)
    {
        var detail = await ActiveDetails(db).Include(c => c.MaPhNavigation).SingleOrDefaultAsync(c => c.MaCtdp == detailId, token)
            ?? throw new InvalidOperationException("Không tìm thấy chi tiết phiếu thuê.");
        if (detail.TrangThai != expected) throw new InvalidOperationException($"Phòng phải ở trạng thái \"{expected}\".");
        return detail;
    }

    private static async Task UpdateRoomStatusAsync(HotelManagementContext db, Ctdp detail, CancellationToken token)
    {
        var room = detail.MaPhNavigation;
        if (room.Ttph == "Đang sửa chữa") return;
        DateTime now = DateTime.Now;
        var states = await ActiveDetails(db).Where(c => c.MaPh == detail.MaPh && c.MaCtdp != detail.MaCtdp
            && (c.TrangThai == Occupied || (c.TrangThai == Reserved && c.CheckOut > now)))
            .Select(c => c.TrangThai).Distinct().ToListAsync(token);
        room.Ttph = states.Contains(Occupied) ? Occupied : states.Contains(Reserved) ? Reserved : "Bình thường";
    }
}
