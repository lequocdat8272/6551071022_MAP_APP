using HotelManagement.Controls;
using HotelManagement.Models;
using HotelManagement.Services;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.SoDoPhong;

public class FormDatPhong : Form
{
    private readonly TextBox _name = new() { Name = "txtHoVaTen", MaxLength = 50 };
    private readonly TextBox _identity = new() { Name = "txtCCCD", MaxLength = 12 };
    private readonly TextBox _phone = new() { Name = "txtSoDienThoai", MaxLength = 10 };
    private readonly TextBox _nationality = new() { Name = "txtQuocTich", MaxLength = 30, Text = "Việt Nam" };
    private readonly ComboBox _gender = new() { Name = "cbGioiTinh", DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly DateTimePicker _start = new() { Name = "dtpNgayBD", Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy HH:mm" };
    private readonly DateTimePicker _end = new() { Name = "dtpNgayKT", Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy HH:mm" };
    private readonly DataGridView _available = new() { Name = "dtgDanhSachPhongTrong" };
    private readonly DataGridView _selected = new() { Name = "dtgDanhSachPhongDaChon" };
    private readonly Button _save = new() { Name = "btnDatPhong", Text = "Đặt trước", AutoSize = true, BackColor = Color.Moccasin };
    private readonly Button _cancel = new() { Text = "Hủy", AutoSize = true, DialogResult = DialogResult.Cancel };
    private readonly List<BookingRoom> _rooms = [];
    private readonly CancellationTokenSource _lifetime = new();
    private CancellationTokenSource? _loading;
    private readonly string? _initialRoom;
    private bool _ready, _saving, _existingCustomer;
    private int _identityVersion;

    public FormDatPhong(string? maPhong = null)
    {
        _initialRoom = maPhong;
        Text = "Đặt phòng trước";
        Font = new Font("Segoe UI", 10);
        BackColor = Color.Honeydew;
        ClientSize = new Size(1180, 690);
        MinimumSize = new Size(1000, 600);
        StartPosition = FormStartPosition.CenterParent;
        _gender.Items.AddRange(["Nam", "Nữ", "Khác"]);
        _start.MinDate = DateTime.Today;
        _start.MaxDate = new DateTime(2079, 6, 5);
        _end.MaxDate = new DateTime(2079, 6, 6, 23, 59, 0);
        DateTime arrival = DateTime.Today.AddHours(14);
        if (arrival < DateTime.Now) arrival = arrival.AddDays(1);
        _start.Value = arrival;
        _end.Value = arrival.Date.AddDays(1).AddHours(12);
        var heading = new Label { Text = "ĐẶT PHÒNG", Dock = DockStyle.Top, Height = 48, TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.Firebrick, Font = new Font(Font, FontStyle.Bold) };
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(12) };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 315));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var customer = new GroupBox { Text = "Thông tin khách hàng", Dock = DockStyle.Fill };
        var fields = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1, Padding = new Padding(12) };
        foreach (var (label, control) in new (string, Control)[] { ("CCCD/Passport", _identity), ("Họ và tên", _name),
            ("Số điện thoại", _phone), ("Quốc tịch", _nationality), ("Giới tính", _gender) })
        {
            fields.Controls.Add(new Label { Text = label, AutoSize = true, Margin = new Padding(3, 12, 3, 4) });
            control.Dock = DockStyle.Top;
            fields.Controls.Add(control);
        }
        fields.Controls.Add(new Label { Text = "Rời ô CCCD/Passport để tra cứu khách đã có.\nDấu + để chọn phòng, dấu x để bỏ phòng.", AutoSize = true, MaximumSize = new Size(270, 0), Margin = new Padding(3, 20, 3, 3) });
        customer.Controls.Add(fields);
        layout.Controls.Add(customer, 0, 0);
        var right = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var dates = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, Padding = new Padding(8) };
        dates.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150)); dates.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        dates.Controls.Add(new Label { Text = "Ngày giờ nhận", AutoSize = true }, 0, 0);
        dates.Controls.Add(_start, 1, 0);
        dates.Controls.Add(new Label { Text = "Ngày giờ trả", AutoSize = true }, 0, 1);
        dates.Controls.Add(_end, 1, 1);
        _start.Width = _end.Width = 260;
        right.Controls.Add(dates, 0, 0);
        var lists = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        lists.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        lists.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
        DanhSachForm.ConfigureGrid(_available); DanhSachForm.ConfigureGrid(_selected);
        _available.Columns.Add("MaPH", "Mã phòng"); _available.Columns.Add("TenLPH", "Loại phòng");
        _available.Columns.Add("Capacity", "Sức chứa");
        _available.Columns.Add(DanhSachForm.ImageColumn("Them", "+", Properties.Resources.Add));
        _selected.Columns.Add("MaPH", "Phòng"); _selected.Columns.Add("Guests", "Số người");
        _selected.Columns.Add("CheckIn", "Nhận phòng"); _selected.Columns.Add("CheckOut", "Trả phòng");
        _selected.Columns["CheckIn"]!.DefaultCellStyle.Format = _selected.Columns["CheckOut"]!.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
        _selected.Columns["MaPH"]!.FillWeight = _selected.Columns["Guests"]!.FillWeight = 55;
        _selected.Columns["CheckIn"]!.FillWeight = _selected.Columns["CheckOut"]!.FillWeight = 145;
        _selected.Columns.Add(DanhSachForm.ImageColumn("Huy", "x", Properties.Resources.delete));
        var availableGroup = new GroupBox { Text = "Danh sách phòng trống", Dock = DockStyle.Fill };
        var selectedGroup = new GroupBox { Text = "Danh sách phòng đã chọn", Dock = DockStyle.Fill };
        availableGroup.Controls.Add(_available); selectedGroup.Controls.Add(_selected);
        lists.Controls.Add(availableGroup, 0, 0); lists.Controls.Add(selectedGroup, 1, 0);
        right.Controls.Add(lists, 0, 1);
        layout.Controls.Add(right, 1, 0);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 60, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(12) };
        actions.Controls.AddRange([_cancel, _save]);
        Controls.Add(layout); Controls.Add(heading); Controls.Add(actions);
        CancelButton = _cancel;
        _identity.TextChanged += (_, _) => { _identityVersion++; SetCustomerEditable(true); };
        _identity.Leave += async (_, _) => await LookupCustomerAsync();
        _start.ValueChanged += async (_, _) =>
        {
            if (!_ready) return;
            if (_end.Value.Date <= _start.Value.Date) _end.Value = _start.Value.Date.AddDays(1).AddHours(12);
            await LoadAvailableAsync();
        };
        _end.ValueChanged += async (_, _) => { if (_ready) await LoadAvailableAsync(); };
        _available.CellClick += async (_, e) =>
        {
            if (_saving || e.RowIndex < 0 || e.ColumnIndex < 0 || _available.Columns[e.ColumnIndex].Name != "Them") return;
            var row = _available.Rows[e.RowIndex];
            var room = new BookingRoom(row.Cells["MaPH"].Value!.ToString()!, Convert.ToInt32(row.Cells["Capacity"].Value), Minute(_start.Value), Minute(_end.Value));
            try
            {
                BookingService.ValidateRooms([room], DateTime.Now);
                if (_rooms.Any(r => r.RoomId == room.RoomId)) return;
                _rooms.Add(room); RefreshSelected(); await LoadAvailableAsync();
            }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Thông báo"); }
        };
        _selected.CellClick += async (_, e) =>
        {
            if (_saving || e.RowIndex < 0 || e.ColumnIndex < 0 || _selected.Columns[e.ColumnIndex].Name != "Huy") return;
            _rooms.RemoveAt(e.RowIndex); RefreshSelected(); await LoadAvailableAsync();
        };
        _save.Click += async (_, _) => await SaveAsync();
        Load += async (_, _) => { _ready = true; await LoadAvailableAsync(); };
        FormClosing += (_, e) => { if (_saving && e.CloseReason == CloseReason.UserClosing) e.Cancel = true; };
        Disposed += (_, _) => { _lifetime.Cancel(); _loading?.Cancel(); };
    }

    private static DateTime Minute(DateTime time) => new(time.Year, time.Month, time.Day, time.Hour, time.Minute, 0);

    private void SetCustomerEditable(bool editable)
    {
        if (editable && _existingCustomer)
        {
            _name.Clear(); _phone.Clear(); _nationality.Clear(); _gender.SelectedIndex = -1;
        }
        _existingCustomer = !editable;
        _name.Enabled = _phone.Enabled = _nationality.Enabled = _gender.Enabled = editable;
    }

    private async Task LookupCustomerAsync()
    {
        string identity = _identity.Text.Trim();
        int version = _identityVersion;
        if (identity.Length == 0) return;
        try
        {
            await using var db = new HotelManagementContext();
            var guest = await db.KhachHangs.AsNoTracking().FirstOrDefaultAsync(k => k.CccdPassport == identity && k.DaXoa != true, _lifetime.Token);
            if (IsDisposed || version != _identityVersion) return;
            if (guest == null) { SetCustomerEditable(true); return; }
            _name.Text = guest.TenKh; _phone.Text = guest.Sdt; _nationality.Text = guest.QuocTich; _gender.Text = guest.GioiTinh;
            SetCustomerEditable(false);
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
        catch (Exception ex) { if (!IsDisposed) MessageBox.Show(this, "Lỗi tra cứu khách hàng: " + ex.Message, "Lỗi"); }
    }

    private async Task LoadAvailableAsync()
    {
        _loading?.Cancel();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        _loading = cancellation;
        _available.Enabled = false;
        try
        {
            DateTime start = Minute(_start.Value), end = Minute(_end.Value);
            _available.Rows.Clear();
            if (end.Date <= start.Date) return;
            await using var db = new HotelManagementContext();
            var rooms = await db.Phongs.AsNoTracking().AvailableDuring(start, end).OrderBy(p => p.MaPh)
                .Select(p => new { p.MaPh, p.MaLphNavigation.TenLph, p.MaLphNavigation.SoNguoiToiDa }).ToListAsync(cancellation.Token);
            if (IsDisposed || cancellation.IsCancellationRequested) return;
            foreach (var room in rooms.Where(r => !_rooms.Any(s => s.RoomId == r.MaPh)))
            {
                int index = _available.Rows.Add(room.MaPh, room.TenLph, room.SoNguoiToiDa, Properties.Resources.Add);
                if (room.MaPh == _initialRoom) _available.CurrentCell = _available.Rows[index].Cells[0];
            }
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        catch (Exception ex) { if (!IsDisposed) MessageBox.Show(this, "Lỗi tải phòng trống: " + ex.Message, "Lỗi"); }
        finally
        {
            if (ReferenceEquals(_loading, cancellation))
            {
                _loading = null;
                if (!IsDisposed) _available.Enabled = !_saving;
            }
        }
    }

    private void RefreshSelected()
    {
        _selected.Rows.Clear();
        foreach (var room in _rooms) _selected.Rows.Add(room.RoomId, room.Guests, room.CheckIn, room.CheckOut, Properties.Resources.delete);
    }

    private async Task SaveAsync()
    {
        if (_saving) return;
        _saving = true;
        _loading?.Cancel();
        foreach (Control control in Controls) control.Enabled = false;
        try
        {
            await LookupCustomerAsync();
            var customer = new BookingCustomer(_name.Text.Trim(), _identity.Text.Trim(), _phone.Text.Trim(), _nationality.Text.Trim(), _gender.Text);
            string id = await BookingService.SaveAsync(customer, _rooms.ToList(), DangNhap.FormDangNhap.MaNV_DangNhap, _lifetime.Token);
            MessageBox.Show(this, $"Đặt phòng thành công! Mã phiếu: {id}", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _saving = false; DialogResult = DialogResult.OK; Close();
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
        catch (Exception ex)
        {
            if (!IsDisposed) MessageBox.Show(this, "Không thể đặt phòng: " + (ex.InnerException?.Message ?? ex.Message), "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            _saving = false;
            if (!IsDisposed)
            {
                foreach (Control control in Controls) control.Enabled = true;
                _available.Enabled = true;
            }
        }
    }
}
