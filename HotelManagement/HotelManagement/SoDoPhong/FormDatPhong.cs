using HotelManagement.Common;
using HotelManagement.DangNhap;
using HotelManagement.Models;
using HotelManagement.Services;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.SoDoPhong;

public sealed class FormDatPhong : Form
{
    private readonly TextBox _passport = new() { MaxLength = 12 }, _name = new() { MaxLength = 50 },
        _phone = new() { MaxLength = 10 }, _nationality = new() { MaxLength = 30 };
    private readonly ComboBox _gender = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly DateTimePicker _start = new(), _end = new();
    private readonly NumericUpDown _guests = new() { Minimum = 1, Maximum = 100, Value = 1, Width = 65 };
    private readonly DataGridView _available = Ui.CreateGrid(), _chosen = Ui.CreateGrid();
    private readonly Button _save = Ui.Button("Đặt phòng"), _lookup = Ui.Button("Tra cứu CCCD"), _refresh = Ui.Button("Tìm phòng trống");
    private readonly Label _message = new() { AutoSize = true, ForeColor = Color.DarkCyan, MaximumSize = new Size(290, 0) };
    private readonly CancellationTokenSource _lifetime = new();
    private readonly string? _initialRoom;
    private CancellationTokenSource? _searchCancellation;
    private readonly List<BookingRoom> _rooms = new();
    private bool _ready, _saving;

    public FormDatPhong(string? selectedRoom = null)
    {
        _initialRoom = selectedRoom;
        Text = "Đặt phòng";
        Font = new Font("Segoe UI", 10);
        ClientSize = new Size(1220, 720);
        MinimumSize = new Size(1050, 660);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.Honeydew;
        var heading = new Label { Dock = DockStyle.Top, Height = 48, Text = "ĐẶT PHÒNG", Font = new Font(Font, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter, ForeColor = Color.DarkCyan };
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(10) };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 310));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var customer = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true };
        customer.Controls.Add(new Label { Text = "Thông tin khách hàng", AutoSize = true, Font = new Font(Font, FontStyle.Bold) });
        foreach (var (caption, control) in new (string, Control)[] { ("CCCD/Passport", _passport), ("Họ tên", _name), ("Số điện thoại", _phone), ("Quốc tịch", _nationality), ("Giới tính", _gender) })
        {
            customer.Controls.Add(new Label { Text = caption, AutoSize = true, Margin = new Padding(3, 12, 3, 2) });
            control.Width = 282; control.Name = caption;
            customer.Controls.Add(control);
        }
        _gender.Items.AddRange(new object[] { "Nam", "Nữ" });
        customer.Controls.Add(_lookup); customer.Controls.Add(_message);
        var roomPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        roomPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 145)); roomPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var dates = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true };
        _start.Format = _end.Format = DateTimePickerFormat.Custom;
        _start.CustomFormat = _end.CustomFormat = "dd/MM/yyyy HH:mm";
        _start.Width = _end.Width = 190;
        _start.Value = DateTime.Today.AddDays(1).AddHours(14);
        _end.Value = DateTime.Today.AddDays(2).AddHours(12);
        _start.MinDate = DateTime.Today;
        _start.MaxDate = new DateTime(2079, 6, 5);
        _end.MaxDate = new DateTime(2079, 6, 6, 23, 59, 0);
        foreach (var (caption, control) in new (string, Control)[] { ("Nhận phòng", _start), ("Trả phòng", _end), ("Số người/phòng", _guests) })
        {
            var block = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, WrapContents = false };
            block.Controls.Add(new Label { Text = caption, AutoSize = true }); block.Controls.Add(control); dates.Controls.Add(block);
        }
        dates.Controls.Add(_refresh);
        dates.Controls.Add(new Label { Text = "Mỗi phòng giữ ngày và giờ tại lúc bấm Chọn. Chọn lại phòng để đổi thời gian.", AutoSize = true });
        roomPanel.Controls.Add(dates, 0, 0);
        var grids = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        grids.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45)); grids.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
        grids.Controls.Add(GridGroup("Danh sách phòng trống", _available), 0, 0);
        grids.Controls.Add(GridGroup("Danh sách phòng đã chọn", _chosen), 1, 0);
        roomPanel.Controls.Add(grids, 0, 1);
        layout.Controls.Add(customer, 0, 0); layout.Controls.Add(roomPanel, 1, 0);
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 55, FlowDirection = FlowDirection.RightToLeft };
        var cancel = Ui.Button("Hủy"); cancel.DialogResult = DialogResult.Cancel;
        buttons.Controls.Add(cancel); buttons.Controls.Add(_save);
        Controls.Add(layout); Controls.Add(buttons); Controls.Add(heading);
        CancelButton = cancel;
        _available.Columns.Add("MaPH", "Mã phòng"); _available.Columns.Add("LoaiPhong", "Loại phòng"); _available.Columns.Add("SucChua", "Tối đa");
        Ui.ActionColumn(_available, "Them", "Chọn");
        _chosen.Columns.Add("MaPH", "Phòng"); _chosen.Columns.Add("SoNguoi", "Số người");
        _chosen.Columns.Add("CheckIn", "Nhận phòng"); _chosen.Columns.Add("CheckOut", "Trả phòng");
        _chosen.Columns["CheckIn"]!.DefaultCellStyle.Format = _chosen.Columns["CheckOut"]!.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
        Ui.ActionColumn(_chosen, "Huy", "Bỏ chọn");
        _passport.TextChanged += (_, _) => { CustomerEditable(true); _name.Clear(); _phone.Clear(); _nationality.Clear(); _gender.SelectedIndex = -1; _message.Text = ""; };
        _lookup.Click += async (_, _) =>
        {
            _save.Enabled = false;
            try { await LookupAsync(); }
            catch (OperationCanceledException) { }
            catch (Exception ex) { if (!IsDisposed) Ui.Info(this, "Lỗi tra cứu khách hàng: " + ex.Message); }
            finally { if (!IsDisposed) _save.Enabled = true; }
        };
        _refresh.Click += async (_, _) => await LoadAvailableAsync();
        _start.ValueChanged += async (_, _) => { if (_ready) { if (_end.Value.Date <= _start.Value.Date) _end.Value = _start.Value.Date.AddDays(1).AddHours(12); await LoadAvailableAsync(); } };
        _end.ValueChanged += async (_, _) => { if (_ready) await LoadAvailableAsync(); };
        _available.CellClick += (_, e) =>
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || _available.Columns[e.ColumnIndex].Name != "Them") return;
            var row = _available.Rows[e.RowIndex];
            string id = row.Cells["MaPH"].Value!.ToString()!;
            if (_rooms.Any(x => x.RoomId == id)) { Ui.Info(this, "Phòng đã được chọn."); return; }
            if ((int)_guests.Value > Convert.ToInt32(row.Cells["SucChua"].Value)) { Ui.Info(this, "Số người vượt sức chứa của phòng."); return; }
            var booking = new BookingRoom(id, (int)_guests.Value, Minute(_start.Value), Minute(_end.Value));
            try { BookingService.ValidateRooms(new[] { booking }); }
            catch (ArgumentException ex) { Ui.Info(this, ex.Message); return; }
            _rooms.Add(booking); RenderChosen();
        };
        _chosen.CellClick += (_, e) =>
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || _chosen.Columns[e.ColumnIndex].Name != "Huy") return;
            string id = _chosen.Rows[e.RowIndex].Cells["MaPH"].Value!.ToString()!;
            _rooms.RemoveAll(x => x.RoomId == id); RenderChosen();
        };
        _save.Click += async (_, _) =>
        {
            _saving = true; layout.Enabled = buttons.Enabled = false;
            try
            {
                await LookupAsync();
                var input = new BookingCustomer(_name.Text.Trim(), _passport.Text.Trim(), _phone.Text.Trim(), _nationality.Text.Trim(), _gender.Text);
                string id = await new BookingService().SaveAsync(new BookingRequest(FormDangNhap.MaNV_DangNhap, input, _rooms.ToArray()));
                Ui.Info(this, "Đặt phòng thành công. Mã phiếu thuê: " + id);
                _saving = false; DialogResult = DialogResult.OK; Close();
            }
            catch (Exception ex) { if (!IsDisposed) Ui.Info(this, ex.InnerException?.Message ?? ex.Message); }
            finally { _saving = false; if (!IsDisposed) layout.Enabled = buttons.Enabled = true; }
        };
        Load += async (_, _) => { _ready = true; await LoadAvailableAsync(); };
        FormClosing += (_, e) => { if (_saving) e.Cancel = true; };
        Disposed += (_, _) => { _lifetime.Cancel(); _searchCancellation?.Cancel(); };
    }

    private static GroupBox GridGroup(string title, Control grid)
    {
        var group = new GroupBox { Text = title, Dock = DockStyle.Fill, Padding = new Padding(6, 22, 6, 6) };
        group.Controls.Add(grid); return group;
    }
    private static DateTime Minute(DateTime value) => new(value.Year, value.Month, value.Day, value.Hour, value.Minute, 0);
    private void CustomerEditable(bool enabled) => _name.Enabled = _phone.Enabled = _nationality.Enabled = _gender.Enabled = enabled;

    private async Task LookupAsync()
    {
        string passport = _passport.Text.Trim();
        if (passport.Length == 0) return;
        _lookup.Enabled = false;
        try
        {
            await using var db = new HotelManagementContext();
            var customer = await db.KhachHangs.AsNoTracking().FirstOrDefaultAsync(x => x.CccdPassport == passport && x.DaXoa != true, _lifetime.Token);
            if (IsDisposed || passport != _passport.Text.Trim()) return;
            if (customer == null) { _message.Text = "Khách hàng mới. Nhập đầy đủ thông tin để tạo khi đặt phòng."; return; }
            _name.Text = customer.TenKh; _phone.Text = customer.Sdt; _nationality.Text = customer.QuocTich; _gender.SelectedItem = customer.GioiTinh;
            CustomerEditable(false);
            _message.Text = "Đã tìm thấy khách hàng. Thông tin được giữ theo hồ sơ hiện có.";
        }
        finally { if (!IsDisposed) _lookup.Enabled = true; }
    }

    private async Task LoadAvailableAsync()
    {
        _searchCancellation?.Cancel();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        _searchCancellation = cancellation;
        _available.Enabled = false;
        try
        {
            DateTime start = Minute(_start.Value), end = Minute(_end.Value);
            _available.Rows.Clear();
            if (end.Date <= start.Date) { _refresh.Text = "Kiểm tra ngày trả"; return; }
            await using var db = new HotelManagementContext();
            var rooms = await db.Phongs.AsNoTracking().AvailableDuring(start, end).OrderBy(x => x.MaPh)
                .Select(x => new { x.MaPh, x.MaLphNavigation.TenLph, x.MaLphNavigation.SoNguoiToiDa }).ToListAsync(cancellation.Token);
            if (IsDisposed || cancellation.IsCancellationRequested) return;
            foreach (var room in rooms)
            {
                int index = _available.Rows.Add(room.MaPh, room.TenLph, room.SoNguoiToiDa);
                if (room.MaPh == _initialRoom) _available.CurrentCell = _available.Rows[index].Cells[0];
            }
            _refresh.Text = $"Tìm phòng trống ({rooms.Count})";
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { if (!IsDisposed) Ui.Info(this, "Lỗi tải phòng trống: " + ex.Message); }
        finally
        {
            if (ReferenceEquals(_searchCancellation, cancellation))
            {
                _searchCancellation = null;
                if (!IsDisposed) _available.Enabled = true;
            }
        }
    }
    private void RenderChosen()
    {
        _chosen.Rows.Clear();
        foreach (var room in _rooms) _chosen.Rows.Add(room.RoomId, room.Guests, room.CheckIn, room.CheckOut);
    }
}
