using ClosedXML.Excel;
using HotelManagement.DangNhap;
using HotelManagement.Services;

namespace HotelManagement.ThongKe;

public sealed class FormThongKe : Form
{
    private readonly DateTimePicker _from = DateField();
    private readonly DateTimePicker _to = DateField();
    private readonly Button _apply = new() { Text = "Xem thống kê", AutoSize = true, BackColor = Color.PaleTurquoise };
    private readonly Button _all = new() { Text = "Toàn bộ", AutoSize = true };
    private readonly Button _export = new() { Text = "Xuất Excel", AutoSize = true, Enabled = false };
    private readonly Label _period = new() { Dock = DockStyle.Top, Height = 28, Padding = new Padding(12, 4, 0, 0) };
    private readonly Label[] _totals = [new(), new(), new()];
    private readonly Label[] _leaders = [new(), new(), new()];
    private readonly ReportChart _roomRevenue = new("Doanh thu theo loại phòng", InvoiceData.Money);
    private readonly ReportChart _roomCount = new("Lượt đặt theo loại phòng", value => $"{value:N0}");
    private readonly ReportChart _services = new("Dịch vụ đã dùng trong hóa đơn đã thu", value => $"{value:N0}", pie: true);
    private readonly CancellationTokenSource _lifetime = new();
    private BusinessReport? _report;

    public FormThongKe()
    {
        Text = "Thống kê kinh doanh";
        Font = new Font("Segoe UI", 10);
        BackColor = Color.Honeydew;
        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 57, Padding = new Padding(10), AutoScroll = true, WrapContents = false };
        toolbar.Controls.AddRange([Caption("Từ ngày"), _from, Caption("Đến ngày"), _to, _apply, _all, _export]);
        var kpis = Cards(_totals, [Color.Teal, Color.SteelBlue, Color.SeaGreen], 82, Color.White);
        var leaders = Cards(_leaders, [Color.AliceBlue, Color.MintCream, Color.OldLace], 76, Color.DarkSlateGray);
        var body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, Padding = new Padding(5) };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 47));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 53));
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        body.Controls.Add(_services, 0, 0);
        body.SetRowSpan(_services, 2);
        body.Controls.Add(_roomRevenue, 1, 0);
        body.Controls.Add(_roomCount, 1, 1);
        Controls.Add(body);
        Controls.Add(leaders);
        Controls.Add(kpis);
        Controls.Add(_period);
        Controls.Add(toolbar);
        _apply.Click += async (_, _) => await LoadReportAsync();
        _all.Click += async (_, _) =>
        {
            SetAllDates();
            await LoadReportAsync();
        };
        _export.Click += (_, _) => Export();
        Load += async (_, _) =>
        {
            SetAllDates();
            await LoadReportAsync();
        };
        Disposed += (_, _) => _lifetime.Cancel();
    }

    private static Label Caption(string text) => new() { Text = text, AutoSize = true, Margin = new Padding(4, 7, 1, 0) };

    private static DateTimePicker DateField() => new()
    {
        Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy", Width = 126,
        MinDate = new DateTime(1900, 1, 1), MaxDate = new DateTime(2079, 6, 6)
    };

    private void SetAllDates()
    {
        _from.Value = _from.MinDate;
        _to.Value = DateTime.Today;
    }

    private static TableLayoutPanel Cards(Label[] labels, Color[] colors, int height, Color foreground)
    {
        var table = new TableLayoutPanel { Dock = DockStyle.Top, Height = height, ColumnCount = 3, RowCount = 1, Padding = new Padding(5, 0, 5, 0) };
        for (int i = 0; i < labels.Length; i++)
        {
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / labels.Length));
            var label = labels[i];
            label.Dock = DockStyle.Fill;
            label.Margin = new Padding(4);
            label.Padding = new Padding(8);
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.BackColor = colors[i];
            label.ForeColor = foreground;
            table.Controls.Add(label, i, 0);
        }
        return table;
    }

    private async Task LoadReportAsync()
    {
        if (!_apply.Enabled) return;
        _apply.Enabled = _all.Enabled = _export.Enabled = false;
        try
        {
            var report = await BusinessReport.LoadAsync(_from.Value, _to.Value, FormDangNhap.TenTK_DangNhap, _lifetime.Token);
            if (IsDisposed) return;
            _report = report;
            _period.Text = $"Kỳ báo cáo {report.From:dd/MM/yyyy} – {report.To:dd/MM/yyyy}   •   Doanh thu từ hóa đơn đã thu";
            _totals[0].Text = $"Doanh thu tiền phòng\n{InvoiceData.Money(report.RoomRevenue)}";
            _totals[1].Text = $"Doanh thu dịch vụ\n{InvoiceData.Money(report.ServiceRevenue)}";
            _totals[2].Text = $"Số lượt đặt phòng\n{report.Bookings:N0} lượt";
            var topRoom = report.Rooms.Where(r => r.Revenue > 0).MaxBy(r => r.Revenue);
            var topBooking = report.Rooms.Where(r => r.Bookings > 0).MaxBy(r => r.Bookings);
            var topService = report.Services.Where(s => s.Revenue > 0).MaxBy(s => s.Revenue);
            _leaders[0].Text = "Loại phòng doanh thu cao nhất\n" + (topRoom == null ? "—" : $"{topRoom.Name} • {InvoiceData.Money(topRoom.Revenue)}");
            _leaders[1].Text = "Loại phòng được đặt nhiều nhất\n" + (topBooking == null ? "—" : $"{topBooking.Name} • {topBooking.Bookings:N0} lượt");
            _leaders[2].Text = "Dịch vụ doanh thu cao nhất\n" + (topService == null ? "—" : $"{topService.Name} • {InvoiceData.Money(topService.Revenue)}");
            _roomRevenue.Display(report.Rooms.Select(r => (r.Name, r.Revenue)));
            _roomCount.Display(report.Rooms.Select(r => (r.Name, (decimal)r.Bookings)));
            _services.Display(report.Services.Select(s => (s.Name, (decimal)s.Quantity)));
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
        catch (Exception ex) { if (!IsDisposed) MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        finally
        {
            if (!IsDisposed)
            {
                _apply.Enabled = _all.Enabled = true;
                _export.Enabled = _report != null;
            }
        }
    }

    private void Export()
    {
        if (_report is not { } report) return;
        using var save = new SaveFileDialog { Filter = "Excel (*.xlsx)|*.xlsx", FileName = $"ThongKe_{report.From:yyyyMMdd}_{report.To:yyyyMMdd}.xlsx" };
        if (save.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            using var workbook = new XLWorkbook();
            var summary = workbook.AddWorksheet("Tổng hợp");
            summary.Cell(1, 1).Value = $"Kỳ báo cáo {report.From:dd/MM/yyyy} – {report.To:dd/MM/yyyy}";
            summary.Cell(2, 1).Value = "Doanh thu phòng";
            summary.Cell(2, 2).Value = report.RoomRevenue;
            summary.Cell(3, 1).Value = "Doanh thu dịch vụ";
            summary.Cell(3, 2).Value = report.ServiceRevenue;
            summary.Cell(4, 1).Value = "Lượt đặt";
            summary.Cell(4, 2).Value = report.Bookings;
            var rooms = workbook.AddWorksheet("Loại phòng");
            rooms.Cell(1, 1).Value = "Loại phòng";
            rooms.Cell(1, 2).Value = "Doanh thu";
            rooms.Cell(1, 3).Value = "Lượt đặt";
            for (int i = 0; i < report.Rooms.Count; i++)
            {
                var room = report.Rooms[i];
                rooms.Cell(i + 2, 1).Value = room.Name;
                rooms.Cell(i + 2, 2).Value = room.Revenue;
                rooms.Cell(i + 2, 3).Value = room.Bookings;
            }
            var services = workbook.AddWorksheet("Dịch vụ");
            services.Cell(1, 1).Value = "Dịch vụ";
            services.Cell(1, 2).Value = "Doanh thu";
            services.Cell(1, 3).Value = "Số lượng";
            for (int i = 0; i < report.Services.Count; i++)
            {
                var service = report.Services[i];
                services.Cell(i + 2, 1).Value = service.Name;
                services.Cell(i + 2, 2).Value = service.Revenue;
                services.Cell(i + 2, 3).Value = service.Quantity;
            }
            foreach (var sheet in workbook.Worksheets)
            {
                sheet.Row(1).Style.Font.Bold = true;
                sheet.ColumnsUsed().AdjustToContents();
            }
            workbook.SaveAs(save.FileName);
            MessageBox.Show(this, "Đã xuất báo cáo Excel.", Text);
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
}
