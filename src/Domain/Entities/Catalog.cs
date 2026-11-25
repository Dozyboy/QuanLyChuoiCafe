using QuanLyChuoiCafe.Domain.Exceptions;

namespace QuanLyChuoiCafe.Domain.Entities;

public class Category
{
    public Category(int id, string name)
    {
        Id = id;
        Name = name;
    }

    public int Id { get; }
    public string Name { get; }
    public override string ToString() => Name;
}

/// <summary>Một kích cỡ của món và giá bán. Công thức định lượng (TV2) gắn theo Id của lớp này.</summary>
public class ProductSize
{
    public ProductSize(int id, int productId, string name, decimal price)
    {
        Id = id;
        ProductId = productId;
        Rename(name);
        ChangePrice(price);
    }

    public int Id { get; set; }
    public int ProductId { get; }
    public string Name { get; private set; }
    public decimal Price { get; private set; }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ValidationException("Tên size không được để trống.");
        Name = name.Trim();
    }

    public void ChangePrice(decimal price)
    {
        if (price <= 0) throw new ValidationException("Giá bán phải lớn hơn 0.");
        if (price != decimal.Truncate(price)) throw new ValidationException("Giá bán tính theo đồng, không có phần lẻ.");
        Price = price;
    }
}

/// <summary>Món trong thực đơn. Quản lý danh sách size (quan hệ hợp thành).</summary>
public class Product
{
    private readonly List<ProductSize> _sizes = new();

    public Product(int id, string name, int categoryId, bool isActive = true)
    {
        Id = id;
        CategoryId = categoryId;
        IsActive = isActive;
        Rename(name);
    }

    public int Id { get; set; }
    public string Name { get; private set; }
    public int CategoryId { get; private set; }
    public bool IsActive { get; set; }
    public IReadOnlyList<ProductSize> Sizes => _sizes;

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ValidationException("Tên món không được để trống.");
        Name = name.Trim();
    }

    public void MoveTo(int categoryId) => CategoryId = categoryId;

    public void AddSize(ProductSize size)
    {
        if (_sizes.Any(s => string.Equals(s.Name, size.Name, StringComparison.OrdinalIgnoreCase)))
            throw new ValidationException($"Món \"{Name}\" đã có size \"{size.Name}\".");
        _sizes.Add(size);
    }

    public override string ToString() => Name;
}
