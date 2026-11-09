using ClosedXML.Excel;

namespace HotelManagement.Common;

public static class ExcelExporter
{
    public static void Write(DataGridView grid, string path)
    {
        var rows = grid.Rows.Cast<DataGridViewRow>().Where(r => r.Visible && !r.IsNewRow).ToList();
        var columns = grid.Columns.Cast<DataGridViewColumn>().Where(c => c.Visible && c is DataGridViewTextBoxColumn).ToList();
        if (rows.Count == 0) throw new ArgumentException("Không có dữ liệu để xuất.");
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Danh sách");
        for (int c = 0; c < columns.Count; c++)
        {
            sheet.Cell(1, c + 1).Value = columns[c].HeaderText;
            for (int r = 0; r < rows.Count; r++)
            {
                object? value = rows[r].Cells[columns[c].Index].Value;
                var cell = sheet.Cell(r + 2, c + 1);
                switch (value)
                {
                    case decimal money: cell.Value = money; cell.Style.NumberFormat.Format = "#,##0"; break;
                    case int number: cell.Value = number; break;
                    case DateTime date: cell.Value = date; cell.Style.DateFormat.Format = "dd/MM/yyyy HH:mm"; break;
                    default: cell.Value = value?.ToString() ?? ""; break;
                }
            }
        }
        sheet.Row(1).Style.Font.Bold = true;
        sheet.SheetView.FreezeRows(1);
        sheet.Columns().AdjustToContents(8d, 50d);
        workbook.SaveAs(path);
    }
}
