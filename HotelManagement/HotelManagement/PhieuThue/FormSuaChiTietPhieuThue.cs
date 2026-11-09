using HotelManagement.Controls;
using HotelManagement.Services;

namespace HotelManagement.PhieuThue;

public sealed class FormSuaChiTietPhieuThue : NhapLieuForm
{
    private readonly string _detailId;

    public FormSuaChiTietPhieuThue(string detailId, string room) : base("Xác nhận nhận phòng")
    {
        _detailId = detailId;
        ClientSize = new Size(580, 360);
        TextField("Mã chi tiết", "MaCTDP", 7, true).Text = detailId;
        TextField("Mã phòng", "MaPH", 5, true).Text = room;
        TextField("Trạng thái mới", "TrangThai", 20, true).Text = StayService.Occupied;
        SaveButton.Text = "Nhận phòng";
    }

    protected override Task SaveDataAsync() => StayService.CheckInAsync(_detailId, Lifetime.Token);
}
