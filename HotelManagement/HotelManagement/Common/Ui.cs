namespace HotelManagement.Common;

public static class Ui
{
    public static Button Button(string text) => new() { Text = text, AutoSize = true, MinimumSize = new Size(110, 34), BackColor = Color.Moccasin };
    public static DataGridView CreateGrid() => new()
    {
        Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
        MultiSelect = false, RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, BackgroundColor = Color.White,
        AutoGenerateColumns = false, RowTemplate = { Height = 36 }, ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
        AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells,
        DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.True }
    };
    public static void ActionColumn(DataGridView grid, string name, string title) => grid.Columns.Add(new DataGridViewButtonColumn
    {
        Name = name, HeaderText = title, Text = title, UseColumnTextForButtonValue = true,
        AutoSizeMode = DataGridViewAutoSizeColumnMode.None, Width = Math.Max(70, TextRenderer.MeasureText(title, grid.Font).Width + 24)
    });
    public static void Info(IWin32Window owner, string message) => MessageBox.Show(owner, message, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
}
