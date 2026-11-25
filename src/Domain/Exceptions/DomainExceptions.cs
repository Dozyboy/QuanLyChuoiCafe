namespace QuanLyChuoiCafe.Domain.Exceptions;

/// <summary>Gốc của mọi lỗi nghiệp vụ: giao diện chỉ cần bắt lớp này để báo cho người dùng.</summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}

/// <summary>Dữ liệu nhập hoặc thao tác trên đơn hàng không hợp lệ.</summary>
public class ValidationException : DomainException
{
    public ValidationException(string message) : base(message) { }
}

public class AuthenticationException : DomainException
{
    public AuthenticationException(string message) : base(message) { }
}

public class PermissionDeniedException : DomainException
{
    public PermissionDeniedException(string message) : base(message) { }
}

/// <summary>Vi phạm quy tắc ca làm việc.</summary>
public class ShiftException : DomainException
{
    public ShiftException(string message) : base(message) { }
}
