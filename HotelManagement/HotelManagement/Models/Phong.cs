using System;
using System.Collections.Generic;

namespace HotelManagement.Models;

public partial class Phong
{
    public string MaPh { get; set; } = null!;

    public string Ttph { get; set; } = null!;

    public string Ttdd { get; set; } = null!;

    public string? GhiChu { get; set; }

    public string MaLph { get; set; } = null!;

    public bool? DaXoa { get; set; }

    public virtual ICollection<Ctdp> Ctdps { get; set; } = new List<Ctdp>();

    public virtual LoaiPhong MaLphNavigation { get; set; } = null!;
}
