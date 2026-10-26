using System;
using System.Collections.Generic;

namespace HotelManagement.Models;

public partial class Ctdp
{
    public string MaCtdp { get; set; } = null!;

    public int? SoNguoi { get; set; }

    public string MaPt { get; set; } = null!;

    public string MaPh { get; set; } = null!;

    public DateTime CheckIn { get; set; }

    public DateTime CheckOut { get; set; }

    public string TrangThai { get; set; } = null!;

    public decimal? DonGia { get; set; }

    public decimal? ThanhTien { get; set; }

    public bool? DaXoa { get; set; }

    public bool? TheoGio { get; set; }

    public virtual ICollection<Ctdv> Ctdvs { get; set; } = new List<Ctdv>();

    public virtual ICollection<HoaDon> HoaDons { get; set; } = new List<HoaDon>();

    public virtual Phong MaPhNavigation { get; set; } = null!;

    public virtual PhieuThue MaPtNavigation { get; set; } = null!;
}
