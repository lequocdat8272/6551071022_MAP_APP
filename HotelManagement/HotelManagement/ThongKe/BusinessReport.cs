using HotelManagement.Models;
using HotelManagement.Services;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.ThongKe;

internal sealed record RoomMetric(string Id, string Name, decimal Revenue, int Bookings);
internal sealed record ServiceMetric(string Id, string Name, decimal Revenue, long Quantity);
internal sealed record BusinessReport(DateTime From, DateTime To, IReadOnlyList<RoomMetric> Rooms, IReadOnlyList<ServiceMetric> Services)
{
    internal decimal RoomRevenue => Rooms.Sum(r => r.Revenue);
    internal decimal ServiceRevenue => Services.Sum(s => s.Revenue);
    internal int Bookings => Rooms.Sum(r => r.Bookings);

    internal static async Task<BusinessReport> LoadAsync(DateTime from, DateTime to, string actor, CancellationToken token)
    {
        if (from.Date > to.Date) throw new InvalidOperationException("Từ ngày phải trước hoặc bằng đến ngày.");
        DateTime start = from.Date;
        DateTime end = to.Date.AddDays(1).AddMinutes(-1);
        await using var db = new HotelManagementContext();
        await AccountService.RequireRoleAsync(db, actor, 2, token);

        // Một số CTDP trong dữ liệu cũ có nhiều hóa đơn. Chỉ ghi nhận lần thanh toán đầu tiên.
        var invoices = await db.HoaDons.AsNoTracking()
            .Where(h => h.DaXoa != true && h.TrangThai == StayService.Paid && h.NgHd != null && h.NgHd <= end)
            .OrderBy(h => h.NgHd).ThenBy(h => h.MaHd)
            .Select(h => new
            {
                h.MaCtdp, h.NgHd, h.MaCtdpNavigation.ThanhTien,
                h.MaCtdpNavigation.MaPhNavigation.MaLph,
                h.MaCtdpNavigation.MaPhNavigation.MaLphNavigation.TenLph
            }).ToListAsync(token);
        var paid = invoices.DistinctBy(h => h.MaCtdp).Where(h => h.NgHd >= start).ToArray();
        var paidDetails = paid.Select(h => h.MaCtdp).ToArray();
        var booked = await StayService.ActiveDetails(db).AsNoTracking()
            .Where(c => c.MaPtNavigation.NgPt >= start && c.MaPtNavigation.NgPt <= end
                && c.TrangThai != StayService.Cancelled && c.TrangThai != "Hủy")
            .Select(c => new { c.MaPhNavigation.MaLph, c.MaPhNavigation.MaLphNavigation.TenLph }).ToListAsync(token);

        var rooms = paid.Select(h => new RoomMetric(h.MaLph, h.TenLph, h.ThanhTien ?? 0m, 0))
            .Concat(booked.Select(c => new RoomMetric(c.MaLph, c.TenLph, 0m, 1)))
            .GroupBy(row => (row.Id, row.Name))
            .Select(g => new RoomMetric(g.Key.Id, g.Key.Name, g.Sum(r => r.Revenue), g.Sum(r => r.Bookings)))
            .OrderBy(r => r.Name).ToArray();
        var serviceTotals = await db.Ctdvs.AsNoTracking().Where(v => v.DaXoa != true && paidDetails.Contains(v.MaCtdp))
            .Select(v => new { v.MaDv, v.MaDvNavigation.TenDv, Quantity = (long)v.Sl,
                Amount = v.ThanhTien ?? v.Sl * (v.DonGia ?? v.MaDvNavigation.DonGia) })
            .GroupBy(v => new { v.MaDv, v.TenDv })
            .Select(g => new { g.Key.MaDv, g.Key.TenDv, Revenue = g.Sum(v => v.Amount), Quantity = g.Sum(v => v.Quantity) })
            .OrderBy(v => v.TenDv).ToListAsync(token);
        var services = serviceTotals.Select(v => new ServiceMetric(v.MaDv, v.TenDv, v.Revenue, v.Quantity)).ToArray();
        return new BusinessReport(start, to.Date, rooms, services);
    }
}
