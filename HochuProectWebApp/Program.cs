
using HochuProectWebApp.Services.EF_core;
using HochuProectWebApp.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.HttpResults;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddScoped<IAdvertisementService, AdvertisementEfService>();
builder.Services.AddScoped<IUserService, UserEfService>();
builder.Services.AddScoped<ICategoryService, CategoryEfService>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options => options.LoginPath = "/login");

var app = builder.Build();
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