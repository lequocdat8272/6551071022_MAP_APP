namespace HotelManagement.Common;

public abstract class FormNhapLieu : Form
{
    protected readonly TableLayoutPanel Fields = new() { Dock = DockStyle.Fill, ColumnCount = 2, AutoScroll = true, Padding = new Padding(18) };
    private readonly Button _save = Ui.Button("Lưu");
    private bool _saving;
    protected FormNhapLieu(string title)
    {
        Text = title;
        Font = new Font("Segoe UI", 10);
        ClientSize = new Size(600, 520);
        MinimumSize = new Size(540, 420);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.Honeydew;
        Fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
        Fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var titleLabel = new Label { Text = title, Dock = DockStyle.Top, Height = 60, Padding = new Padding(18), ForeColor = Color.DarkCyan, Font = new Font(Font, FontStyle.Bold) };
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 60, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(10) };
        var cancel = Ui.Button("Thoát");
        cancel.DialogResult = DialogResult.Cancel;
        buttons.Controls.AddRange(new Control[] { cancel, _save });
        Controls.Add(Fields);
        Controls.Add(buttons);
        Controls.Add(titleLabel);
        AcceptButton = _save;
        CancelButton = cancel;
        _save.Enabled = false;
        Load += async (_, _) =>
        {
            Fields.RowCount++;
            Fields.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            try { await LoadDataAsync(); if (!IsDisposed) _save.Enabled = true; }
            catch (Exception ex)
            {
                if (!IsDisposed) { Ui.Info(this, ex.Message); DialogResult = DialogResult.Cancel; Close(); }
            }
        };
        _save.Click += async (_, _) =>
        {
            if (_saving) return;
            _saving = true;
            _save.Enabled = Fields.Enabled = cancel.Enabled = false;
            try { await SaveDataAsync(); DialogResult = DialogResult.OK; _saving = false; Close(); }
            catch (Exception ex) { Ui.Info(this, ex.InnerException?.Message ?? ex.Message); }
            finally
            {
                _saving = false;
                if (!IsDisposed) _save.Enabled = Fields.Enabled = cancel.Enabled = true;
            }
        };
        FormClosing += (_, e) => { if (_saving) e.Cancel = true; };
    }

    protected abstract Task LoadDataAsync();
    protected abstract Task SaveDataAsync();

    protected T Field<T>(string label, T control) where T : Control
    {
        int row = Fields.RowCount++;
        Fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        Fields.Controls.Add(new Label { Text = label, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, row);
        control.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        control.Name = label;
        Fields.Controls.Add(control, 1, row);
        return control;
    }
    protected TextBox TextField(string label, int length) => Field(label, new TextBox { MaxLength = length });
    protected ComboBox Choice(string label, params string[] values)
    {
        var combo = Field(label, new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList });
        combo.Items.AddRange(values);
        return combo;
    }
    protected static void SelectValue(ComboBox combo, string value)
    {
        if (!combo.Items.Contains(value)) combo.Items.Add(value);
        combo.SelectedItem = value;
    }
}
