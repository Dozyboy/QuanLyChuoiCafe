using QuanLyChuoiCafe.Domain.Discounts;
using QuanLyChuoiCafe.Domain.Entities;

namespace QuanLyChuoiCafe.App.Forms;

/// <summary>Form nghiệp vụ bán hàng tại quầy: chọn món bên trái, đơn hàng và thanh toán bên phải.</summary>
public class frm_nv_ban_hang : Form
{
    private readonly AppServices _services;
    private readonly User _user;
    private Order _order;
    private Customer _customer;
    private bool _loading = true;

    private readonly ComboBox _category = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 130 };
    private readonly TextBox _keyword = new() { Width = 140, PlaceholderText = "Tìm tên món" };
    private readonly NumericUpDown _quantity = new() { Minimum = 1, Maximum = OrderLine.MaxQuantity, Value = 1, Width = 60 };
    private readonly DataGridView _menu = Ui.Grid(("name", "Món", 55, null), ("size", "Size", 15, null), ("price", "Giá (đ)", 30, "#,##0"));
    private readonly DataGridView _lines = Ui.Grid(("name", "Món", 40, null), ("size", "Size", 12, null),
        ("qty", "SL", 10, "#,##0"), ("price", "Đơn giá", 18, "#,##0"), ("total", "Thành tiền", 20, "#,##0"));
    private readonly ComboBox _promotion = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 290 };
    private readonly TextBox _phone = new() { Width = 140, MaxLength = 10, PlaceholderText = "SĐT khách" };
    private readonly Label _customerName = Ui.Label("Khách lẻ");
    private readonly RadioButton _cash = new() { Text = "Tiền mặt", Checked = true, AutoSize = true };
    private readonly RadioButton _transfer = new() { Text = "Chuyển khoản", AutoSize = true };
    private readonly NumericUpDown _received = new() { Maximum = 100_000_000, Increment = 1000, ThousandsSeparator = true, Width = 140 };
    private readonly Label _summary = Ui.Label("");
    private readonly Label _total = Ui.Label("", bold: true);

    public frm_nv_ban_hang(AppServices services, User user)
    {
        _services = services;
        _user = user;
        SuspendLayout();
        Ui.Prepare(this, "Bán hàng", 980, 640);

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));

        var left = Column(SizeType.AutoSize, SizeType.Percent, SizeType.AutoSize);
        left.Controls.Add(Ui.Row(_category, _keyword), 0, 0);
        left.Controls.Add(_menu, 0, 1);
        left.Controls.Add(Ui.Row(Ui.Label("Số lượng"), _quantity, Ui.Button("Thêm vào đơn", (_, _) => AddSelected(), true)), 0, 2);

        var right = Column(SizeType.AutoSize, SizeType.Percent, SizeType.AutoSize, SizeType.AutoSize,
            SizeType.AutoSize, SizeType.AutoSize, SizeType.AutoSize);
        right.Controls.Add(Ui.Row(Ui.Label("Đơn hàng", bold: true),
            Ui.Button("Bỏ món đã chọn", (_, _) => RemoveSelected()), Ui.Button("Đơn mới", (_, _) => StartOrder())), 0, 0);
        right.Controls.Add(_lines, 0, 1);
        right.Controls.Add(Ui.Row(Ui.Label("Khuyến mãi"), _promotion), 0, 2);
        right.Controls.Add(Ui.Row(_phone, Ui.Button("Tìm khách", (_, _) => FindCustomer()), _customerName), 0, 3);
        right.Controls.Add(Ui.Row(_summary, _total), 0, 4);
        right.Controls.Add(Ui.Row(_cash, _transfer, Ui.Label("Khách đưa"), _received), 0, 5);
        right.Controls.Add(Ui.Row(Ui.Button("Thanh toán", (_, _) => Checkout(), true)), 0, 6);

        root.Controls.Add(left, 0, 0);
        root.Controls.Add(right, 1, 0);
        Controls.Add(root);
        ResumeLayout();

        _category.SelectedIndexChanged += (_, _) => LoadMenu();
        _keyword.TextChanged += (_, _) => LoadMenu();
        _promotion.SelectedIndexChanged += (_, _) => ApplyPromotion();
        _transfer.CheckedChanged += (_, _) => RefreshOrder();
        _menu.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) AddSelected(); };
        Load += (_, _) => Initialize();
    }

    private static TableLayoutPanel Column(params SizeType[] rows)
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = rows.Length };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        foreach (var type in rows)
            panel.RowStyles.Add(type == SizeType.Percent ? new RowStyle(type, 100) : new RowStyle(type));
        return panel;
    }

    private void Initialize()
    {
        _category.Items.Add("Tất cả nhóm");
        foreach (var category in _services.Products.GetCategories()) _category.Items.Add(category);
        _category.SelectedIndex = 0;

        _promotion.Items.Add("Không khuyến mãi");
        foreach (var promotion in _services.Promotions.GetValidOn(DateTime.Today)) _promotion.Items.Add(promotion);
        _loading = false;
        LoadMenu();
        StartOrder();
    }

    private void LoadMenu()
    {
        if (_loading) return;
        var categoryId = (_category.SelectedItem as Category)?.Id;
        _menu.Rows.Clear();
        foreach (var product in _services.Products.Search(categoryId, _keyword.Text, activeOnly: true))
        foreach (var size in product.Sizes)
        {
            var index = _menu.Rows.Add(product.Name, size.Name, size.Price);
            _menu.Rows[index].Tag = (product, size);
        }
    }

    private void StartOrder()
    {
        _order = null;
        _customer = null;
        _phone.Clear();
        _received.Value = 0;
        _promotion.SelectedIndex = 0;
        Ui.Try(() => _order = _services.Sales.NewOrder(_user));
        RefreshOrder();
    }

    private void AddSelected()
    {
        if (_order == null) StartOrder();
        if (_order == null || _menu.CurrentRow?.Tag is not ValueTuple<Product, ProductSize> item) return;
        Ui.Try(() => _order.AddItem(item.Item1, item.Item2, (int)_quantity.Value));
        _quantity.Value = 1;
        RefreshOrder();
    }

    private void RemoveSelected()
    {
        if (_order == null || _lines.CurrentRow?.Tag is not int sizeId) return;
        Ui.Try(() => _order.RemoveItem(sizeId));
        RefreshOrder();
    }

    private void ApplyPromotion()
    {
        if (_order == null) return;
        var promotion = _promotion.SelectedItem as Promotion;
        Ui.Try(() => _order.ApplyDiscount(DiscountSelector.For(promotion, DateTime.Today)));
        RefreshOrder();
    }

    private void FindCustomer()
    {
        if (_order == null) return;
        _customer = _services.Customers.FindByPhone(_phone.Text.Trim());
        _order.AssignCustomer(_customer?.Id);
        RefreshOrder();
        if (_customer == null) Ui.Info("Không tìm thấy khách hàng với số điện thoại này.");
    }

    private void RefreshOrder()
    {
        _lines.Rows.Clear();
        _customerName.Text = _customer == null ? "Khách lẻ" : $"{_customer.Name} ({_customer.Points} điểm)";
        foreach (var line in _order?.Lines ?? Array.Empty<OrderLine>())
        {
            var index = _lines.Rows.Add(line.ProductName, line.SizeName, line.Quantity, line.UnitPrice, line.LineTotal);
            _lines.Rows[index].Tag = line.ProductSizeId;
        }
        _summary.Text = $"Tạm tính: {_order?.Subtotal ?? 0:#,##0} đ    Giảm: {_order?.DiscountAmount ?? 0:#,##0} đ";
        _total.Text = $"CẦN TRẢ: {_order?.Total ?? 0:#,##0} đ";
        _received.Enabled = !_transfer.Checked;
        if (_transfer.Checked) _received.Value = _order?.Total ?? 0;
    }

    private void Checkout()
    {
        if (_order == null) return;
        var method = _transfer.Checked ? PaymentMethod.BankTransfer : PaymentMethod.Cash;
        var amount = _transfer.Checked ? _order.Total : _received.Value;
        Payment payment = null;
        if (!Ui.Try(() => payment = _services.Sales.Checkout(_user, _order, method, amount))) return;

        Ui.Info($"Đã thanh toán hóa đơn số {_order.Id}.\nTiền thừa: {payment.Change:#,##0} đ");
        StartOrder();
    }

    /// <summary>Dựng một đơn minh họa để chụp ảnh giao diện.</summary>
    internal void FillSample()
    {
        foreach (var row in new[] { 2, 7, 10 })
        {
            _menu.CurrentCell = _menu.Rows[row].Cells[0];
            _quantity.Value = row == 7 ? 2 : 1;
            AddSelected();
        }
        _promotion.SelectedIndex = 1;
        _phone.Text = "0900000002";
        FindCustomer();
        _received.Value = 200_000;
        _menu.CurrentCell = _menu.Rows[0].Cells[0];
    }
}
