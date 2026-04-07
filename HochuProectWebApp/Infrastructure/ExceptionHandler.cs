using Microsoft.AspNetCore.Diagnostics;

namespace HochuProectWebApp.Infrastructure
{
    public class ExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<ExceptionHandler> _logger;

        public ExceptionHandler(ILogger<ExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            _logger.LogError("Возникла ошибка в {method}: {path}. Текст ошибки: {errorText}",
                httpContext.Request.Method, httpContext.Request.Path, exception.Message
                );

            //если клиент прервал запрос, то не отправляю ответ
            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            //устанавливаю кодировку страницы для понятного ответа на русском языке
            httpContext.Response.ContentType = "text/plain; charset=utf-8";

            httpContext.Response.StatusCode = 500;
            await httpContext.Response.WriteAsync("Что-то пошло не так");

            return true;
        }
    }
}
