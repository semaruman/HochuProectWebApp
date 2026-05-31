using HochuProectWebApp.Services.Interfaces;

namespace HochuProectWebApp.Services
{
    public class ServiceResult<T> : IServiceResult<T>
    {
        public bool IsSuccess { get; private set; }
        public T? Value { get; private set; }
        public string? ErrorMessage { get; private set; }
        public int StatusCode { get; private set; }

        public static ServiceResult<T> Success(T? value)
        {
            return new ServiceResult<T>
            {
                IsSuccess = true,
                Value = value,
                StatusCode = 200
            };
        }

        public static ServiceResult<T> Fail(string errorMessage, int statusCode = 400)
        {
            return new ServiceResult<T>
            {
                IsSuccess = false,
                ErrorMessage = errorMessage,
                StatusCode = statusCode
            };
        }
    }
}
