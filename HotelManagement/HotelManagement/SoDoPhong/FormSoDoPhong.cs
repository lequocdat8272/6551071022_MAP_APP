using HotelManagement.Models;
using HotelManagement.Services;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.SoDoPhong;

public partial class FormSoDoPhong : Form
{
    private readonly System.Windows.Forms.Timer _filterTimer;
    private CancellationTokenSource? _loadCancellation;
    private bool _ready;

    public FormSoDoPhong()
    {
        InitializeComponent();
        ClientSizeChanged += (_, _) => CapNhatBoCuc();
        CapNhatBoCuc();
        _filterTimer = new System.Windows.Forms.Timer(components!) { Interval = 250 };
        _filterTimer.Tick += async (_, _) => { _filterTimer.Stop(); await TaiLaiDanhSachAsync(); };
        Load += FormSoDoPhong_Load;
        txtSearchMaPhong.TextChanged += (_, _) => ScheduleFilter();
        dtNgayChon.ValueChanged += (_, _) => ScheduleFilter();
        dateTimePicker2.ValueChanged += (_, _) => ScheduleFilter();
        btnDatPhong.Click += btnDatPhong_Click;
        foreach (var panel in new[] { panel2, panel3, panel4 })
            foreach (var radio in panel.Controls.OfType<RadioButton>())
                radio.CheckedChanged += (_, _) => { if (radio.Checked) ScheduleFilter(); };
        Disposed += (_, _) => _loadCancellation?.Cancel();
    }

    private async void FormSoDoPhong_Load(object? sender, EventArgs e)
    {
        rdbTatCaTrangThaiPhong.Checked = true;
        rdbTatCaLoaiPhong.Checked = true;
        rdbTatCaPhong.Checked = true;
        dtNgayChon.Format = DateTimePickerFormat.Short;
        dtNgayChon.ShowCheckBox = true;
        dtNgayChon.Checked = false;
        dateTimePicker2.Format = DateTimePickerFormat.Time;
        dateTimePicker2.ShowUpDown = true;
        dateTimePicker2.ShowCheckBox = true;
        dateTimePicker2.Checked = false;
        dateTimePicker2.Enabled = false;
        toolTip1.SetToolTip(txtSearchMaPhong, "Tìm mã phòng, ví dụ: P101, 20...");
        toolTip1.SetToolTip(dtNgayChon, "Tích để tìm phòng trống trong ngày đã chọn.");
        toolTip1.SetToolTip(dateTimePicker2, "Tích để kiểm tra tại giờ đã chọn; bỏ tích để kiểm tra cả ngày.");
        dataGridViewPhong.ReadOnly = true;
        dataGridViewPhong.AllowUserToAddRows = false;
        dataGridViewPhong.AllowUserToDeleteRows = false;
        dataGridViewPhong.MultiSelect = false;
        dataGridViewPhong.RowHeadersVisible = false;
        dataGridViewPhong.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dataGridViewPhong.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        CapNhatBoCuc();
        _ready = true;
        await TaiLaiDanhSachAsync();
    }

    private void CapNhatBoCuc()
    {
        dataGridViewPhong.Size = new Size(Math.Max(200, ClientSize.Width - dataGridViewPhong.Left - 12),
            Math.Max(100, ClientSize.Height - dataGridViewPhong.Top - btnDatPhong.Height - 24));
        btnDatPhong.Location = new Point(Math.Max(dataGridViewPhong.Left, ClientSize.Width - btnDatPhong.Width - 12),
            Math.Max(dataGridViewPhong.Bottom + 6, ClientSize.Height - btnDatPhong.Height - 12));
    }

    private void ScheduleFilter()
    {
        if (!_ready || IsDisposed) return;
        dateTimePicker2.Enabled = dtNgayChon.Checked;
        _loadCancellation?.Cancel();
        _filterTimer.Stop();
        _filterTimer.Start();
    }

    private async Task TaiLaiDanhSachAsync()
    {
        _loadCancellation?.Cancel();
        using var cancellation = new CancellationTokenSource();
        _loadCancellation = cancellation;
        try
        {
            string? status = rdbPhongTrong.Checked ? "Bình thường"
                : rdbPhongDaDat.Checked ? "Đã đặt" : rdbPhongDangThue.Checked ? "Đang thuê"
                : rdbPhongDangSuaChua.Checked ? "Đang sửa chữa" : null;
            string? type = rdbPhongThuongDon.Checked ? "NOR01" : rdbPhongThuongDoi.Checked ? "NOR02"
                : rdbPhongVipDon.Checked ? "VIP01" : rdbPhongVipDoi.Checked ? "VIP02" : null;
            string? cleaning = rdbPhongDaDonDep.Checked ? "Đã dọn dẹp"
                : rdbPhongChuaDon.Checked ? "Chưa dọn dẹp" : null;
            string keyword = txtSearchMaPhong.Text.Trim();
            await using var db = new HotelManagementContext();
            var query = db.Phongs.AsNoTracking().Where(p => p.DaXoa != true);
            if (status != null) query = query.Where(p => p.Ttph == status);
            if (type != null) query = query.Where(p => p.MaLph == type);
            if (cleaning != null) query = query.Where(p => p.Ttdd == cleaning);
            if (keyword.Length > 0) query = query.Where(p => p.MaPh.Contains(keyword));
            if (dtNgayChon.Checked)
            {
                var start = dtNgayChon.Value.Date;
                var end = start.AddDays(1);
                if (dateTimePicker2.Checked)
                {
                    start = start.Add(dateTimePicker2.Value.TimeOfDay);
                    // SMALLDATETIME có độ chính xác theo phút.
                    start = new DateTime(start.Year, start.Month, start.Day, start.Hour, start.Minute, 0);
                    end = start.AddMinutes(1);
                }
                query = query.AvailableDuring(start, end);
            }
            var rooms = await query.OrderBy(p => p.MaPh).Select(p => new
            {
                MaPH = p.MaPh,
                Tang = p.MaPh.Length > 1 ? p.MaPh.Substring(1, 1) : "",
                TenLPH = p.MaLphNavigation.TenLph,
                TTPH = p.Ttph,
                TTDD = p.Ttdd,
                p.GhiChu
            }).ToListAsync(cancellation.Token);
            if (cancellation.IsCancellationRequested || IsDisposed) return;
            dataGridViewPhong.DataSource = rooms;
            string[] names = { "MaPH", "Tang", "TenLPH", "TTPH", "TTDD", "GhiChu" };
            string[] headers = { "Mã phòng", "Tầng", "Loại phòng", "Trạng thái phòng", "Dọn dẹp", "Ghi chú" };
            for (int i = 0; i < names.Length; i++) dataGridViewPhong.Columns[names[i]]!.HeaderText = headers[i];
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            if (!IsDisposed && !cancellation.IsCancellationRequested)
                MessageBox.Show(this, "Lỗi tải danh sách phòng: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            if (ReferenceEquals(_loadCancellation, cancellation)) _loadCancellation = null;
        }
    }

    private void btnDatPhong_Click(object? sender, EventArgs e)
    {
        string? room = dataGridViewPhong.CurrentRow?.Cells["MaPH"].Value?.ToString();
        MessageBox.Show(this, "Chức năng đặt phòng thuộc phần thực hành tiếp theo."
            + (room == null ? "" : Environment.NewLine + "Phòng đang chọn: " + room),
            "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
