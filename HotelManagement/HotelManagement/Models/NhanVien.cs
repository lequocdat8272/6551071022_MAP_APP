using System;
using System.Collections.Generic;

namespace HotelManagement.Models;

public partial class NhanVien
{
    public string MaNv { get; set; } = null!;

    public string TenNv { get; set; } = null!;

    public string ChucVu { get; set; } = null!;

    public decimal Luong { get; set; }

    public string Sdt { get; set; } = null!;

    public string Cccd { get; set; } = null!;

    public DateOnly NgaySinh { get; set; }

    public string GioiTinh { get; set; } = null!;

    public string DiaChi { get; set; } = null!;

    public string? Email { get; set; }

    public bool? DaXoa { get; set; }

    public virtual ICollection<HoaDon> HoaDons { get; set; } = new List<HoaDon>();

    public virtual ICollection<PhieuThue> PhieuThues { get; set; } = new List<PhieuThue>();

    public virtual ICollection<TaiKhoan> TaiKhoans { get; set; } = new List<TaiKhoan>();
}
