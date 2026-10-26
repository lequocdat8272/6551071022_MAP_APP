using System;
using System.Collections.Generic;

namespace HotelManagement.Models;

public partial class LoaiPhong
{
    public string MaLph { get; set; } = null!;

    public string TenLph { get; set; } = null!;

    public int SoGiuong { get; set; }

    public int SoNguoiToiDa { get; set; }

    public decimal GiaNgay { get; set; }

    public decimal GiaGio { get; set; }

    public virtual ICollection<Cttn> Cttns { get; set; } = new List<Cttn>();

    public virtual ICollection<Phong> Phongs { get; set; } = new List<Phong>();
}
