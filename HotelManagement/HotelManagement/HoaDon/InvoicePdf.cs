using HotelManagement.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace HotelManagement.HoaDon;

internal static class InvoicePdf
{
    internal static void Save(InvoiceData invoice, string path)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        byte[] pdf = Document.Create(doc => doc.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(36);
            page.DefaultTextStyle(style => style.FontFamily("Lato").FontSize(10));
            page.Header().Column(header =>
            {
                header.Item().Text("HOTEL MANAGEMENT").FontSize(10).FontColor(Colors.Teal.Darken2).Bold();
                header.Item().PaddingTop(6).Text("HÓA ĐƠN THANH TOÁN").FontSize(23).Bold();
                header.Item().PaddingTop(4).Text($"{invoice.Id}   •   {invoice.Date:dd/MM/yyyy HH:mm}   •   {invoice.Status}");
            });
            page.Content().PaddingVertical(22).Column(content =>
            {
                content.Spacing(8);
                content.Item().Text($"Khách hàng: {invoice.Guest}");
                content.Item().Text($"Nhân viên lập: {invoice.Employee}");
                content.Item().Text($"Phòng: {invoice.Room}   •   Loại phòng: {invoice.RoomType}");
                content.Item().Text($"Nhận: {invoice.Arrival:dd/MM/yyyy HH:mm}   •   Trả: {invoice.Departure:dd/MM/yyyy HH:mm}   •   {invoice.Duration}");
                content.Item().PaddingTop(8).Text($"Tiền phòng: {InvoiceData.Money(invoice.RoomCharge)}").Bold();
                content.Item().PaddingTop(10).Text("Dịch vụ đã sử dụng").FontSize(13).Bold();
                if (invoice.Services.Count == 0) content.Item().Text("Không phát sinh dịch vụ.");
                else content.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(2);
                    });
                    table.Header(header =>
                    {
                        foreach (string title in new[] { "Dịch vụ", "Đơn giá", "SL", "Thành tiền" })
                            header.Cell().Background(Colors.Teal.Lighten4).Padding(7).Text(title).Bold();
                    });
                    foreach (var line in invoice.Services)
                    {
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(7).Text(line.Name);
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(7).AlignRight().Text(InvoiceData.Money(line.UnitPrice));
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(7).AlignRight().Text(line.Quantity.ToString());
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(7).AlignRight().Text(InvoiceData.Money(line.Total));
                    }
                });
                content.Item().PaddingTop(12).AlignRight().Text($"Tổng cộng: {InvoiceData.Money(invoice.Total)}").FontSize(15).Bold();
            });
            page.Footer().AlignCenter().Text(text =>
            {
                text.Span("Trang ");
                text.CurrentPageNumber();
                text.Span(" / ");
                text.TotalPages();
            });
        })).GeneratePdf();
        File.WriteAllBytes(path, pdf);
    }
}
