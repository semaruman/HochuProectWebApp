using HochuProect.Application.IRepositories;
using HochuProect.Application.IServices;
using HochuProect.Application.IUnitOfWork;
using HochuProect.Infrastructure.Data;
using HochuProect.Infrastructure.Repositories;
using HochuProect.Infrastructure.Services;
using HochuProect.Infrastructure.UnitOfWork;
using HochuProectWebApp.Infrastructure;
using HochuProectWebApp.Middleware;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddScoped<IAdvertisementService, AdvertisementEfService>();
builder.Services.AddScoped<IUserService, UserEfService>();
builder.Services.AddScoped<ICategoryService, CategoryEfService>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options => options.LoginPath = "/login");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgresConnection")));

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