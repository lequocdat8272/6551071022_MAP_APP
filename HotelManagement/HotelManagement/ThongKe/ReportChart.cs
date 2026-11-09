namespace HotelManagement.ThongKe;

internal sealed class ReportChart : Control
{
    private static readonly Color[] Palette = [Color.Teal, Color.SteelBlue, Color.DarkOrange, Color.SeaGreen,
        Color.MediumPurple, Color.IndianRed, Color.CadetBlue, Color.DimGray];
    private readonly string _title;
    private readonly bool _pie;
    private readonly Func<decimal, string> _format;
    private readonly ToolTip _tip = new();
    private (string Label, decimal Value)[] _series = [];
    private readonly List<(Rectangle Bounds, string Text)> _hitAreas = [];
    private string? _currentTip;

    internal ReportChart(string title, Func<decimal, string> format, bool pie = false)
    {
        _title = title;
        _format = format;
        _pie = pie;
        Dock = DockStyle.Fill;
        Margin = new Padding(5);
        BackColor = Color.White;
        DoubleBuffered = true;
    }

    internal void Display(IEnumerable<(string Label, decimal Value)> values)
    {
        var positive = values.Where(point => point.Value > 0).OrderByDescending(point => point.Value).ToArray();
        _series = positive.Length <= 7 ? positive : [.. positive.Take(7), ("Khác", positive.Skip(7).Sum(point => point.Value))];
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        _hitAreas.Clear();
        using var titleFont = new Font(Font, FontStyle.Bold);
        TextRenderer.DrawText(e.Graphics, _title, titleFont, new Rectangle(12, 8, Math.Max(1, Width - 24), 29),
            Color.DarkSlateGray, TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);
        if (_series.Length == 0)
        {
            TextRenderer.DrawText(e.Graphics, "Không có dữ liệu", Font, new Rectangle(12, 45, Math.Max(1, Width - 24), 28), Color.DimGray);
            return;
        }
        if (Height < 75 || Width < 120) return;
        if (_pie) DrawPie(e.Graphics);
        else DrawBars(e.Graphics);
    }

    private void DrawBars(Graphics graphics)
    {
        int labelWidth = Width / 3;
        int amountWidth = Math.Max(75, Width / 4);
        int barWidth = Math.Max(1, Width - labelWidth - amountWidth - 24);
        int rowHeight = Math.Max(15, Math.Min(32, (Height - 46) / _series.Length));
        decimal max = _series[0].Value;
        for (int i = 0; i < _series.Length; i++)
        {
            var point = _series[i];
            int y = 40 + i * rowHeight;
            using var brush = new SolidBrush(Palette[i]);
            graphics.FillRectangle(brush, labelWidth + 8, y + 4, Math.Max(1, (int)(barWidth * (point.Value / max))), rowHeight - 7);
            TextRenderer.DrawText(graphics, point.Label, Font, new Rectangle(8, y, labelWidth, rowHeight), ForeColor,
                TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);
            TextRenderer.DrawText(graphics, _format(point.Value), Font, new Rectangle(Width - amountWidth - 7, y, amountWidth, rowHeight),
                ForeColor, TextFormatFlags.Right | TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);
            _hitAreas.Add((new Rectangle(8, y, Width - 16, rowHeight), $"{point.Label}: {_format(point.Value)}"));
        }
    }

    private void DrawPie(Graphics graphics)
    {
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        decimal total = _series.Sum(point => point.Value);
        int size = Math.Min(Width / 2 - 10, Height - 57);
        var pie = new Rectangle(12, 43, size, size);
        int legendX = Width / 2 + 12;
        int rowHeight = Math.Max(18, Math.Min(28, (Height - 47) / _series.Length));
        float angle = -90;
        for (int i = 0; i < _series.Length; i++)
        {
            var point = _series[i];
            float sweep = (float)(point.Value / total * 360);
            using var brush = new SolidBrush(Palette[i]);
            graphics.FillPie(brush, pie, angle, sweep);
            angle += sweep;
            int y = 43 + i * rowHeight;
            graphics.FillRectangle(brush, legendX, y + 4, 11, 11);
            string label = $"{point.Label} ({_format(point.Value)})";
            var bounds = new Rectangle(legendX + 15, y, Math.Max(1, Width - legendX - 22), rowHeight);
            TextRenderer.DrawText(graphics, label, Font, bounds, ForeColor, TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);
            _hitAreas.Add((bounds, label));
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        string? text = _hitAreas.FirstOrDefault(area => area.Bounds.Contains(e.Location)).Text;
        if (text == _currentTip) return;
        _currentTip = text;
        _tip.SetToolTip(this, text);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _tip.Dispose();
        base.Dispose(disposing);
    }
}
