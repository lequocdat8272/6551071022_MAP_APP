using HotelManagement.Controls;
using HotelManagement.DangNhap;
using HotelManagement.Services;

namespace HotelManagement.TaiKhoan;

internal sealed class FormDoiMatKhau : NhapLieuForm
{
    private readonly TextBox _old;
    private readonly TextBox _new;
    private readonly TextBox _confirm;

    internal FormDoiMatKhau() : base("Đổi mật khẩu")
    {
        ClientSize = new Size(610, 380);
        _old = TextField("Mật khẩu hiện tại", "MatKhauCu", 1000);
        _new = TextField("Mật khẩu mới", "MatKhauMoi", 100);
        _confirm = TextField("Nhập lại mật khẩu mới", "XacNhan", 100);
        _old.UseSystemPasswordChar = _new.UseSystemPasswordChar = _confirm.UseSystemPasswordChar = true;
    }

    protected override Task SaveDataAsync()
    {
        if (_new.Text != _confirm.Text) throw new InvalidOperationException("Hai mật khẩu mới không khớp.");
        return AccountService.ChangePasswordAsync(FormDangNhap.TenTK_DangNhap, _old.Text, _new.Text, Lifetime.Token);
    }
}
