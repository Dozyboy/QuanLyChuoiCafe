using QuanLyChuoiCafe.Domain.Entities;

namespace QuanLyChuoiCafe.App.Forms;

/// <summary>Khung chung cho các hộp thoại chi tiết: danh sách nhãn - ô nhập và hai nút Lưu, Hủy.</summary>
public abstract class DetailDialog : Form
{
    protected void Build(string title, params (string Label, Control Input)[] fields)
    {
        Ui.Prepare(this, title, 440, 90 + fields.Length * 44);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(16) };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var i = 0; i < fields.Length; i++)
        {
            fields[i].Input.Width = 250;
            layout.Controls.Add(Ui.Label(fields[i].Label), 0, i);
            layout.Controls.Add(fields[i].Input, 1, i);
        }

        var save = Ui.Button("Lưu", (_, _) => DialogResult = DialogResult.OK, primary: true);
        var cancel = Ui.Button("Hủy", (_, _) => DialogResult = DialogResult.Cancel);
        layout.Controls.Add(Ui.Row(save, cancel), 1, fields.Length);
        AcceptButton = save;
        CancelButton = cancel;
        Controls.Add(layout);
    }
}

public class frm_dm_san_pham_chi_tiet : DetailDialog
{
    private readonly TextBox _name = new() { MaxLength = 80 };
    private readonly ComboBox _category = new() { DropDownStyle = ComboBoxStyle.DropDownList };

    public frm_dm_san_pham_chi_tiet(IReadOnlyList<Category> categories, Product product)
    {
        SuspendLayout();
        foreach (var category in categories) _category.Items.Add(category);
        _name.Text = product?.Name ?? "";
        _category.SelectedItem = categories.FirstOrDefault(c => c.Id == product?.CategoryId) ?? categories[0];
        Build(product == null ? "Thêm món" : "Sửa món", ("Tên món", _name), ("Nhóm", _category));
        ResumeLayout();
    }

    public string ProductName_ => _name.Text;
    public int CategoryId => ((Category)_category.SelectedItem).Id;
}

public class frm_dm_size_chi_tiet : DetailDialog
{
    private readonly TextBox _name = new() { MaxLength = 10 };
    private readonly NumericUpDown _price = new() { Maximum = 10_000_000, Increment = 1000, ThousandsSeparator = true };

    public frm_dm_size_chi_tiet(string productName, ProductSize size)
    {
        SuspendLayout();
        _name.Text = size?.Name ?? "";
        _price.Value = size?.Price ?? 0;
        Build((size == null ? "Thêm size - " : "Sửa size - ") + productName, ("Size", _name), ("Giá bán (đ)", _price));
        ResumeLayout();
    }

    public string SizeName => _name.Text;
    public decimal Price => _price.Value;
}
