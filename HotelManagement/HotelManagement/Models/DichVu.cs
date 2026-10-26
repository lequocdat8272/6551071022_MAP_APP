using System;
using System.Collections.Generic;

namespace HotelManagement.Models;

public partial class DichVu
{
    public string MaDv { get; set; } = null!;

    public string TenDv { get; set; } = null!;

    public decimal DonGia { get; set; }

    public int? SlconLai { get; set; }

    public string LoaiDv { get; set; } = null!;

    public bool? DaXoa { get; set; }

    public virtual ICollection<Ctdv> Ctdvs { get; set; } = new List<Ctdv>();
}
