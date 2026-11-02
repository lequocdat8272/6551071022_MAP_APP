using ClosedXML.Excel;

namespace HotelManagement.Services;

public static class ExcelExport
{
    public static void SaveVisibleRows(DataGridView grid, string outputPath)
    {
        var rows = grid.Rows.Cast<DataGridViewRow>().Where(r => r.Visible && !r.IsNewRow).ToList();
        if (rows.Count == 0) throw new InvalidOperationException("Không có dữ liệu để xuất!");
        var columns = grid.Columns.Cast<DataGridViewColumn>().Where(c => c.Visible && c is not DataGridViewImageColumn).ToList();
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Danh sách");
        for (int c = 0; c < columns.Count; c++)
        {
            sheet.Cell(1, c + 1).Value = columns[c].HeaderText;
            for (int r = 0; r < rows.Count; r++)
            {
                var cell = sheet.Cell(r + 2, c + 1);
                switch (rows[r].Cells[columns[c].Index].Value)
                {
                    case decimal number: cell.Value = number; cell.Style.NumberFormat.Format = "#,##0.####"; break;
                    case int number: cell.Value = number; break;
                    case DateTime date: cell.Value = date; cell.Style.NumberFormat.Format = "dd/MM/yyyy"; break;
                    default: cell.Value = rows[r].Cells[columns[c].Index].Value?.ToString() ?? ""; break;
                }
            }
        }
        sheet.Row(1).Style.Font.Bold = true;
        sheet.SheetView.FreezeRows(1);
        sheet.RangeUsed()!.SetAutoFilter();
        sheet.Columns().AdjustToContents();
        workbook.SaveAs(outputPath);
    }
}
