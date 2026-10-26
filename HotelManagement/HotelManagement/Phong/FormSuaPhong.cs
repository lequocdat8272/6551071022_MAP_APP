using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using HotelManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Phong
{
    public partial class FormSuaPhong : Form
    {
        private string? maPhong;

        public FormSuaPhong()
        {
            InitializeComponent();

            Text = "Cập nhật thông tin phòng";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            AcceptButton = btnCapNhat;
            CancelButton = btnThoat;

            txtGhiChu.MaxLength = 100;

            cbTrangThaiPhong.DropDownStyle = ComboBoxStyle.DropDownList;
            cbTinhTrangDonDep.DropDownStyle = ComboBoxStyle.DropDownList;
            cbLoaiPhong.DropDownStyle = ComboBoxStyle.DropDownList;
        }

        public FormSuaPhong(string maPhong) : this()
        {
            this.maPhong = maPhong;

            Text = $"Cập nhật phòng {maPhong}";
            label5.Text = $"Cập nhật thông tin phòng {maPhong}";

            Load += async (_, _) =>
            {
                btnCapNhat.Enabled = false;
                panel1.Enabled = false;
                if (await LoadLoaiPhongAsync() && await LoadThongTinPhongAsync() && !IsDisposed)
                {
                    panel1.Enabled = true;
                    btnCapNhat.Enabled = true;
                }
            };
        }

        private async void btnCapNhat_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(maPhong))
            {
                MessageBox.Show("Chưa xác định được phòng cần cập nhật.", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string trangThai = cbTrangThaiPhong.Text;
            string donDep = cbTinhTrangDonDep.Text;
            string? maLPH = cbLoaiPhong.SelectedValue?.ToString();
            string ghiChu = txtGhiChu.Text.Trim();

            if (string.IsNullOrEmpty(maLPH))
            {
                MessageBox.Show("Vui lòng chọn loại phòng.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cbLoaiPhong.Focus();
                return;
            }

            if (trangThai.Length == 0 || donDep.Length == 0)
            {
                MessageBox.Show("Vui lòng chọn trạng thái phòng và tình trạng dọn dẹp.", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!btnCapNhat.Enabled) return;
            btnCapNhat.Enabled = false;
            btnThoat.Enabled = false;
            try
            {
                using var db = new HotelManagementContext();

                var phong = await db.Phongs.FirstOrDefaultAsync(p => p.MaPh == maPhong && p.DaXoa != true);

                if (phong == null)
                {
                    MessageBox.Show("Không tìm thấy phòng cần cập nhật (có thể đã bị xóa).", "Lỗi",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                phong.Ttph = trangThai;
                phong.Ttdd = donDep;
                phong.MaLph = maLPH;
                phong.GhiChu = ghiChu.Length == 0 ? null : ghiChu;

                int rows = await db.SaveChangesAsync();

                if (rows > 0)
                {
                    MessageBox.Show("Cập nhật thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                MessageBox.Show("Lỗi khi cập nhật phòng: " + (ex.InnerException?.Message ?? ex.Message),
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi cập nhật phòng: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        private async Task<bool> LoadLoaiPhongAsync()
        {
            try
            {
                using var db = new HotelManagementContext();

                var dsLoaiPhong = await db.LoaiPhongs
                    .AsNoTracking()
                    .OrderBy(l => l.MaLph)
                    .Select(l => new { l.MaLph, l.TenLph })
                    .ToListAsync();
                if (IsDisposed) return false;

                cbLoaiPhong.Items.Clear();
                cbLoaiPhong.DisplayMember = "TenLph";
                cbLoaiPhong.ValueMember = "MaLph";
                cbLoaiPhong.DataSource = dsLoaiPhong;
                return dsLoaiPhong.Count > 0;
            }
            catch (Exception ex)
            {
                if (!IsDisposed) MessageBox.Show("Lỗi tải danh sách loại phòng: " + ex.Message);
                return false;
            }
        }

        private async Task<bool> LoadThongTinPhongAsync()
        {
            try
            {
                using var db = new HotelManagementContext();

                var thongTin = await db.Phongs
                    .AsNoTracking()
                    .Where(p => p.MaPh == maPhong && p.DaXoa != true)
                    .Select(p => new { p.Ttph, p.Ttdd, p.MaLph, p.GhiChu })
                    .FirstOrDefaultAsync();
                if (IsDisposed) return false;

                if (thongTin == null)
                {
                    MessageBox.Show("Không tìm thấy phòng " + maPhong + ".", "Lỗi",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    btnCapNhat.Enabled = false;
                    return false;
                }

                cbTrangThaiPhong.SelectedIndex = cbTrangThaiPhong.FindStringExact(thongTin.Ttph);
                cbTinhTrangDonDep.SelectedIndex = cbTinhTrangDonDep.FindStringExact(thongTin.Ttdd);
                cbLoaiPhong.SelectedValue = thongTin.MaLph;
                txtGhiChu.Text = thongTin.GhiChu ?? "";
                return true;
            }
            catch (Exception ex)
            {
                if (!IsDisposed) MessageBox.Show("Lỗi tải thông tin phòng: " + ex.Message);
                btnCapNhat.Enabled = false;
                return false;
            }
        }
    }
}
