namespace HotelManagement.Controls;

public abstract class NhapLieuForm : Form
{
    protected readonly TableLayoutPanel Fields = new();
    protected readonly Button SaveButton = new();
    protected readonly Button ExitButton = new();
    protected readonly CancellationTokenSource Lifetime = new();
    private readonly Panel _body = new();
    private bool _busy;

    protected NhapLieuForm(string title)
    {
        Text = title;
        Font = new Font("Segoe UI", 10);
        BackColor = Color.Honeydew;
        ClientSize = new Size(650, 560);
        MinimumSize = new Size(560, 400);
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        var heading = new Label { Text = title.ToUpperInvariant(), Dock = DockStyle.Top, Height = 65,
            TextAlign = ContentAlignment.MiddleCenter, ForeColor = Color.Firebrick, Font = new Font(Font, FontStyle.Bold) };
        Fields.Dock = DockStyle.Top;
        Fields.AutoSize = true;
        Fields.ColumnCount = 2;
        Fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
        Fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
        Fields.Padding = new Padding(16, 8, 16, 8);
        _body.Dock = DockStyle.Fill;
        _body.AutoScroll = true;
        _body.Controls.Add(Fields);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 65, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(12) };
        ExitButton.Text = "Thoát";
        ExitButton.AutoSize = true;
        ExitButton.BackColor = Color.LightSteelBlue;
        ExitButton.DialogResult = DialogResult.Cancel;
        SaveButton.Text = "Lưu";
        SaveButton.AutoSize = true;
        SaveButton.BackColor = Color.Moccasin;
        actions.Controls.AddRange([ExitButton, SaveButton]);
        Controls.Add(_body);
        Controls.Add(heading);
        Controls.Add(actions);
        AcceptButton = SaveButton;
        CancelButton = ExitButton;
        Load += async (_, _) => await ExecuteAsync(LoadDataAsync, false);
        SaveButton.Click += async (_, _) => await ExecuteAsync(SaveDataAsync, true);
        FormClosing += (_, e) => { if (_busy && e.CloseReason == CloseReason.UserClosing) e.Cancel = true; };
        Disposed += (_, _) => Lifetime.Cancel();
    }

    protected void Field(string label, Control control)
    {
        int row = Fields.RowCount++;
        Fields.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Fields.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(4, 12, 8, 12) }, 0, row);
        control.Dock = DockStyle.Top;
        control.Margin = new Padding(4, 9, 4, 9);
        Fields.Controls.Add(control, 1, row);
    }

    protected TextBox TextField(string label, string name, int maxLength, bool readOnly = false)
    {
        var control = new TextBox { Name = name, MaxLength = maxLength, ReadOnly = readOnly };
        Field(label, control);
        return control;
    }

    protected ComboBox ChoiceField(string label, string name, params string[] choices)
    {
        var control = new ComboBox { Name = name, DropDownStyle = ComboBoxStyle.DropDownList };
        control.Items.AddRange(choices);
        Field(label, control);
        return control;
    }

    protected static string Required(Control control, string label, int maxLength)
    {
        string value = control.Text.Trim();
        if (value.Length == 0 || value.Length > maxLength)
        {
            control.Focus();
            throw new InvalidOperationException($"{label} bắt buộc, tối đa {maxLength} ký tự.");
        }
        return value;
    }

    protected virtual Task LoadDataAsync() => Task.CompletedTask;
    protected abstract Task SaveDataAsync();

    private async Task ExecuteAsync(Func<Task> action, bool saving)
    {
        if (_busy || IsDisposed) return;
        _busy = true;
        SaveButton.Enabled = ExitButton.Enabled = _body.Enabled = false;
        try
        {
            await action();
            if (saving && !IsDisposed) { _busy = false; DialogResult = DialogResult.OK; Close(); }
        }
        catch (OperationCanceledException) when (Lifetime.IsCancellationRequested) { }
        catch (Exception ex)
        {
            if (!IsDisposed)
            {
                MessageBox.Show(this, ex.InnerException?.Message ?? ex.Message, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                if (!saving) { _busy = false; DialogResult = DialogResult.Cancel; Close(); }
            }
        }
        finally
        {
            _busy = false;
            if (!IsDisposed) SaveButton.Enabled = ExitButton.Enabled = _body.Enabled = true;
        }
    }
}
