using System;
using System.Collections.Generic;

namespace HotelManagement.Models;

public partial class PhieuThue
{
    public string MaPt { get; set; } = null!;

    public DateTime NgPt { get; set; }

    public string MaKh { get; set; } = null!;

    public string MaNv { get; set; } = null!;

    public bool? DaXoa { get; set; }

    public virtual ICollection<Ctdp> Ctdps { get; set; } = new List<Ctdp>();

    public virtual KhachHang MaKhNavigation { get; set; } = null!;

    public virtual NhanVien MaNvNavigation { get; set; } = null!;
}
