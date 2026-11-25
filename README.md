# Quản lý chuỗi quán cà phê / trà sữa

Bài tập lớn môn Lập trình hướng đối tượng (MI4090) - Nhóm 73.
C# .NET 8, Windows Forms, SQLite. Đang phát triển: phân hệ Bán hàng đã có bản đầu, phân hệ Kho chưa làm (xem [PHANCONG.md](PHANCONG.md)).

## Chạy thử

Yêu cầu: Windows và .NET 8 SDK.

```
dotnet build
dotnet test
dotnet run --project src/App
```

Cơ sở dữ liệu `cafe.db` được tạo tự động ở lần chạy đầu. Đường dẫn đổi trong `src/App/appsettings.json`.

Tài khoản demo (chỉ để chạy thử trên máy cá nhân), mật khẩu chung `Demo@123`:

| Tài khoản | Vai trò |
|---|---|
| `quanly` | Quản lý chuỗi |
| `thungan` | Thu ngân |
| `thukho` | Thủ kho |

Thứ tự dùng: đăng nhập, mở ca, bán hàng, đóng ca.

## Cấu trúc

| Thư mục | Nội dung |
|---|---|
| `src/Domain` | Lớp nghiệp vụ, dịch vụ, giao diện; không phụ thuộc UI hay SQL |
| `src/Data` | Truy cập SQLite bằng truy vấn tham số, giao dịch |
| `src/App` | Windows Forms |
| `tests/Tests` | Unit test và test tích hợp (xUnit) |
| `Database` | Script tạo bảng và dữ liệu mẫu |

## Design pattern đã cài

- **Strategy**: `src/Domain/Discounts` - các cách tính khuyến mãi.
- **Observer**: `src/Domain/Events` - thanh toán đơn phát sự kiện để phân hệ Kho trừ nguyên liệu.
