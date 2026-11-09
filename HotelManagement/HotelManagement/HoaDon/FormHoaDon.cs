using HotelManagement.Controls;
using HotelManagement.Services;

namespace HotelManagement.HoaDon;

public sealed class FormHoaDon : DanhSachForm
{
    private readonly string _invoiceId;
    private InvoiceData? _invoice;
    private readonly Label _summary = new()
    {
        Dock = DockStyle.Top, Height = 134, Padding = new Padding(14, 10, 10, 0),
        ForeColor = Color.DarkSlateGray
    };
    private readonly Button _exportPdf = new() { Text = "Xuất hóa đơn PDF", AutoSize = true, BackColor = Color.Moccasin };
    private readonly Button _confirmPayment = new() { Text = "Xác nhận đã thu tiền", AutoSize = true, BackColor = Color.Honeydew };
    private bool _paymentConfirmed;

    public FormHoaDon(string invoiceId) : base($"Hóa đơn {invoiceId}", detailsOnly: true)
    {
        _invoiceId = invoiceId;
        ClientSize = new Size(850, 570);
        Controls.Add(_summary);
        foreach (Control control in Toolbar.Controls) control.Visible = false;
        Toolbar.Controls.AddRange([_exportPdf, _confirmPayment]);
        _exportPdf.Enabled = _confirmPayment.Enabled = false;
        Columns(Properties.Resources.DichVu, ("DichVu", "Dịch vụ đã dùng"), ("Gia", "Đơn giá"),
            ("SL", "Số lượng"), ("Tien", "Thành tiền"));
        Grid.Columns["Sua"]!.Visible = false;
        foreach (string name in new[] { "Gia", "Tien" })
        {
            Grid.Columns[name]!.DefaultCellStyle.Format = "#,##0.####";
            Grid.Columns[name]!.DefaultCellStyle.FormatProvider = InvoiceData.Vietnamese;
        }
        Grid.EnabledChanged += (_, _) => UpdateActions();
        _exportPdf.Click += (_, _) => ExportPdf();
        _confirmPayment.Click += async (_, _) =>
        {
            if (_invoice?.Status != StayService.AwaitingPayment ||
                MessageBox.Show(this, $"Xác nhận đã thu {InvoiceData.Money(_invoice.Total)} cho hóa đơn {_invoiceId}?",
                    Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            await RunAsync(async () =>
            {
                await StayService.ConfirmPaymentAsync(_invoiceId, Lifetime.Token);
                _paymentConfirmed = true;
            });
            await ReloadAsync();
        };
    }

    protected override async Task LoadRowsAsync()
    {
        var invoice = await InvoiceData.LoadAsync(_invoiceId, Lifetime.Token);
        if (IsDisposed) return;
        _invoice = invoice;
        _summary.Text = $"HÓA ĐƠN {invoice.Id}   •   {invoice.Date:dd/MM/yyyy HH:mm}   •   {invoice.Status}\n" +
            $"Khách: {invoice.Guest}   •   Nhân viên: {invoice.Employee}\n" +
            $"Phòng {invoice.Room} ({invoice.RoomType})   •   {invoice.Arrival:dd/MM/yyyy HH:mm} → {invoice.Departure:dd/MM/yyyy HH:mm} ({invoice.Duration})\n" +
            $"Tiền phòng: {InvoiceData.Money(invoice.RoomCharge)}   •   TỔNG CỘNG: {InvoiceData.Money(invoice.Total)}";
        Grid.Rows.Clear();
        foreach (var line in invoice.Services)
            Row(Properties.Resources.DichVu, line.Name, line.UnitPrice, line.Quantity, line.Total);
    }

    private void UpdateActions()
    {
        _exportPdf.Enabled = Grid.Enabled && _invoice != null;
        _confirmPayment.Enabled = Grid.Enabled && _invoice?.Status == StayService.AwaitingPayment;
    }

    private void ExportPdf()
    {
        if (_invoice == null) return;
        using var dialog = new SaveFileDialog { Filter = "Hóa đơn PDF (*.pdf)|*.pdf", FileName = $"HoaDon_{_invoiceId}.pdf" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            InvoicePdf.Save(_invoice, dialog.FileName);
            if (MessageBox.Show(this, "Đã xuất hóa đơn. Mở file PDF?", Text, MessageBoxButtons.YesNo) == DialogResult.Yes)
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dialog.FileName) { UseShellExecute = true });
        }
        catch (Exception ex) { MessageBox.Show(this, "Lỗi xuất PDF: " + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        if (_paymentConfirmed) DialogResult = DialogResult.OK;
        base.OnFormClosed(e);
    }

    protected override Form CreateEditor(string? id) => throw new NotSupportedException();
}
