using System;
using System.Collections.Generic;

namespace HotelManagement.Models;

public partial class TienNghi
{
    public string MaTn { get; set; } = null!;

    public string TenTn { get; set; } = null!;

    public bool? DaXoa { get; set; }

    public virtual ICollection<Cttn> Cttns { get; set; } = new List<Cttn>();
}
