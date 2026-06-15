namespace HochuProect.Application.IServices
{
    public interface IServiceResult<T>
    {
        bool IsSuccess { get; }
        T? Value { get; }
        string? ErrorMessage { get; }
        int StatusCode { get; }
    }
}
