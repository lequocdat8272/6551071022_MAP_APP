using HotelManagement.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace HotelManagement.LoaiPhong
{
    public partial class FormDanhSachLoaiPhong : Form
    {
        private readonly CancellationTokenSource _lifetime = new();

        private readonly Image _iconGiuong = Properties.Resources.Giuong;
        private readonly Image _iconTienNghi = Properties.Resources.TienNghi;
        private readonly Image _iconSua = Properties.Resources.edit;

        public FormDanhSachLoaiPhong()
        {
            InitializeComponent();
            dtgDanhSachLoaiPhong.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            btnXuatExcel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnThemMoi.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            Disposed += (_, _) => _lifetime.Cancel();
        }

        private void txtTimKiem_TextChanged(object sender, EventArgs e)
        {
            LocTheoTuKhoa();
        }

        private void btnXuatExcel_Click(object sender, EventArgs e)
        {
            var cacDong = dtgDanhSachLoaiPhong.Rows.Cast<DataGridViewRow>()
                .Where(r => r.Visible && !r.IsNewRow)
                .ToList();

            if (cacDong.Count == 0)
            {
                MessageBox.Show("Không có dữ liệu để xuất!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var hopLuu = new SaveFileDialog())
            {
                hopLuu.Title = "Xuất danh sách loại phòng";
                hopLuu.Filter = "Excel Workbook (*.xlsx)|*.xlsx";
                hopLuu.FileName = $"DanhSachLoaiPhong_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";

                if (hopLuu.ShowDialog() != DialogResult.OK)
                    return;

                try
                {
                    using (var wb = new ClosedXML.Excel.XLWorkbook())
                    {
                        var ws = wb.Worksheets.Add("Danh sách loại phòng");

                        string[] tenCot = { "MaLPH", "TenLPH", "SoGiuong", "SoNguoiToiDa", "GiaNgay", "GiaGio" };

                        for (int c = 0; c < tenCot.Length; c++)
                            ws.Cell(1, c + 1).Value = dtgDanhSachLoaiPhong.Columns[tenCot[c]]!.HeaderText;
                        ws.Row(1).Style.Font.Bold = true;

                        for (int i = 0; i < cacDong.Count; i++)
                        {
                            for (int c = 0; c < tenCot.Length; c++)
                            {
                                var o = ws.Cell(i + 2, c + 1);
                                object? giaTri = cacDong[i].Cells[tenCot[c]].Value;

                                switch (giaTri)
                                {
                                    case decimal d:
                                        o.Value = d;
                                        o.Style.NumberFormat.Format = "#,##0";
                                        break;
                                    case int n:
                                        o.Value = n;
                                        break;
                                    default:
                                        o.Value = giaTri?.ToString() ?? "";
                                        break;
                                }
                            }
                        }

                        ws.Columns().AdjustToContents();
                        wb.SaveAs(hopLuu.FileName);
                    }

                    var traLoi = MessageBox.Show("Xuất dữ liệu thành công!\nBạn có muốn mở file ngay không?", "Thông báo",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                    if (traLoi == DialogResult.Yes)
                    {
                        System.Diagnostics.Process.Start(
                            new System.Diagnostics.ProcessStartInfo(hopLuu.FileName) { UseShellExecute = true });
                    }
                }
                catch (System.IO.IOException)
                {
                    MessageBox.Show("Không ghi được file. Có thể file đang được mở trong Excel, hãy đóng lại rồi thử lần nữa.",
                        "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi khi xuất Excel: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private async void btnThemMoi_Click(object sender, EventArgs e)
        {
            using (var formThem = new FormThemLoaiPhong())
            {
                if (formThem.ShowDialog() == DialogResult.OK)
                {
                    await LoadLoaiPhongAsync();
                }
            }
        }

        private async void dtgDanhSachLoaiPhong_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;

            var dong = dtgDanhSachLoaiPhong.Rows[e.RowIndex];
            string? maLPH = dong.Cells["MaLPH"].Value?.ToString();
            string? tenLPH = dong.Cells["TenLPH"].Value?.ToString();

            if (string.IsNullOrEmpty(maLPH))
                return;

            string tenCot = dtgDanhSachLoaiPhong.Columns[e.ColumnIndex].Name;

            if (tenCot == "colSua")
            {
                using (var formSua = new FormSuaLoaiPhong())
                {
                    formSua.MaLoaiPhong = maLPH;
                    if (formSua.ShowDialog() == DialogResult.OK)
                    {
                        await LoadLoaiPhongAsync();
                    }
                }
            }
            else if (tenCot == "colTienNghi")
            {
                MessageBox.Show(
                    $"Chức năng chi tiết tiện nghi của loại phòng {tenLPH} ({maLPH}) đang được xây dựng.",
                    "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private async void FormDanhSachLoaiPhong_Load(object sender, EventArgs e)
        {
            CauHinhLuoi();
            await LoadLoaiPhongAsync();
        }

        private void CauHinhLuoi()
        {
            if (dtgDanhSachLoaiPhong.Columns.Count > 0)
                return;

            dtgDanhSachLoaiPhong.ReadOnly = true;
            dtgDanhSachLoaiPhong.AllowUserToAddRows = false;
            dtgDanhSachLoaiPhong.AllowUserToDeleteRows = false;
            dtgDanhSachLoaiPhong.AllowUserToResizeRows = false;
            dtgDanhSachLoaiPhong.MultiSelect = false;
            dtgDanhSachLoaiPhong.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dtgDanhSachLoaiPhong.RowHeadersVisible = false;
            dtgDanhSachLoaiPhong.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dtgDanhSachLoaiPhong.RowTemplate.Height = 40;

            dtgDanhSachLoaiPhong.Columns.Add(TaoCotAnh("colIcon", "", _iconGiuong, 50));
            dtgDanhSachLoaiPhong.Columns.Add("MaLPH", "Mã loại");
            dtgDanhSachLoaiPhong.Columns.Add("TenLPH", "Tên loại");
            dtgDanhSachLoaiPhong.Columns.Add("SoGiuong", "Số giường");
            dtgDanhSachLoaiPhong.Columns.Add("SoNguoiToiDa", "Số người tối đa");
            dtgDanhSachLoaiPhong.Columns.Add("GiaNgay", "Giá ngày");
            dtgDanhSachLoaiPhong.Columns.Add("GiaGio", "Giá giờ");

            dtgDanhSachLoaiPhong.Columns["GiaNgay"]!.DefaultCellStyle.Format = "N0";
            dtgDanhSachLoaiPhong.Columns["GiaGio"]!.DefaultCellStyle.Format = "N0";
            dtgDanhSachLoaiPhong.Columns["GiaNgay"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dtgDanhSachLoaiPhong.Columns["GiaGio"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            dtgDanhSachLoaiPhong.Columns.Add(TaoCotAnh("colTienNghi", "Tiện nghi", _iconTienNghi, 60));
            dtgDanhSachLoaiPhong.Columns.Add(TaoCotAnh("colSua", "Cập nhật", _iconSua, 70));
        }

        private DataGridViewImageColumn TaoCotAnh(string ten, string tieuDe, Image anh, int doRong)
        {
            return new DataGridViewImageColumn
            {
                Name = ten,
                HeaderText = tieuDe,
                Image = anh,
                ImageLayout = DataGridViewImageCellLayout.Zoom,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                Width = doRong,
                Resizable = DataGridViewTriState.False,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };
        }

        private async Task LoadLoaiPhongAsync()
        {
            btnThemMoi.Enabled = false;
            btnXuatExcel.Enabled = false;
            dtgDanhSachLoaiPhong.Enabled = false;
            try
            {
                using var db = new HotelManagementContext();

                var dsLoaiPhong = await db.LoaiPhongs
                    .AsNoTracking()
                    .OrderBy(l => l.MaLph)
                    .Select(l => new
                    {
                        l.MaLph,
                        l.TenLph,
                        l.SoGiuong,
                        l.SoNguoiToiDa,
                        l.GiaNgay,
                        l.GiaGio
                    })
                    .ToListAsync(_lifetime.Token);

                if (IsDisposed) return;
                dtgDanhSachLoaiPhong.Rows.Clear();

                foreach (var l in dsLoaiPhong)
                {
                    dtgDanhSachLoaiPhong.Rows.Add(
                        _iconGiuong,
                        l.MaLph,
                        l.TenLph,
                        l.SoGiuong,
                        l.SoNguoiToiDa,
                        l.GiaNgay,
                        l.GiaGio,
                        _iconTienNghi,
                        _iconSua
                    );
                }

                LocTheoTuKhoa();
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                if (!IsDisposed) MessageBox.Show("Lỗi khi tải loại phòng: " + ex.Message);
            }
            finally
            {
                if (!IsDisposed)
                {
                    btnThemMoi.Enabled = true;
                    btnXuatExcel.Enabled = true;
                    dtgDanhSachLoaiPhong.Enabled = true;
                }
            }
        }

        private void LocTheoTuKhoa()
        {
            string keyword = txtTimKiem.Text.Trim();

            dtgDanhSachLoaiPhong.CurrentCell = null;

            foreach (DataGridViewRow row in dtgDanhSachLoaiPhong.Rows)
            {
                string ma = row.Cells["MaLPH"].Value?.ToString() ?? "";
                string ten = row.Cells["TenLPH"].Value?.ToString() ?? "";

                row.Visible = keyword.Length == 0
                              || ma.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                              || ten.Contains(keyword, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
