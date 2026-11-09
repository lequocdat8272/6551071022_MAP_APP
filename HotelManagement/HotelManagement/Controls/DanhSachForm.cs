using HotelManagement.Services;

namespace HotelManagement.Controls;

// Khung danh sách dùng chung cho các danh mục của Lab 2.
public abstract class DanhSachForm : Form
{
    protected readonly DataGridView Grid = new();
    protected readonly TextBox Search = new();
    protected readonly Button AddButton = new();
    protected readonly FlowLayoutPanel Toolbar = new();
    protected readonly CancellationTokenSource Lifetime = new();
    private readonly Button _export = new();
    private readonly Label _count = new();
    private bool _busy;
    private readonly bool _detailsOnly;

    protected DanhSachForm(string title, bool detailsOnly = false)
    {
        _detailsOnly = detailsOnly;
        Text = title;
        Font = new Font("Segoe UI", 10);
        BackColor = Color.Honeydew;
        ClientSize = new Size(960, 620);
        MinimumSize = new Size(650, 400);
        StartPosition = FormStartPosition.CenterParent;
        var toolbar = Toolbar;
        toolbar.Dock = DockStyle.Top;
        toolbar.Height = 65;
        toolbar.Padding = new Padding(12);
        toolbar.WrapContents = false;
        toolbar.AutoScroll = true;
        toolbar.Controls.Add(new PictureBox { Image = Properties.Resources.search, SizeMode = PictureBoxSizeMode.Zoom, Size = new Size(30, 32) });
        Search.Name = "txtTimKiem";
        Search.Width = 230;
        Search.PlaceholderText = "Tìm theo mã hoặc tên";
        Search.Margin = new Padding(4, 4, 16, 4);
        _export.Text = "Xuất file Excel";
        _export.AutoSize = true;
        _export.BackColor = Color.DarkTurquoise;
        AddButton.Name = "btnThemMoi";
        AddButton.Text = "Thêm mới";
        AddButton.AutoSize = true;
        AddButton.BackColor = Color.LightPink;
        toolbar.Controls.AddRange([Search, _export, AddButton]);
        _count.Dock = DockStyle.Bottom;
        _count.Height = 30;
        _count.Padding = new Padding(12, 4, 0, 0);
        ConfigureGrid(Grid);
        Controls.Add(Grid);
        Controls.Add(toolbar);
        Controls.Add(_count);
        Search.TextChanged += (_, _) => Filter();
        AddButton.Click += async (_, _) => await EditAsync(null);
        _export.Click += (_, _) => Export();
        Grid.CellClick += async (_, e) =>
        {
            if (_busy || e.RowIndex < 0 || e.ColumnIndex < 0) return;
            string action = Grid.Columns[e.ColumnIndex].Name;
            string id = Grid.Rows[e.RowIndex].Cells[1].Value?.ToString() ?? "";
            if (id.Length == 0) return;
            if (action == "Sua") await EditAsync(id);
            if (action == "Xoa" && MessageBox.Show(this, $"Bạn có chắc muốn xóa {id}?", "Xác nhận xóa",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                await RunAsync(async () => await DeleteAsync(id));
                await ReloadAsync();
            }
        };
        Load += async (_, _) => await ReloadAsync();
        Disposed += (_, _) => Lifetime.Cancel();
    }

    public static void ConfigureGrid(DataGridView grid)
    {
        grid.Dock = DockStyle.Fill;
        grid.ReadOnly = true;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AllowUserToResizeRows = false;
        grid.MultiSelect = false;
        grid.RowHeadersVisible = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.RowTemplate.Height = 38;
        grid.BackgroundColor = Color.White;
        grid.BorderStyle = BorderStyle.FixedSingle;
        grid.AutoGenerateColumns = false;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
    }

    public static DataGridViewImageColumn ImageColumn(string name, string header, Image image) => new()
    {
        Name = name, HeaderText = header, Image = image, ImageLayout = DataGridViewImageCellLayout.Zoom,
        AutoSizeMode = DataGridViewAutoSizeColumnMode.None, Width = 48, Resizable = DataGridViewTriState.False
    };

    protected void Columns(Image icon, params (string Name, string Header)[] columns)
    {
        Grid.Columns.Add(ImageColumn("Icon", "", icon));
        foreach (var column in columns) Grid.Columns.Add(column.Name, column.Header);
        if (_detailsOnly)
        {
            var details = ImageColumn("Sua", "Chi tiết", Properties.Resources.details);
            details.Width = 68;
            Grid.Columns.Add(details);
        }
        else
        {
            Grid.Columns.Add(ImageColumn("Sua", "Sửa", Properties.Resources.edit));
            Grid.Columns.Add(ImageColumn("Xoa", "Xóa", Properties.Resources.delete));
        }
    }

    protected void Row(Image icon, params object?[] values)
    {
        Image[] actions = _detailsOnly ? [Properties.Resources.details] : [Properties.Resources.edit, Properties.Resources.delete];
        Grid.Rows.Add(new object?[] { icon }.Concat(values).Concat(actions).Select(value => value ?? "").ToArray());
    }

    protected abstract Task LoadRowsAsync();
    protected abstract Form CreateEditor(string? id);
    protected virtual Task DeleteAsync(string id) => throw new NotSupportedException();

    public async Task ReloadAsync()
    {
        await RunAsync(async () =>
        {
            await LoadRowsAsync();
            if (!IsDisposed) Filter();
        });
    }

    private async Task EditAsync(string? id)
    {
        using var editor = CreateEditor(id);
        if (editor.ShowDialog(this) == DialogResult.OK) await ReloadAsync();
    }

    protected async Task RunAsync(Func<Task> work)
    {
        if (_busy || IsDisposed) return;
        _busy = true;
        Grid.Enabled = AddButton.Enabled = _export.Enabled = false;
        try { await work(); }
        catch (OperationCanceledException) when (Lifetime.IsCancellationRequested) { }
        catch (Exception ex)
        {
            if (!IsDisposed) MessageBox.Show(this, ex.InnerException?.Message ?? ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _busy = false;
            if (!IsDisposed) Grid.Enabled = AddButton.Enabled = _export.Enabled = true;
        }
    }

    protected void Filter()
    {
        string keyword = Search.Text.Trim();
        Grid.CurrentCell = null;
        foreach (DataGridViewRow row in Grid.Rows)
            row.Visible = MatchesRow(row, keyword);
        _count.Text = $"{Grid.Rows.Cast<DataGridViewRow>().Count(r => r.Visible)} dòng";
    }

    protected virtual bool MatchesRow(DataGridViewRow row, string keyword)
        => keyword.Length == 0 || row.Cells.Cast<DataGridViewCell>()
            .Where(c => c.OwningColumn is not DataGridViewImageColumn)
            .Any(c => (c.Value?.ToString() ?? "").Contains(keyword, StringComparison.OrdinalIgnoreCase));

    private void Export()
    {
        var rows = Grid.Rows.Cast<DataGridViewRow>().Where(r => r.Visible && !r.IsNewRow).ToList();
        if (rows.Count == 0) { MessageBox.Show(this, "Không có dữ liệu để xuất!", "Thông báo"); return; }
        using var dialog = new SaveFileDialog { Filter = "Excel Workbook (*.xlsx)|*.xlsx", FileName = $"{GetType().Name}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            ExcelExport.SaveVisibleRows(Grid, dialog.FileName);
            if (MessageBox.Show(this, "Xuất thành công. Mở file vừa xuất?", "Thông báo", MessageBoxButtons.YesNo) == DialogResult.Yes)
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dialog.FileName) { UseShellExecute = true });
        }
        catch (Exception ex) { MessageBox.Show(this, "Không thể xuất Excel: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
}
