# HotelManagement - Bài thực hành 2

Ứng dụng Windows Forms C# / .NET 10, SQL Server và Entity Framework Core. Dự án kế thừa mã LAB1 từ `Documents/Study/LTTQ/6551071022_MAP_APP/HotelManagement`, được sao chép vào workspace này để thực hiện mục IV, trang 103-234 của tài liệu LAB1-2-3.

## Các chức năng đã thực hiện

| Phần | Chức năng | Mở từ ứng dụng |
| --- | --- | --- |
| 1 | Dịch vụ: danh sách, thêm, sửa, xóa mềm, tìm kiếm, xuất Excel; số lượng không giới hạn; khôi phục mã dịch vụ đã xóa | Danh sách dịch vụ |
| 2 | Tiện nghi: danh sách, thêm, sửa, xóa mềm, tìm kiếm, xuất Excel | Danh sách tiện nghi |
| 3 | Khách hàng: tự sinh mã, thêm, sửa, xóa mềm, tìm kiếm, xuất Excel; kiểm tra CCCD/Passport trùng | Danh sách khách hàng |
| 4 | Nhân viên: thêm, sửa, xóa mềm và vô hiệu hóa tài khoản liên quan; kiểm tra tuổi, lương, email, CCCD và điện thoại; tìm kiếm, xuất Excel | Danh sách nhân viên |
| 5 | Tiện nghi theo loại phòng: thêm tiện nghi chưa gắn, sửa số lượng, xóa liên kết; hiển thị tình trạng sử dụng | Danh sách loại phòng → cột Tiện nghi |
| 6 | Đặt nhiều phòng, chọn ngày giờ và số người, tra cứu khách hàng theo CCCD/Passport, tự sinh mã phiếu và chi tiết; chống trùng lịch, kiểm tra sức chứa, lưu trong giao dịch | Sơ đồ phòng → Đặt phòng hoặc menu Đặt phòng |

Các form LAB2 có lớp giao diện dùng chung trong `Common` và các form cụ thể trong `DichVu`, `TienNghi`, `KhachHang`, `NhanVien`, `ChiTietTienNghi`, `SoDoPhong`. Giao diện được khởi tạo bằng C#; các form LAB1 và tài nguyên được giữ lại.

## Chạy ứng dụng

1. Mở `HotelManagement.slnx` bằng Visual Studio có workload .NET desktop development và SDK .NET 10.
2. Dùng SQL Server có cơ sở dữ liệu `HotelManagement`. Nếu chưa có, chạy `Database.sql` trong SSMS. Script tạo mới database, vì vậy chỉ chạy trên máy chưa có database cùng tên.
3. Cấu hình kết nối trong `HotelManagement/Models/Hotelmanagementcontext.custom.cs`, hoặc đặt biến môi trường `HOTELMANAGEMENT_CONNECTION_STRING`. Biến môi trường được ưu tiên.
4. Chạy project `HotelManagement`, đăng nhập bằng tài khoản có sẵn. Dữ liệu mẫu SQL cung cấp tài khoản `admin`, mật khẩu `1234`.

Ví dụ cấu hình Windows Authentication trong PowerShell:

```powershell
$env:HOTELMANAGEMENT_CONNECTION_STRING = 'Server=.;Database=HotelManagement;Integrated Security=True;TrustServerCertificate=True;'
dotnet run --project HotelManagement
```

Bản chạy đã publish nằm trong `output/app/HotelManagement.exe`. Giữ toàn bộ thư mục `output/app` khi sao chép sang máy khác; máy chạy cần .NET Desktop Runtime 10 và kết nối SQL Server.

## Quy tắc đặt phòng

- Chọn thời điểm nhận và trả, rồi bấm **Chọn** ở từng phòng trống. Ngày trả phải sau ngày nhận; thông tin thời gian được giữ riêng cho từng dòng đã chọn.
- **Số người/phòng** áp dụng khi bấm Chọn; không vượt sức chứa loại phòng. Bấm **Bỏ chọn** rồi chọn lại để thay đổi ngày giờ hoặc số người.
- Dùng **Tra cứu CCCD** để điền và khóa thông tin khách hàng đã tồn tại. Khi lưu, ứng dụng tra cứu lại; hồ sơ đang hoạt động được giữ nguyên, hồ sơ đã xóa được khôi phục với thông tin nhập mới.
- Lịch dùng khoảng `[nhận phòng, trả phòng)`, cho phép lượt tiếp theo bắt đầu đúng lúc lượt trước trả phòng. Phiếu đã hủy, đã xong hoặc đã xóa không chặn lịch. Phòng sửa chữa không được đặt; phòng đang có lịch vẫn có thể đặt vào khoảng thời gian khác.
- Giao dịch `Serializable` kiểm tra lại lịch trước khi lưu. Khi một bước thất bại, toàn bộ khách hàng mới, phiếu thuê và chi tiết trong lượt lưu được rollback. Nếu hai lượt lưu đồng thời bị SQL Server hủy một giao dịch do deadlock, giao diện báo lỗi để người dùng tải lại và thử lại.
- Chi tiết phòng được lưu từng bản ghi để tương thích trigger tính tiền trong SQL bài thực hành. Trạng thái phòng được cập nhật sau khi đặt thành công.
- Điện thoại khách hàng trong danh mục có thể để trống; khi đặt phòng cần nhập chữ số, tối đa 10 ký tự. Điện thoại nhân viên cần đủ 10 chữ số, CCCD nhân viên đủ 12 chữ số và tuổi ít nhất 18.

## SQL đi kèm

Bản `Database.sql` trong workspace đã sửa dấu phẩy trước khóa chính tổng hợp CTTN và bổ sung cột `CTDV.DonGia` để phù hợp với model/trigger. Không sửa hoặc tạo lại cơ sở dữ liệu HotelManagement hiện có trong quá trình thực hiện bài.

## Kiểm tra

```powershell
dotnet build HotelManagement.slnx
dotnet run --project tests/HotelManagement.SmokeTests -p:NuGetAudit=false
```

Bộ smoke test dùng chuỗi kết nối của ứng dụng để tạo database riêng có tên `HotelManagement_Lab2Smoke_<GUID>`, kiểm tra SQL trigger, đặt nhiều phòng, trùng lịch, hai lượt đặt đồng thời, rollback, khôi phục khách hàng và thao tác thêm/sửa/xóa các danh mục. Database tạm được xóa trong `finally`. Tài khoản chạy kiểm tra cần quyền tạo/xóa database trên SQL Server.

Ảnh giao diện phục vụ kiểm tra bố cục nằm trong `output/qa`. Chạy riêng kiểm tra đầu vào khi không có SQL Server:

```powershell
dotnet run --project tests/HotelManagement.SmokeTests -- --input-only
```

Các chức năng tài khoản, hóa đơn, thống kê thuộc các phần thực hành khác và vẫn giữ trạng thái của LAB1.
