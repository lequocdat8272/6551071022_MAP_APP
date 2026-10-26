using System;
using System.Collections.Generic;

namespace HotelManagement.Models;

public partial class Ctdv
{
    public string MaCtdp { get; set; } = null!;

    public string MaDv { get; set; } = null!;

    public decimal? DonGia { get; set; }

    public int Sl { get; set; }

    public bool? DaXoa { get; set; }

    public decimal? ThanhTien { get; set; }

    public virtual Ctdp MaCtdpNavigation { get; set; } = null!;

    public virtual DichVu MaDvNavigation { get; set; } = null!;
}
