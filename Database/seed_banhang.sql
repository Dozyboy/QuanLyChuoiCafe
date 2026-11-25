-- Dữ liệu mẫu phân hệ Bán hàng. Chạy lại nhiều lần không sinh bản ghi trùng.
INSERT OR IGNORE INTO roles (id, name) VALUES (1, 'Quản lý chuỗi'), (2, 'Thu ngân'), (3, 'Thủ kho');

INSERT OR IGNORE INTO branches (id, name, address) VALUES
    (1, 'Chi nhánh Bách Khoa', '1 Đại Cồ Việt, Hà Nội'),
    (2, 'Chi nhánh Cầu Giấy', '100 Xuân Thủy, Hà Nội');

INSERT OR IGNORE INTO categories (id, name) VALUES (1, 'Cà phê'), (2, 'Trà sữa'), (3, 'Trà trái cây');

INSERT OR IGNORE INTO products (id, name, category_id) VALUES
    (1, 'Cà phê đen', 1), (2, 'Cà phê sữa', 1), (3, 'Bạc xỉu', 1),
    (4, 'Trà sữa trân châu', 2), (5, 'Trà sữa matcha', 2),
    (6, 'Trà đào cam sả', 3), (7, 'Trà vải', 3);

INSERT OR IGNORE INTO product_sizes (id, product_id, name, price) VALUES
    (1, 1, 'M', 25000), (2, 1, 'L', 30000),
    (3, 2, 'M', 29000), (4, 2, 'L', 35000),
    (5, 3, 'M', 32000), (6, 3, 'L', 38000),
    (7, 4, 'M', 39000), (8, 4, 'L', 45000),
    (9, 5, 'M', 42000), (10, 5, 'L', 49000),
    (11, 6, 'M', 39000), (12, 6, 'L', 45000),
    (13, 7, 'M', 35000), (14, 7, 'L', 41000);

INSERT OR IGNORE INTO customers (id, name, phone, points) VALUES
    (1, 'Khách mẫu An', '0900000001', 12),
    (2, 'Khách mẫu Bình', '0900000002', 40),
    (3, 'Khách mẫu Chi', '0900000003', 0);

INSERT OR IGNORE INTO promotions (id, name, type, value, min_order, date_from, date_to) VALUES
    (1, 'Giảm 10% đơn từ 100.000 đ', 0, 10, 100000, '2026-01-01', '2027-12-31'),
    (2, 'Giảm 20.000 đ đơn từ 150.000 đ', 1, 20000, 150000, '2026-01-01', '2027-12-31'),
    (3, 'Mua 2 tặng 1', 2, 2, 0, '2026-01-01', '2027-12-31');
