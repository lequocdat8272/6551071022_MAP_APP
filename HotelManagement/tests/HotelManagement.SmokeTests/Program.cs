using System.Diagnostics;
using System.Drawing.Imaging;
using System.Reflection;
using System.Text.RegularExpressions;
using HotelManagement.Common;
using HotelManagement.Models;
using HotelManagement.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

internal static class Program
{
    private static string _testConnection = "";
    private static string _masterConnection = "";
    private static readonly string DatabaseName = "HotelManagement_Lab2Smoke_" + Guid.NewGuid().ToString("N");
    private static int _checks;
    private static bool _databaseCreated;
    private static HotelManagementContext Db() => new(new DbContextOptionsBuilder<HotelManagementContext>().UseSqlServer(_testConnection).Options);

    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            InputTests();
            if (args.Contains("--input-only")) return 0;
            SetupDatabaseAsync().GetAwaiter().GetResult();
            DatabaseTestsAsync().GetAwaiter().GetResult();
            ApplicationConfiguration.Initialize();
            Environment.SetEnvironmentVariable("HOTELMANAGEMENT_CONNECTION_STRING", _testConnection);
            UiTests();
            Console.WriteLine($"PASS: {_checks} checks; temporary database and UI screenshots verified.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex.ToString()); return 1; }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(null);
            if (_databaseCreated) CleanupAsync().GetAwaiter().GetResult();
        }
    }
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception("FAIL: " + message);
        Console.WriteLine("PASS: " + message); _checks++;
    }
    private static void Reject(Action action, string label)
    {
        try { action(); }
        catch (ArgumentException) { Check(true, label); return; }
        throw new Exception("Expected invalid input: " + label);
    }
    private static void InputTests()
    {
        Lab2Input.Customer("Nguyễn A", "A123456", "", "Việt Nam", "Nam");
        Check(true, "Customer phone is optional; passports accept letters");
        Reject(() => Lab2Input.Customer("A", "123", "09x", "VN", "Nam"), "Reject non-numeric customer phone");
        Reject(() => Lab2Input.Employee("A", "Lễ tân", 100, "0900000000", "123456789012", DateTime.Today.AddYears(-17), "Nam", "HCM", "a@b.vn"), "Reject employee under 18");
        Reject(() => Lab2Input.Employee("A", "Lễ tân", 100, "0900000000", "123456789012", DateTime.Today.AddYears(-20), "Nam", "HCM", "bad-email"), "Reject malformed email");
        Check(Lab2Input.NextId(new[] { "KH001", "KH009", "other" }, "KH", 3, 5) == "KH010", "Generate next identifier");
        try { Lab2Input.NextId(new[] { "KH999" }, "KH", 3, 5); throw new Exception("Expected exhausted IDs"); }
        catch (InvalidOperationException) { Check(true, "Reject exhausted identifiers"); }
        var room = new BookingRoom("P101", 1, DateTime.Today.AddDays(1), DateTime.Today.AddDays(2));
        Reject(() => BookingService.ValidateRooms(Array.Empty<BookingRoom>()), "Reject empty reservation");
        Reject(() => BookingService.ValidateRooms(new[] { room, room }), "Reject duplicate selected room");
        Reject(() => BookingService.ValidateRooms(new[] { room with { CheckOut = room.CheckIn } }), "Reject invalid stay dates");
        Reject(() => BookingService.ValidateRooms(new[] { room with { Guests = 0 } }), "Reject zero guests");
        Reject(() => BookingService.ValidateRooms(new[] { room with { CheckIn = room.CheckIn.AddSeconds(30) } }), "Reject sub-minute timestamps");
    }

    private static async Task SetupDatabaseAsync()
    {
        await using var current = new HotelManagementContext();
        var builder = new SqlConnectionStringBuilder(current.Database.GetDbConnection().ConnectionString) { InitialCatalog = "master", ConnectTimeout = 5 };
        _masterConnection = builder.ConnectionString;
        await using var master = new SqlConnection(_masterConnection);
        await master.OpenAsync();
        await using (var command = new SqlCommand($"CREATE DATABASE [{DatabaseName}]", master)) await command.ExecuteNonQueryAsync();
        _databaseCreated = true;
        builder.InitialCatalog = DatabaseName; _testConnection = builder.ConnectionString;
        string script = await File.ReadAllTextAsync("Database.sql");
        script = script[..script.IndexOf("INSERT INTO NhanVien", StringComparison.Ordinal)];
        await using var connection = new SqlConnection(_testConnection); await connection.OpenAsync();
        foreach (string batch in Regex.Split(script, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(batch) || batch.TrimStart().StartsWith("CREATE DATABASE", StringComparison.OrdinalIgnoreCase)
                || batch.TrimStart().StartsWith("USE ", StringComparison.OrdinalIgnoreCase)) continue;
            await using var command = new SqlCommand(batch, connection); await command.ExecuteNonQueryAsync();
        }
        await using var db = Db();
        db.NhanViens.Add(new NhanVien { MaNv = "NV001", TenNv = "Nhân viên mẫu", ChucVu = "Lễ tân", Luong = 10000000,
            Sdt = "0900000000", Cccd = "123456789012", NgaySinh = new DateOnly(1995, 1, 1), GioiTinh = "Nam", DiaChi = "HCM", Email = "test@example.com", DaXoa = false });
        db.LoaiPhongs.Add(new LoaiPhong { MaLph = "NOR01", TenLph = "Phòng tiêu chuẩn", SoGiuong = 2, SoNguoiToiDa = 4, GiaNgay = 500000, GiaGio = 100000 });
        foreach (string id in new[] { "P101", "P102", "P103", "P104" }) db.Phongs.Add(new Phong { MaPh = id, MaLph = "NOR01", Ttph = "Bình thường", Ttdd = "Đã dọn dẹp", DaXoa = false });
        db.TienNghis.Add(new TienNghi { MaTn = "TN001", TenTn = "Tivi", DaXoa = false });
        db.DichVus.Add(new DichVu { MaDv = "DV001", TenDv = "Giặt ủi", LoaiDv = "Giặt ủi", DonGia = 50000, SlconLai = -1, DaXoa = false });
        db.Cttns.Add(new Cttn { MaLph = "NOR01", MaTn = "TN001", Sl = 1, DaXoa = false });
        await db.SaveChangesAsync();
        Check(true, "Create isolated schema with original SQL triggers and seed data");
    }

    private static async Task DatabaseTestsAsync()
    {
        var customer = new BookingCustomer("Khách mẫu", "B123456", "0901234567", "Việt Nam", "Nam");
        var room = new BookingRoom("P101", 2, DateTime.Today.AddDays(10).AddHours(14), DateTime.Today.AddDays(12).AddHours(12));
        var service = new BookingService(Db);
        var request = new BookingRequest("NV001", customer, new[] { room, room with { RoomId = "P102" } });
        string id = await service.SaveAsync(request);
        await using (var db = Db())
        {
            Check(await db.PhieuThues.CountAsync() == 1 && await db.Ctdps.CountAsync() == 2, "One rental with two room details");
            Check(await db.KhachHangs.CountAsync() == 1, "Create customer once");
            Check(await db.Ctdps.AllAsync(x => x.ThanhTien == 1000000), "SQL trigger computes correct room charge");
            Check(await db.Phongs.Where(x => x.MaPh == "P101" || x.MaPh == "P102").AllAsync(x => x.Ttph == "Đã đặt"), "Update room status after booking");
            Check(!await db.Phongs.Where(x => x.MaPh == "P101").AvailableDuring(room.CheckIn, room.CheckOut).AnyAsync(), "Exclude overlapping booked room");
            Check(await db.Phongs.Where(x => x.MaPh == "P101").AvailableDuring(room.CheckOut, room.CheckOut.AddDays(1)).AnyAsync(), "Allow adjacent non-overlapping reservation");
        }
        try { await service.SaveAsync(request with { Customer = customer with { Passport = "C123456" } }); throw new Exception("Expected conflict"); }
        catch (InvalidOperationException) { Check(true, "Reject overlapping reservation"); }
        try { await service.SaveAsync(request with { Rooms = new[] { room with { RoomId = "P103", Guests = 5 } } }); throw new Exception("Expected capacity error"); }
        catch (ArgumentException) { Check(true, "Reject room over capacity"); }
        // Lỗi sau SaveChanges đầu tiên phải rollback cả khách hàng và phiếu thuê.
        await using (var db = Db())
            await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER Lab2_RejectRoom ON CTDP AFTER INSERT AS BEGIN THROW 51000, 'Injected failure', 1; END");
        try { await service.SaveAsync(request with { Customer = customer with { Passport = "FAIL123" }, Rooms = new[] { room with { RoomId = "P103" } } }); throw new Exception("Expected database failure"); }
        catch (DbUpdateException) { Check(true, "Inject detail save failure"); }
        await using (var db = Db())
        {
            await db.Database.ExecuteSqlRawAsync("DROP TRIGGER Lab2_RejectRoom");
            Check(await db.PhieuThues.CountAsync() == 1 && !await db.KhachHangs.AnyAsync(x => x.CccdPassport == "FAIL123"), "Rollback rental and new customer after detail failure");
        }
        await service.SaveAsync(request with { Customer = customer with { Name = "Must not overwrite" }, Rooms = new[] { room with { RoomId = "P103" } } });
        await using (var db = Db()) Check((await db.KhachHangs.SingleAsync()).TenKh == customer.Name, "Reuse customer without overwriting profile");
        var concurrent = request with { Rooms = new[] { room with { RoomId = "P104" } } };
        async Task<bool> Attempt() { try { await service.SaveAsync(concurrent); return true; } catch (Exception ex) when (ex is InvalidOperationException or DbUpdateException or SqlException) { return false; } }
        var results = await Task.WhenAll(Attempt(), Attempt());
        Check(results.Count(x => x) == 1, "Concurrent overlapping attempts produce one booking");
        await using (var db = Db())
        {
            var detail = await db.Ctdps.SingleAsync(x => x.MaPh == "P104"); detail.TrangThai = "Đã hủy"; await db.SaveChangesAsync();
            Check(await db.Phongs.Where(x => x.MaPh == "P104").AvailableDuring(room.CheckIn, room.CheckOut).AnyAsync(), "Ignore cancelled reservation");
            var deletedCustomer = await db.KhachHangs.SingleAsync(); deletedCustomer.DaXoa = true; await db.SaveChangesAsync();
        }
        await service.SaveAsync(request with { Customer = customer with { Name = "Khôi phục" }, Rooms = new[] { room with { CheckIn = room.CheckOut, CheckOut = room.CheckOut.AddDays(2) } } });
        await using (var db = Db()) Check((await db.KhachHangs.SingleAsync()).DaXoa == false, "Restore soft-deleted customer during booking");
    }

    private static void UiTests()
    {
        Directory.CreateDirectory("output/qa");
        var forms = new Form[]
        {
            new HotelManagement.DichVu.FormDanhSachDichVu(), new HotelManagement.TienNghi.FormDanhSachTienNghi(),
            new HotelManagement.KhachHang.FormDanhSachKhachHang(), new HotelManagement.NhanVien.FormDanhSachNhanVien(),
            new HotelManagement.ChiTietTienNghi.FormDanhSachChiTietTienNghi("NOR01", "Phòng tiêu chuẩn"),
            new HotelManagement.DichVu.FormThemDichVu(), new HotelManagement.TienNghi.FormThemTienNghi(),
            new HotelManagement.KhachHang.FormThemKhachHang(), new HotelManagement.NhanVien.FormThemNhanVien(),
            new HotelManagement.ChiTietTienNghi.FormThemChiTietTienNghi("NOR01"), new HotelManagement.SoDoPhong.FormDatPhong("P101")
        };
        // Thêm tiện nghi chưa gắn để kiểm tra hộp thoại thêm chi tiết.
        using (var db = Db()) { db.TienNghis.Add(new TienNghi { MaTn = "TN002", TenTn = "Điều hòa", DaXoa = false }); db.SaveChanges(); }
        foreach (var form in forms)
        {
            using (form)
            {
                form.Opacity = 0; form.ShowInTaskbar = false; form.Show();
                var watch = Stopwatch.StartNew();
                while (watch.ElapsedMilliseconds < 900) { Application.DoEvents(); Thread.Sleep(10); }
                Check(!form.IsDisposed && form.Width > 0, "Open " + form.GetType().Name);
                using var bitmap = new Bitmap(form.Width, form.Height);
                form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
                bitmap.Save("output/qa/" + form.GetType().Name + ".png", ImageFormat.Png);
                if (form is FormDanhSach)
                {
                    var grid = Descendants(form).OfType<DataGridView>().Single();
                    Check(grid.Rows.Count > 0, "Load data " + form.GetType().Name);
                    string exportPath = "output/qa/" + form.GetType().Name + ".xlsx";
                    ExcelExporter.Write(grid, exportPath);
                    using (var workbook = new ClosedXML.Excel.XLWorkbook(exportPath))
                    {
                        var sheet = workbook.Worksheet(1);
                        Check(sheet.LastRowUsed()!.RowNumber() == grid.Rows.Count + 1 && sheet.Cell(2, 1).GetString() == grid.Rows[0].Cells[0].Value!.ToString(), "Excel rows and identifiers " + form.GetType().Name);
                        if (form is HotelManagement.DichVu.FormDanhSachDichVu)
                            Check(sheet.Cell(2, 3).DataType == ClosedXML.Excel.XLDataType.Number && sheet.Cell(2, 3).GetDouble() == 50000, "Excel price remains numeric");
                    }
                    var search = Descendants(form).OfType<TextBox>().Single();
                    search.Text = "NO_MATCH_ABC"; Check(grid.Rows.Cast<DataGridViewRow>().All(x => !x.Visible), "Filter " + form.GetType().Name);
                }
                form.Close();
            }
        }
        CrudUiTests();
    }
    private static void CrudUiTests()
    {
        void Edit(FormNhapLieu form, Action<Form> fill)
        {
            using (form)
            {
                form.Opacity = 0; form.ShowInTaskbar = false; form.Show();
                var watch = Stopwatch.StartNew();
                while (!Descendants(form).OfType<Button>().Any(x => x.Text == "Lưu" && x.Enabled) && watch.ElapsedMilliseconds < 5000)
                { Application.DoEvents(); Thread.Sleep(10); }
                fill(form);
                var task = (Task)form.GetType().GetMethod("SaveDataAsync", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(form, null)!;
                while (!task.IsCompleted) { Application.DoEvents(); Thread.Sleep(10); }
                task.GetAwaiter().GetResult(); form.Close();
            }
        }
        void Text(Form form, string name, string value) => Descendants(form).Single(x => x.Name == name).Text = value;
        void Number(Form form, string name, decimal value) => ((NumericUpDown)Descendants(form).Single(x => x.Name == name)).Value = value;
        Edit(new HotelManagement.DichVu.FormThemDichVu(), f =>
        {
            Text(f, "Mã dịch vụ", "DV002"); Text(f, "Tên dịch vụ", "Đưa đón sân bay");
            Number(f, "Đơn giá (VNĐ)", 250000); Text(f, "Loại dịch vụ", "Đưa đón");
        });
        Edit(new HotelManagement.DichVu.FormSuaDichVu { MaDichVu = "DV002" }, f => Number(f, "Đơn giá (VNĐ)", 300000));
        using (var db = Db()) Check(db.DichVus.Single(x => x.MaDv == "DV002").DonGia == 300000, "Add and edit service through forms");
        Edit(new HotelManagement.TienNghi.FormThemTienNghi(), f => { Text(f, "Mã tiện nghi", "TN003"); Text(f, "Tên tiện nghi", "Máy sấy"); });
        Edit(new HotelManagement.TienNghi.FormSuaTienNghi("TN003"), f => Text(f, "Tên tiện nghi", "Máy sấy tóc"));
        using (var db = Db()) Check(db.TienNghis.Single(x => x.MaTn == "TN003").TenTn == "Máy sấy tóc", "Add and edit amenity through forms");
        string customerId = "";
        Edit(new HotelManagement.KhachHang.FormThemKhachHang(), f =>
        {
            customerId = Descendants(f).Single(x => x.Name == "Mã khách hàng").Text;
            Text(f, "Họ tên", "Khách mới"); Text(f, "CCCD/Passport", "NEW123"); Text(f, "Quốc tịch", "Việt Nam"); Text(f, "Giới tính", "Nữ");
        });
        Edit(new HotelManagement.KhachHang.FormSuaKhachHang(customerId), f => Text(f, "Họ tên", "Khách đã sửa"));
        using (var db = Db()) Check(db.KhachHangs.Single(x => x.MaKh == customerId).TenKh == "Khách đã sửa", "Add customer with optional phone, generated ID, then edit");
        Edit(new HotelManagement.NhanVien.FormThemNhanVien(), f =>
        {
            Text(f, "Mã nhân viên", "NV002"); Text(f, "Họ tên", "Nhân viên mới"); Text(f, "Chức vụ", "Lễ tân");
            Number(f, "Lương (VNĐ)", 10000000); Text(f, "Số điện thoại", "0900000001"); Text(f, "CCCD", "123456789013");
            Text(f, "Giới tính", "Nữ"); Text(f, "Địa chỉ", "HCM"); Text(f, "Email", "second@example.com");
        });
        Edit(new HotelManagement.NhanVien.FormSuaNhanVien("NV002"), f => Number(f, "Lương (VNĐ)", 12000000));
        using (var db = Db()) Check(db.NhanViens.Single(x => x.MaNv == "NV002").Luong == 12000000, "Add and edit employee through forms");
        Edit(new HotelManagement.ChiTietTienNghi.FormThemChiTietTienNghi("NOR01"), f =>
        {
            ((ComboBox)Descendants(f).Single(x => x.Name == "Tiện nghi")).SelectedValue = "TN002";
            Number(f, "Số lượng", 2);
        });
        Edit(new HotelManagement.ChiTietTienNghi.FormSuaChiTietTienNghi("NOR01", "TN002"), f => Number(f, "Số lượng", 0));
        using (var db = Db()) Check(db.Cttns.Single(x => x.MaTn == "TN002").Sl == 0, "Add amenity to room type and set unavailable quantity");
        using (var db = Db()) { db.TaiKhoans.Add(new TaiKhoan { TenTk = "smoke", Password = "test", MaNv = "NV002", CapDoQuyen = 1, DaXoa = false }); db.SaveChanges(); }
        void Delete(FormDanhSach form, string id)
        {
            using (form)
            {
                var task = (Task)form.GetType().GetMethod("DeleteAsync", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(form, new object[] { id })!;
                while (!task.IsCompleted) { Application.DoEvents(); Thread.Sleep(10); }
                task.GetAwaiter().GetResult();
            }
        }
        Delete(new HotelManagement.DichVu.FormDanhSachDichVu(), "DV002");
        Delete(new HotelManagement.TienNghi.FormDanhSachTienNghi(), "TN003");
        Delete(new HotelManagement.KhachHang.FormDanhSachKhachHang(), customerId);
        Delete(new HotelManagement.NhanVien.FormDanhSachNhanVien(), "NV002");
        Delete(new HotelManagement.ChiTietTienNghi.FormDanhSachChiTietTienNghi("NOR01", "Test"), "TN002");
        using (var db = Db())
        {
            Check(db.DichVus.Single(x => x.MaDv == "DV002").DaXoa == true && db.TienNghis.Single(x => x.MaTn == "TN003").DaXoa == true
                && db.KhachHangs.Single(x => x.MaKh == customerId).DaXoa == true, "Soft-delete service, amenity and customer");
            Check(db.NhanViens.Single(x => x.MaNv == "NV002").DaXoa == true && db.TaiKhoans.Single(x => x.TenTk == "smoke").DaXoa == true, "Employee delete also disables associated account");
            Check(!db.Cttns.Any(x => x.MaTn == "TN002"), "Remove room-type amenity association");
        }
    }
    private static IEnumerable<Control> Descendants(Control parent) => parent.Controls.Cast<Control>().SelectMany(c => new[] { c }.Concat(Descendants(c)));
    private static async Task CleanupAsync()
    {
        if (!Regex.IsMatch(DatabaseName, "^HotelManagement_Lab2Smoke_[a-f0-9]{32}$")) throw new Exception("Unsafe cleanup target");
        SqlConnection.ClearAllPools();
        await using var connection = new SqlConnection(_masterConnection); await connection.OpenAsync();
        await using var command = new SqlCommand($"IF DB_ID('{DatabaseName}') IS NOT NULL BEGIN ALTER DATABASE [{DatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{DatabaseName}]; END", connection);
        await command.ExecuteNonQueryAsync();
        Console.WriteLine("Temporary database cleaned up.");
    }
}
