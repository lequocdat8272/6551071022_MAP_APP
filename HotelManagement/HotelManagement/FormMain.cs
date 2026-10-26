using HotelManagement.DangNhap;
using HotelManagement.SoDoPhong;
using HotelManagement.Phong;
using HotelManagement.LoaiPhong;

namespace HotelManagement
{
    public partial class FormMain : Form
    {
        public FormMain()
        {
            InitializeComponent();
            Text = "Quản lý khách sạn";
            AutoScroll = false;
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1080, 720);
            WindowState = FormWindowState.Maximized;
            panel4.AutoScroll = true;
            ClientSizeChanged += (_, _) => CapNhatBoCuc();
            CapNhatBoCuc();
            HienThiThongTinNguoiDung();

            Load += FormMain_Load;
            linkLabel1.LinkClicked += linkLabel1_LinkClicked;
            btnTrangChu.Click += btnTrangChu_Click;
            btnSoDoPhong.Click += btnSoDoPhong_Click;
            btnQuanLyPhong.Click += btnQuanLyPhong_Click;
            button4.Click += button4_Click;
            button13.Click += button13_Click;
            button5.Click += button5_Click;
            button7.Click += button7_Click;
            button6.Click += button6_Click;
            button12.Click += button12_Click;
            button11.Click += button11_Click;
            button9.Click += button9_Click;
            button10.Click += button10_Click;
        }

        private void CapNhatBoCuc()
        {
            panel1.Width = ClientSize.Width;
            panel4.Height = Math.Max(100, ClientSize.Height - panel4.Top);
            foreach (var button in panel4.Controls.OfType<Button>())
            {
                button.Left = 0;
                button.Width = panel4.Width - SystemInformation.VerticalScrollBarWidth - 4;
            }
            panelMain.Size = new Size(Math.Max(300, ClientSize.Width - panelMain.Left - 12),
                Math.Max(200, ClientSize.Height - panelMain.Top - 12));
        }

        private void FormMain_Load(object? sender, EventArgs e)
        {
            HienThiTrangChu();
        }

        private void HienThiThongTinNguoiDung()
        {
            lblTenNguoiDung.Text = FormDangNhap.TenNV_DangNhap;
            lblTenNguoiDung.Left = (panel2.Width - lblTenNguoiDung.PreferredWidth) / 2;
        }

        private void linkLabel1_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e)
        {
            if (MessageBox.Show("Bạn có chắc muốn đăng xuất?", "Đăng xuất",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            this.Hide();
            using (var formLogin = new FormDangNhap())
            {
                if (formLogin.ShowDialog() == DialogResult.OK)
                {
                    HienThiThongTinNguoiDung();
                    HienThiTrangChu();
                    this.Show();
                }
                else
                {
                    this.Close();
                }
            }
        }

        private void btnTrangChu_Click(object? sender, EventArgs e)
        {
            HienThiTrangChu();
        }

        private void btnSoDoPhong_Click(object? sender, EventArgs e)
        {
            LoadFormCon(new FormSoDoPhong());
        }

        private void btnQuanLyPhong_Click(object? sender, EventArgs e)
        {
            LoadFormCon(new FormDanhSachPhong());
        }

        private void button4_Click(object? sender, EventArgs e)
        {
            LoadFormCon(new FormDanhSachLoaiPhong());
        }

        private void button13_Click(object? sender, EventArgs e)
        {
            HienThiDangPhatTrien("Thống kê");
        }

        private void button5_Click(object? sender, EventArgs e)
        {
            HienThiDangPhatTrien("Danh sách tài khoản");
        }

        private void button7_Click(object? sender, EventArgs e)
        {
            HienThiDangPhatTrien("Danh sách nhân viên");
        }

        private void button6_Click(object? sender, EventArgs e)
        {
            HienThiDangPhatTrien("Danh sách khách hàng");
        }

        private void button12_Click(object? sender, EventArgs e)
        {
            HienThiDangPhatTrien("Danh sách tiện nghi");
        }

        private void button11_Click(object? sender, EventArgs e)
        {
            HienThiDangPhatTrien("Danh sách dịch vụ");
        }

        private void button9_Click(object? sender, EventArgs e)
        {
            HienThiDangPhatTrien("Danh sách đặt phòng");
        }

        private void button10_Click(object? sender, EventArgs e)
        {
            HienThiDangPhatTrien("Danh sách hóa đơn");
        }

        private void XoaNoiDungPanelMain()
        {
            foreach (Control c in panelMain.Controls.Cast<Control>().ToList())
            {
                panelMain.Controls.Remove(c);
                c.Dispose();
            }
        }

        private void LoadUserControl(UserControl uc)
        {
            XoaNoiDungPanelMain();
            uc.Dock = DockStyle.Fill;
            panelMain.Controls.Add(uc);
            uc.BringToFront();
        }

        private void LoadFormCon(Form formCon)
        {
            XoaNoiDungPanelMain();
            formCon.TopLevel = false;
            formCon.FormBorderStyle = FormBorderStyle.None;
            formCon.Dock = DockStyle.Fill;
            panelMain.Controls.Add(formCon);
            formCon.Show();
        }

        private void HienThiTrangChu()
        {
            if (panelMain.Controls.Count == 1 && panelMain.Controls[0] is FormTrangChu)
                return;

            LoadUserControl(new FormTrangChu());
        }

        private void HienThiDangPhatTrien(string tenChucNang)
        {
            XoaNoiDungPanelMain();
            panelMain.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = tenChucNang + Environment.NewLine + "(chức năng đang được xây dựng)",
                Font = new Font("Microsoft Sans Serif", 16F, FontStyle.Italic),
                ForeColor = Color.DarkCyan,
                TextAlign = ContentAlignment.MiddleCenter
            });
        }
    }
}
