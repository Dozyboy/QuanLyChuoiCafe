using QuanLyChuoiCafe.Domain.Exceptions;

namespace QuanLyChuoiCafe.Domain.Entities;

[Flags]
public enum Permission
{
    None = 0,
    Sell = 1,
    CancelOrder = 2,
    ManageCatalog = 4,
    ManageShift = 8,
    ManageStock = 16
}

/// <summary>Vai trò người dùng (phân quyền theo vai trò - RBAC).</summary>
public class Role
{
    public static readonly Role Manager = new(1, "Quản lý chuỗi",
        Permission.Sell | Permission.CancelOrder | Permission.ManageCatalog | Permission.ManageShift | Permission.ManageStock);
    public static readonly Role Cashier = new(2, "Thu ngân", Permission.Sell | Permission.ManageShift);
    public static readonly Role StockKeeper = new(3, "Thủ kho", Permission.ManageStock);

    private Role(int id, string name, Permission permissions)
    {
        Id = id;
        Name = name;
        Permissions = permissions;
    }

    public int Id { get; }
    public string Name { get; }
    public Permission Permissions { get; }

    public bool Allows(Permission permission) => (Permissions & permission) == permission;

    public static Role FromId(int id) => id switch
    {
        1 => Manager,
        2 => Cashier,
        3 => StockKeeper,
        _ => throw new ValidationException($"Không có vai trò mã {id}.")
    };
}

public class User
{
    public User(int id, string username, string fullName, string passwordHash, Role role, int branchId, bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(username)) throw new ValidationException("Tên đăng nhập không được để trống.");
        Id = id;
        Username = username.Trim();
        FullName = fullName;
        PasswordHash = passwordHash;
        Role = role ?? throw new ValidationException("Người dùng phải có vai trò.");
        BranchId = branchId;
        IsActive = isActive;
    }

    public int Id { get; }
    public string Username { get; }
    public string FullName { get; }
    public string PasswordHash { get; }
    public Role Role { get; }
    public int BranchId { get; }
    public bool IsActive { get; }

    public void Demand(Permission permission)
    {
        if (!Role.Allows(permission))
            throw new PermissionDeniedException($"Vai trò \"{Role.Name}\" không được thực hiện thao tác này.");
    }
}

/// <summary>Chi nhánh trong chuỗi.</summary>
public class Branch
{
    public Branch(int id, string name, string address)
    {
        Id = id;
        Name = name;
        Address = address;
    }

    public int Id { get; }
    public string Name { get; }
    public string Address { get; }
}
