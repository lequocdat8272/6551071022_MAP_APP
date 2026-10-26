using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using HotelManagement.Models;
using HotelManagement.Services;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.LoaiPhong
{
    public partial class FormSuaLoaiPhong : Form
    {
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string? MaLoaiPhong { get; set; }

        public FormSuaLoaiPhong()
        {
            InitializeComponent();
            Load += FormSuaLoaiPhong_Load;

            Text = "Cập nhật loại phòng";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            AcceptButton = btnCapNhat;
            CancelButton = btnThoat;

            // Mã loại phòng được giữ nguyên khi cập nhật.
            txtTenLP.MaxLength = 20;
            txtSoGiuong.MaxLength = 2;
            txtSoNguoiToiDa.MaxLength = 2;
            txtGiaNgay.MaxLength = 20;
            txtGiaGio.MaxLength = 20;

            txtSoGiuong.KeyPress += ChiChoNhapSo;
            txtSoNguoiToiDa.KeyPress += ChiChoNhapSo;
            txtGiaNgay.KeyPress += ChiChoNhapGia;
            txtGiaGio.KeyPress += ChiChoNhapGia;
        }

        private async void FormSuaLoaiPhong_Load(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(MaLoaiPhong))
            {
                MessageBox.Show("Chưa xác định được loại phòng cần sửa.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnCapNhat.Enabled = false;
                return;
            }

            try
            {
                btnCapNhat.Enabled = false;
                panel1.Enabled = false;
                using var db = new HotelManagementContext();

                var lp = await db.LoaiPhongs.AsNoTracking().FirstOrDefaultAsync(l => l.MaLph == MaLoaiPhong);
                if (IsDisposed) return;

                if (lp == null)
                {
                    MessageBox.Show("Không tìm thấy loại phòng " + MaLoaiPhong, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    btnCapNhat.Enabled = false;
                    return;
                }

                // Mã loại phòng được giữ nguyên khi cập nhật.
                txtTenLP.Text = lp.TenLph;
                txtSoGiuong.Text = lp.SoGiuong.ToString();
                txtSoNguoiToiDa.Text = lp.SoNguoiToiDa.ToString();
                txtGiaNgay.Text = lp.GiaNgay.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture);
                txtGiaGio.Text = lp.GiaGio.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture);
                panel1.Enabled = true;
                btnCapNhat.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải thông tin loại phòng: " + ex.Message);
                btnCapNhat.Enabled = false;
            }
        }

        private async void btnCapNhat_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(MaLoaiPhong))
                return;

            string tenLPH = txtTenLP.Text.Trim();

            if (tenLPH.Length == 0)
            {
                MessageBox.Show("Vui lòng nhập tên loại phòng.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtTenLP.Focus();
                return;
            }

            if (!int.TryParse(txtSoGiuong.Text.Trim(), out int soGiuong) || soGiuong <= 0)
            {
                MessageBox.Show("Số giường không hợp lệ!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtSoGiuong.Focus();
                return;
            }

            if (!int.TryParse(txtSoNguoiToiDa.Text.Trim(), out int soNguoiToiDa) || soNguoiToiDa <= 0)
            {
                MessageBox.Show("Số người tối đa không hợp lệ!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtSoNguoiToiDa.Focus();
                return;
            }

            if (!RoomTypeInput.TryPrice(txtGiaNgay.Text, out decimal giaNgay))
            {
                MessageBox.Show("Giá ngày phải lớn hơn 0, trong giới hạn MONEY và tối đa 4 số lẻ (dấu chấm).", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtGiaNgay.Focus();
                return;
            }

            if (!RoomTypeInput.TryPrice(txtGiaGio.Text, out decimal giaGio))
            {
                MessageBox.Show("Giá giờ phải lớn hơn 0, trong giới hạn MONEY và tối đa 4 số lẻ (dấu chấm).", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtGiaGio.Focus();
                return;
            }

            if (!btnCapNhat.Enabled) return;
            btnCapNhat.Enabled = false;
            btnThoat.Enabled = false;
            try
            {
                using var db = new HotelManagementContext();

                var lp = await db.LoaiPhongs.FirstOrDefaultAsync(l => l.MaLph == MaLoaiPhong);

                if (lp == null)
                {
                    MessageBox.Show("Không tìm thấy loại phòng để cập nhật.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                lp.TenLph = tenLPH;
                lp.SoGiuong = soGiuong;
                lp.SoNguoiToiDa = soNguoiToiDa;
                lp.GiaNgay = giaNgay;
                lp.GiaGio = giaGio;

                int rows = await db.SaveChangesAsync();

                if (rows > 0)
                {
                    MessageBox.Show("Cập nhật loại phòng thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Thông tin không thay đổi nên không có gì để cập nhật.", "Thông báo",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (DbUpdateException ex)
            {
                MessageBox.Show("Lỗi khi cập nhật loại phòng: " + (ex.InnerException?.Message ?? ex.Message),
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi cập nhật loại phòng: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (!IsDisposed)
                {
                    btnCapNhat.Enabled = true;
                    btnThoat.Enabled = true;
                }
            }
        }

        private void btnThoat_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void ChiChoNhapGia(object? sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsAsciiDigit(e.KeyChar) && e.KeyChar != '.') e.Handled = true;
        }

        private void ChiChoNhapSo(object? sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }
    }
}
