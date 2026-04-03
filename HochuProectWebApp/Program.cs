
using HochuProectWebApp.Services.EF_core;
using HochuProectWebApp.Services.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddScoped<IAdvertisementService, AdvertisementEfService>();
builder.Services.AddScoped<IUserService, UserEfService>();
builder.Services.AddScoped<ICategoryService, CategoryEfService>();

var app = builder.Build();

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