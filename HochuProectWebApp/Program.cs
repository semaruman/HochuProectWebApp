
using HochuProectWebApp.Services.EF_core;
using HochuProectWebApp.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.HttpResults;
using HochuProectWebApp.Middleware;
using HochuProectWebApp.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddScoped<IAdvertisementService, AdvertisementEfService>();
builder.Services.AddScoped<IUserService, UserEfService>();
builder.Services.AddScoped<ICategoryService, CategoryEfService>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options => options.LoginPath = "/login");

//сервисы для сваггера
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//сервисы для обработки всех исключений
builder.Services.AddExceptionHandler<ExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

//подключаю обработку исключений
app.UseExceptionHandler();

//подключаю логгирование всех запросов
app.UseLoggingMiddleware();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", GetMenu);
app.Run();

IResult GetMenu()
{
    return Results.Ok(new
    {
        Endpoints = new[]
        {
            ""
        }
    });
}