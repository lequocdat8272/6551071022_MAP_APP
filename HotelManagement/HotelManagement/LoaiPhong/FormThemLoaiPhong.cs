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
    public partial class FormThemLoaiPhong : Form
    {
        public FormThemLoaiPhong()
        {
            InitializeComponent();

            Text = "Thêm mới loại phòng";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            AcceptButton = btnThemMoi;
            CancelButton = btnThoat;

            txtMaLoaiPhong.MaxLength = 5;
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

        private async void btnThemMoi_Click(object sender, EventArgs e)
        {
            string maLPH = txtMaLoaiPhong.Text.Trim().ToUpperInvariant();
            string tenLPH = txtTenLP.Text.Trim();

            if (maLPH.Length == 0)
            {
                MessageBox.Show("Vui lòng nhập mã loại phòng.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtMaLoaiPhong.Focus();
                return;
            }

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

            if (!btnThemMoi.Enabled) return;
            btnThemMoi.Enabled = false;
            btnThoat.Enabled = false;
            try
            {
                using var db = new HotelManagementContext();

                if (await db.LoaiPhongs.AnyAsync(l => l.MaLph == maLPH))
                {
                    MessageBox.Show("Mã loại phòng đã tồn tại. Vui lòng nhập mã khác.", "Thông báo",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtMaLoaiPhong.Focus();
                    return;
                }

                db.LoaiPhongs.Add(new Models.LoaiPhong
                {
                    MaLph = maLPH,
                    TenLph = tenLPH,
                    SoGiuong = soGiuong,
                    SoNguoiToiDa = soNguoiToiDa,
                    GiaNgay = giaNgay,
                    GiaGio = giaGio
                });

                int result = await db.SaveChangesAsync();
                if (result > 0)
                {
                    MessageBox.Show("Thêm loại phòng thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Không thể thêm loại phòng.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (DbUpdateException ex)
            {
                MessageBox.Show("Lỗi khi thêm loại phòng: " + (ex.InnerException?.Message ?? ex.Message),
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi thêm loại phòng: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (!IsDisposed)
                {
                    btnThemMoi.Enabled = true;
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
