using HotelManagement.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace HotelManagement.Services;

internal sealed record InvoiceLine(string Name, decimal UnitPrice, int Quantity, decimal Total);

internal sealed record InvoiceData(string Id, DateTime? Date, string Employee, string Guest, string Room,
    string RoomType, DateTime Arrival, DateTime Departure, bool Hourly, decimal RoomCharge, string Status,
    IReadOnlyList<InvoiceLine> Services)
{
    internal static readonly CultureInfo Vietnamese = CultureInfo.GetCultureInfo("vi-VN");
    internal decimal Total => RoomCharge + Services.Sum(line => line.Total);
    internal string Duration => Hourly || Arrival.Date == Departure.Date
        ? $"{Math.Max(1, Math.Ceiling((Departure - Arrival).TotalHours))} giờ"
        : $"{Math.Max(1, (Departure.Date - Arrival.Date).Days)} đêm";

    internal static string Money(decimal value) => value.ToString("#,##0.####", Vietnamese) + " VNĐ";

    internal static async Task<InvoiceData> LoadAsync(string id, CancellationToken token)
    {
        await using var db = new HotelManagementContext();
        var invoice = await db.HoaDons.AsNoTracking().Where(h => h.MaHd == id && h.DaXoa != true)
            .Select(h => new
            {
                h.MaHd, h.NgHd, h.TrangThai, h.MaCtdp,
                Employee = h.MaNvNavigation != null ? h.MaNvNavigation.TenNv : "",
                Guest = h.MaCtdpNavigation.MaPtNavigation.MaKhNavigation.TenKh,
                Room = h.MaCtdpNavigation.MaPh,
                RoomType = h.MaCtdpNavigation.MaPhNavigation.MaLphNavigation.TenLph,
                h.MaCtdpNavigation.CheckIn, h.MaCtdpNavigation.CheckOut,
                h.MaCtdpNavigation.TheoGio, h.MaCtdpNavigation.ThanhTien
            }).SingleOrDefaultAsync(token) ?? throw new InvalidOperationException("Không tìm thấy hóa đơn.");
        var services = await StayService.UsedServices(db, invoice.MaCtdp).AsNoTracking().OrderBy(v => v.MaDv)
            .Select(v => new InvoiceLine(v.MaDvNavigation.TenDv, v.DonGia ?? v.MaDvNavigation.DonGia,
                v.Sl, v.ThanhTien ?? v.Sl * (v.DonGia ?? v.MaDvNavigation.DonGia))).ToListAsync(token);
        return new InvoiceData(invoice.MaHd, invoice.NgHd, invoice.Employee, invoice.Guest, invoice.Room, invoice.RoomType,
            invoice.CheckIn, invoice.CheckOut, invoice.TheoGio == true, invoice.ThanhTien ?? 0m, invoice.TrangThai, services);
    }
}
