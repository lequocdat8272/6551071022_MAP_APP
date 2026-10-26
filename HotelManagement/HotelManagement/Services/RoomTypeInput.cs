using System.Globalization;

namespace HotelManagement.Services;

public static class RoomTypeInput
{
    public const decimal MaxMoney = 922337203685477.5807m;

    // Dùng dấu chấm thập phân cố định và tối đa 4 chữ số lẻ như kiểu MONEY.
    public static bool TryPrice(string text, out decimal value)
        => decimal.TryParse(text.Trim(), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out value)
            && value > 0 && value <= MaxMoney && decimal.Round(value, 4) == value;
}
