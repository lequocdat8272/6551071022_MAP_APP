namespace HotelManagement.Common;

// Các danh mục LAB2 dùng chung tìm kiếm, xuất Excel và vòng đời tải dữ liệu.
public abstract class FormDanhSach : Form
{
    protected readonly DataGridView Grid = Ui.CreateGrid();
    protected readonly Button AddButton = Ui.Button("Thêm mới");
    private readonly Button _export = Ui.Button("Xuất Excel");
    private readonly TextBox _search = new() { Width = 260, PlaceholderText = "Nhập từ khóa tìm kiếm..." };
    private readonly Label _status = new() { Dock = DockStyle.Bottom, Height = 28, TextAlign = ContentAlignment.MiddleLeft };
    private readonly CancellationTokenSource _lifetime = new();
    private bool _busy;
    protected CancellationToken Lifetime => _lifetime.Token;

    protected FormDanhSach(string title, params (string Name, string Header)[] columns)
    {
        Text = title;
        Font = new Font("Segoe UI", 10);
        BackColor = Color.Honeydew;
        ClientSize = new Size(1060, 650);
        MinimumSize = new Size(680, 440);
        StartPosition = FormStartPosition.CenterParent;
        var header = new Label { Text = title, Dock = DockStyle.Top, Height = 48,
            Font = new Font(Font, FontStyle.Bold), ForeColor = Color.DarkCyan, Padding = new Padding(12) };
        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 55, Padding = new Padding(8), AutoScroll = true };
        toolbar.Controls.AddRange(new Control[] { _search, _export, AddButton });
        Controls.Add(Grid);
        Controls.Add(_status);
        Controls.Add(toolbar);
        Controls.Add(header);
        foreach (var (name, caption) in columns) Grid.Columns.Add(name, caption);
        Ui.ActionColumn(Grid, "Sua", "Sửa");
        Ui.ActionColumn(Grid, "Xoa", "Xóa");
        _search.TextChanged += (_, _) => Filter();
        _export.Click += (_, _) => ExportExcel();
        AddButton.Click += async (_, _) => await EditAsync(null);
        Grid.CellClick += async (_, e) =>
        {
            if (_busy || e.RowIndex < 0 || e.ColumnIndex < 0) return;
            var row = Grid.Rows[e.RowIndex];
            string id = row.Cells[0].Value?.ToString() ?? "";
            string action = Grid.Columns[e.ColumnIndex].Name;
            if (action == "Sua") await EditAsync(id);
            if (action == "Xoa" && MessageBox.Show(this, $"Xóa {id}?", "Xác nhận xóa",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                await RunAsync(async () => await DeleteAsync(id));
                await ReloadAsync();
            }
        };
        Load += async (_, _) => await ReloadAsync();
        Disposed += (_, _) => _lifetime.Cancel();
    }

    protected abstract Task LoadRowsAsync();
    protected abstract Form CreateEditor(string? id);
    protected abstract Task DeleteAsync(string id);

    private async Task EditAsync(string? id)
    {
        using var form = CreateEditor(id);
        if (form.ShowDialog(this) == DialogResult.OK) await ReloadAsync();
    }

    public Task ReloadAsync() => RunAsync(async () =>
    {
        await LoadRowsAsync();
        if (!IsDisposed) Filter();
    });

    private async Task RunAsync(Func<Task> action)
    {
        if (_busy || IsDisposed) return;
        _busy = true;
        Grid.Enabled = AddButton.Enabled = _export.Enabled = false;
        _status.Text = "Đang xử lý...";
        try { await action(); }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            if (!IsDisposed) MessageBox.Show(this, ex.InnerException?.Message ?? ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _busy = false;
            if (!IsDisposed)
            {
                Grid.Enabled = AddButton.Enabled = _export.Enabled = true;
                Filter();
            }
        }
    }

    private void Filter()
    {
        Grid.CurrentCell = null;
        string keyword = _search.Text.Trim();
        foreach (DataGridViewRow row in Grid.Rows)
            row.Visible = keyword.Length == 0 || row.Cells.Cast<DataGridViewCell>()
                .Where(c => c.OwningColumn is DataGridViewTextBoxColumn)
                .Any(c => c.Value?.ToString()?.Contains(keyword, StringComparison.CurrentCultureIgnoreCase) == true);
        _status.Text = $"{Grid.Rows.Cast<DataGridViewRow>().Count(r => r.Visible)} bản ghi";
    }

    private void ExportExcel()
    {
        var rows = Grid.Rows.Cast<DataGridViewRow>().Where(r => r.Visible && !r.IsNewRow).ToList();
        if (rows.Count == 0) { Ui.Info(this, "Không có dữ liệu để xuất."); return; }
        using var dialog = new SaveFileDialog { Filter = "Excel Workbook (*.xlsx)|*.xlsx", FileName = $"{GetType().Name}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            ExcelExporter.Write(Grid, dialog.FileName);
            Ui.Info(this, "Xuất Excel thành công.");
        }
        catch (Exception ex) { Ui.Info(this, "Không thể xuất Excel: " + ex.Message); }
    }
}
