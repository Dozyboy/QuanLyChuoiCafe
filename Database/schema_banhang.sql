-- Phân hệ Bán hàng, Quản trị, Khách hàng (TV1). Chuẩn 3NF, tiền lưu số nguyên (đồng).
CREATE TABLE IF NOT EXISTS roles (
    id      INTEGER PRIMARY KEY,
    name    TEXT NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS branches (
    id      INTEGER PRIMARY KEY AUTOINCREMENT,
    name    TEXT NOT NULL UNIQUE,
    address TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS users (
    id            INTEGER PRIMARY KEY AUTOINCREMENT,
    username      TEXT NOT NULL UNIQUE COLLATE NOCASE,
    full_name     TEXT NOT NULL,
    password_hash TEXT NOT NULL,
    role_id       INTEGER NOT NULL REFERENCES roles(id),
    branch_id     INTEGER NOT NULL REFERENCES branches(id),
    is_active     INTEGER NOT NULL DEFAULT 1
);

CREATE TABLE IF NOT EXISTS categories (
    id   INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS products (
    id          INTEGER PRIMARY KEY AUTOINCREMENT,
    name        TEXT NOT NULL UNIQUE,
    category_id INTEGER NOT NULL REFERENCES categories(id),
    is_active   INTEGER NOT NULL DEFAULT 1
);

CREATE TABLE IF NOT EXISTS product_sizes (
    id         INTEGER PRIMARY KEY AUTOINCREMENT,
    product_id INTEGER NOT NULL REFERENCES products(id),
    name       TEXT NOT NULL,
    price      INTEGER NOT NULL CHECK (price > 0),
    UNIQUE (product_id, name)
);

CREATE TABLE IF NOT EXISTS customers (
    id     INTEGER PRIMARY KEY AUTOINCREMENT,
    name   TEXT NOT NULL,
    phone  TEXT NOT NULL UNIQUE,
    points INTEGER NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS promotions (
    id        INTEGER PRIMARY KEY AUTOINCREMENT,
    name      TEXT NOT NULL UNIQUE,
    type      INTEGER NOT NULL,
    value     INTEGER NOT NULL,
    min_order INTEGER NOT NULL DEFAULT 0,
    date_from TEXT NOT NULL,
    date_to   TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS shifts (
    id           INTEGER PRIMARY KEY AUTOINCREMENT,
    branch_id    INTEGER NOT NULL REFERENCES branches(id),
    cashier_id   INTEGER NOT NULL REFERENCES users(id),
    opened_at    TEXT NOT NULL,
    opening_cash INTEGER NOT NULL,
    closed_at    TEXT,
    counted_cash INTEGER
);

-- Mỗi thu ngân chỉ có tối đa một ca đang mở.
CREATE UNIQUE INDEX IF NOT EXISTS ux_shifts_open ON shifts(cashier_id) WHERE closed_at IS NULL;

CREATE TABLE IF NOT EXISTS orders (
    id            INTEGER PRIMARY KEY AUTOINCREMENT,
    shift_id      INTEGER NOT NULL REFERENCES shifts(id),
    customer_id   INTEGER REFERENCES customers(id),
    status        INTEGER NOT NULL,
    discount_name TEXT NOT NULL,
    discount      INTEGER NOT NULL,
    cancel_reason TEXT,
    cancelled_at  TEXT
);

CREATE TABLE IF NOT EXISTS order_lines (
    order_id        INTEGER NOT NULL REFERENCES orders(id),
    product_size_id INTEGER NOT NULL REFERENCES product_sizes(id),
    unit_price      INTEGER NOT NULL,
    quantity        INTEGER NOT NULL CHECK (quantity BETWEEN 1 AND 100),
    PRIMARY KEY (order_id, product_size_id)
);

CREATE TABLE IF NOT EXISTS payments (
    order_id        INTEGER PRIMARY KEY REFERENCES orders(id),
    method          INTEGER NOT NULL,
    amount_due      INTEGER NOT NULL,
    amount_received INTEGER NOT NULL,
    paid_at         TEXT NOT NULL
);
