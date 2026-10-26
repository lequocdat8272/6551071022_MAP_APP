using System;
using System.Collections.Generic;

namespace HotelManagement.Models;

public partial class KhachHang
{
    public string MaKh { get; set; } = null!;

    public string TenKh { get; set; } = null!;

    public string? Sdt { get; set; }

    public string CccdPassport { get; set; } = null!;

    public string QuocTich { get; set; } = null!;

    public string GioiTinh { get; set; } = null!;

    public bool? DaXoa { get; set; }

    public virtual ICollection<PhieuThue> PhieuThues { get; set; } = new List<PhieuThue>();
}
