# Phân công code - Nhóm 73

Đề tài: Quản lý chuỗi quán cà phê / trà sữa và định lượng nguyên liệu pha chế.
TV1: Minh (Bán hàng, Quản trị, Khách hàng). TV2: Hiền (Kho, Công thức).
Mốc nộp báo cáo PDF và mã nguồn: **30/12/2026**. Ngưỡng số lượng lấy từ checklist "Nhóm 02 người".

## 1. Hiện trạng (26/11/2026)

| Phần | Trạng thái |
|---|---|
| Khung solution 4 project, cấu hình ngoài, script CSDL tự chạy | Xong |
| TV1: đăng nhập, phân quyền, bán hàng, ca làm, danh mục món | Xong bản đầu |
| TV1: hủy đơn | Có ở tầng dịch vụ, chưa có form |
| Strategy (khuyến mãi), phía phát sự kiện của Observer | Xong |
| 24 ca test của TV1 | Đạt |
| TV2: toàn bộ phân hệ Kho | **Chưa làm** |

## 2. Việc cụ thể từng người

| Hạng mục | TV1 - Minh | TV2 - Hiền |
|---|---|---|
| Nhánh BFD | F1 Quản trị, phân quyền; F2 Bán hàng; F3 Khách hàng, khuyến mãi, ca | F4 Nguyên liệu, NCC, công thức; F5 Nhập, hủy, chuyển, kiểm kê; F6 Báo cáo kho và tổng hợp |
| Use case | Bán và thanh toán; hủy/hoàn đơn; mở/đóng ca; khuyến mãi và tích điểm | Lập phiếu nhập; khai báo công thức; kiểm kê điều chỉnh; chuyển kho/xuất hủy |
| Form nghiệp vụ | frm_nv_ban_hang (xong), frm_nv_ca_lam (xong), frm_nv_hoa_don, frm_nv_tich_diem | frm_nv_nhap_kho, frm_nv_xuat_huy_chuyen, frm_nv_kiem_ke, frm_nv_canh_bao_ton |
| Form danh mục | frm_dm_san_pham (xong), frm_dm_khach_hang, frm_dm_nguoi_dung, frm_dm_khuyen_mai | frm_dm_nguyen_lieu, frm_dm_nha_cung_cap, frm_dm_cong_thuc |
| Master-detail (từ 2 trở lên) | san_pham (xong), hoa_don, khach_hang | cong_thuc, nhap_kho, kiem_ke |
| Lớp | Role, User, Branch, Shift, Customer, Category, Product, ProductSize, Order, OrderLine, Payment, Promotion (xong) | Ingredient, Supplier, SupplierIngredient, RecipeLine, StockLevel, PurchaseReceipt, PurchaseLine, StockMovement, StockTake, StockTakeLine |
| Bảng | 12 bảng trong `Database/schema_banhang.sql` (xong) | 10 bảng trong `Database/schema_kho.sql` |
| Quan hệ n-n có thuộc tính | order_lines | recipe_lines, stock_levels, purchase_lines, supplier_ingredients |
| Quy tắc nghiệp vụ | QT1-QT7 (xong), QT8 phía Bán hàng (xong) | QT8 phía Kho, QT9-QT14 |
| Báo cáo | Doanh thu theo chi nhánh/ngày; món bán chạy | Tồn kho và cảnh báo; tiêu hao thực tế so định mức; lãi gộp (tổng hợp) |
| Design pattern | Strategy (xong); Observer - bên phát (xong) | Observer - bên nhận (trừ kho); Factory Method (xuất Excel/PDF) |
| Test (từ 9 mỗi người) | Có 24; thêm 1 ca giả lập Kho không dùng CSDL | Từ 9 unit test (2 ca giả lập Bán hàng) và test tích hợp trừ kho, rollback |
| Sơ đồ sequence/state (từ 2) | Sequence thanh toán; state đơn hàng | Sequence trừ kho; state phiếu nhập |
| Dữ liệu mẫu (cộng lại từ 120 dòng, 2-3 tháng) | `seed_banhang.sql`: đơn hàng nhiều tháng | `seed_kho.sql`: nguyên liệu, công thức, phiếu nhập |

## 3. Quy tắc nghiệp vụ

| Mã | Nội dung | Chủ trì |
|---|---|---|
| QT1 | Chỉ bán khi đang mở ca; mỗi thu ngân tối đa một ca mở | TV1 |
| QT2 | Đơn có ít nhất một món; số lượng mỗi dòng 1-100 | TV1 |
| QT3 | Đơn giá chốt tại thời điểm bán | TV1 |
| QT4 | Khuyến mãi đúng hạn, đủ đơn tối thiểu; tiền giảm không vượt tạm tính | TV1 |
| QT5 | Tiền mặt không thiếu; chuyển khoản đúng số tiền | TV1 |
| QT6 | Đơn đã trả không sửa; hủy cần quyền, lý do, trong ca đang mở | TV1 |
| QT7 | 10.000 đ được 1 điểm; hủy đơn thu hồi điểm | TV1 |
| QT8 | Thanh toán và trừ kho cùng một giao dịch; lỗi thì hoàn tác cả hai | Chung |
| QT9 | Món chưa có công thức thì không bán được | TV2 |
| QT10 | Tồn kho không được âm | TV2 |
| QT11 | Tồn dưới mức tối thiểu thì cảnh báo | TV2 |
| QT12 | Phiếu nhập đã duyệt không được sửa | TV2 |
| QT13 | Chênh lệch kiểm kê phải có lý do và quản lý duyệt | TV2 |
| QT14 | Hủy đơn đã pha chế thì ghi hao hụt, không hoàn kho | TV2 |

## 4. Hợp đồng ghép nối (Hiền đọc trước khi viết code)

- Bán hàng phát `OrderPaidEvent` và `OrderCancelledEvent` (`src/Domain/Events/OrderEvents.cs`). Mỗi dòng chỉ gồm `ProductSizeId` và `Quantity`.
- Kho cài `IOrderPaidHandler`, `IOrderCancelledHandler` và đăng ký tại `src/App/AppServices.cs`: `Publisher.Subscribe(...)`.
- Handler chạy **trong giao dịch thanh toán**. Thiếu nguyên liệu thì ném `ValidationException`, đơn tự hoàn tác.
- Để ghi cùng giao dịch: đặt repository của Kho trong project `Data`, ép `IUnitOfWork` về `SqliteUnitOfWork` để dùng `Connection`, `Transaction`.
- Kho không đọc hay ghi bảng của Bán hàng; Bán hàng không đụng bảng của Kho.
- Script mới đặt tên `schema_kho.sql`, `seed_kho.sql`: chương trình tự chạy.
- Menu: thêm một dòng `AddMenu(...)` với `Permission.ManageStock` trong `frm001_main`.
- Giao diện dùng lớp `Ui` để đồng nhất; tên form `frm_dm_*`, `frm_nv_*`.
- Test của Kho đặt trong `tests/Tests/Kho/`.

## 5. Phần chung

| Việc | Chủ trì | Người kia |
|---|---|---|
| ERD, biểu đồ lớp tổng, package diagram | Minh | Hiền bổ sung và kiểm tra phần Kho |
| README, đóng gói, chạy thử trên máy sạch | Minh | Hiền chạy thử |
| Video demo 3-5 phút | Hiền | Minh quay phần Bán hàng |
| Ghép báo cáo PDF | Hiền | Mỗi người viết phần của mình |
| Tự đánh giá checklist lần 2 | Cả hai | |

## 6. Lịch

| Tuần | TV1 - Minh | TV2 - Hiền |
|---|---|---|
| 26/11-02/12 | Nộp slide; form hóa đơn, hủy đơn | Cài môi trường, đọc hợp đồng, `schema_kho.sql`, lớp Kho |
| 03/12-09/12 | Khách hàng, tích điểm | Nguyên liệu, nhà cung cấp, công thức, handler trừ kho |
| 10/12-16/12 | Người dùng, khuyến mãi | Nhập kho, kiểm kê, xuất hủy/chuyển |
| 17/12-23/12 | Báo cáo doanh thu, test, sơ đồ | Báo cáo kho, Factory Method, test, sơ đồ |
| 24/12-29/12 | Dữ liệu mẫu, README | Dữ liệu mẫu, video, ghép báo cáo |
| 30/12 | Nộp | Nộp |

## 7. Quy trình Git

1. Mỗi người dùng tài khoản và email Git của chính mình.
2. Mỗi việc một nhánh: `feature/banhang-...` hoặc `feature/kho-...`.
3. Mở Pull Request, người kia xem rồi mới merge vào `main`.
4. Commit nhỏ, đều. Checklist yêu cầu từ 24 commit rải ít nhất 3 tuần, mỗi người 40-60% mã theo `git blame`. Không lùi ngày commit.
5. Trước khi push: `dotnet build` không cảnh báo, `dotnet test` đạt.
6. Khi vấn đáp, mỗi người phải tự giải thích được code và tự thêm một lớp mới vào pattern.
