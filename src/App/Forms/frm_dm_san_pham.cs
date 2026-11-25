using QuanLyChuoiCafe.Domain.Entities;
using QuanLyChuoiCafe.Domain.Repositories;

namespace QuanLyChuoiCafe.App.Forms;

/// <summary>Form danh mục master-detail: lưới món ở trên, lưới size của món đang chọn ở dưới.</summary>
public class frm_dm_san_pham : Form
{
    private readonly IProductRepository _products;
    private readonly User _user;
    private IReadOnlyList<Category> _categories = Array.Empty<Category>();
    private bool _loading = true;

    private readonly ComboBox _filter = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150 };
    private readonly TextBox _keyword = new() { Width = 200, PlaceholderText = "Tìm tên món" };
    private readonly DataGridView _master = Ui.Grid(("id", "Mã", 8, "0"), ("name", "Tên món", 46, null),
        ("category", "Nhóm", 24, null), ("sizes", "Số size", 10, "0"), ("active", "Trạng thái", 18, null));
    private readonly DataGridView _detail = Ui.Grid(("size", "Size", 50, null), ("price", "Giá bán (đ)", 50, "#,##0"));

    public frm_dm_san_pham(IProductRepository products, User user)
    {
        _products = products;
        _user = user;
        SuspendLayout();
        Ui.Prepare(this, "Danh mục món", 980, 640);

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 65));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 35));

        layout.Controls.Add(Ui.Row(_filter, _keyword,
            Ui.Button("Thêm món", (_, _) => EditProduct(null), true),
            Ui.Button("Sửa món", (_, _) => { if (Selected != null) EditProduct(Selected); }),
            Ui.Button("Ngừng / bán lại", (_, _) => ToggleActive())), 0, 0);
        layout.Controls.Add(_master, 0, 1);
        layout.Controls.Add(Ui.Row(Ui.Label("Size và giá của món đang chọn", bold: true),
            Ui.Button("Thêm size", (_, _) => EditSize(null), true),
            Ui.Button("Sửa size / giá", (_, _) => EditSize(_detail.CurrentRow?.Tag as ProductSize))), 0, 2);
        layout.Controls.Add(_detail, 0, 3);
        Controls.Add(layout);
        ResumeLayout();

        _filter.SelectedIndexChanged += (_, _) => Reload();
        _keyword.TextChanged += (_, _) => Reload();
        _master.SelectionChanged += (_, _) => ShowSizes();
        Load += (_, _) => Initialize();
    }

    private Product Selected => _master.CurrentRow?.Tag as Product;

    private void Initialize()
    {
        _categories = _products.GetCategories();
        _filter.Items.Add("Tất cả nhóm");
        foreach (var category in _categories) _filter.Items.Add(category);
        _filter.SelectedIndex = 0;
        _loading = false;
        Reload();
    }

    private void Reload()
    {
        if (_loading) return;
        var keepId = Selected?.Id;
        var categoryId = (_filter.SelectedItem as Category)?.Id;
        _master.Rows.Clear();
        foreach (var product in _products.Search(categoryId, _keyword.Text, activeOnly: false))
        {
            var index = _master.Rows.Add(product.Id, product.Name,
                _categories.First(c => c.Id == product.CategoryId).Name, product.Sizes.Count,
                product.IsActive ? "Đang bán" : "Ngừng bán");
            _master.Rows[index].Tag = product;
            if (product.Id == keepId) _master.CurrentCell = _master.Rows[index].Cells[0];
        }
        ShowSizes();
    }

    private void ShowSizes()
    {
        _detail.Rows.Clear();
        foreach (var size in Selected?.Sizes ?? Array.Empty<ProductSize>())
        {
            var index = _detail.Rows.Add(size.Name, size.Price);
            _detail.Rows[index].Tag = size;
        }
    }

    private void EditProduct(Product product)
    {
        using var dialog = new frm_dm_san_pham_chi_tiet(_categories, product);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        Ui.Try(() =>
        {
            _user.Demand(Permission.ManageCatalog);
            if (product == null)
                _products.Add(new Product(0, dialog.ProductName_, dialog.CategoryId));
            else
            {
                product.Rename(dialog.ProductName_);
                product.MoveTo(dialog.CategoryId);
                _products.Update(product);
            }
        });
        Reload();
    }

    private void ToggleActive()
    {
        var product = Selected;
        if (product == null) return;
        Ui.Try(() =>
        {
            _user.Demand(Permission.ManageCatalog);
            product.IsActive = !product.IsActive;
            _products.Update(product);
        });
        Reload();
    }

    private void EditSize(ProductSize size)
    {
        var product = Selected;
        if (product == null) return;
        using var dialog = new frm_dm_size_chi_tiet(product.Name, size);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        Ui.Try(() =>
        {
            _user.Demand(Permission.ManageCatalog);
            if (size == null)
            {
                var added = new ProductSize(0, product.Id, dialog.SizeName, dialog.Price);
                product.AddSize(added);
                _products.AddSize(added);
            }
            else
            {
                size.Rename(dialog.SizeName);
                size.ChangePrice(dialog.Price);
                _products.UpdateSize(size);
            }
        });
        Reload();
    }
}
