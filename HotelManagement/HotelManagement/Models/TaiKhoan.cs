using System;
using System.Collections.Generic;

namespace HotelManagement.Models;

public partial class TaiKhoan
{
    public string TenTk { get; set; } = null!;

    public string Password { get; set; } = null!;

    public int CapDoQuyen { get; set; }

    public string MaNv { get; set; } = null!;

    public bool? DaXoa { get; set; }

    public virtual NhanVien MaNvNavigation { get; set; } = null!;
}
