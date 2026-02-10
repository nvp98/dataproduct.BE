using dataproduct.api.Business;
using dataproduct.api.Models;
using dataproduct.api.Models.MasterData;
using dataproduct.api.Repositories;
using DinkToPdf;
using DinkToPdf.Contracts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Thêm CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins", builder =>
    {
        builder.AllowAnyOrigin()  // Cho phép tất cả các origin
               .AllowAnyMethod()  // Cho phép tất cả các method (GET, POST, PUT, DELETE...)
               .AllowAnyHeader(); // Cho phép tất cả các header
    });
    //options.AddPolicy("AllowReact",
    //    policy =>
    //    {
    //        policy.WithOrigins("http://localhost:5173") // Cho phép React truy cập
    //              .AllowAnyHeader()
    //              .AllowAnyMethod();
    //    });
});

// Đăng ký các lớp xử lý
//builder.Services.AddScoped<IPhieuRepository, PhieuRepository>();
//builder.Services.AddScoped<PhieuService>();

builder.Services.AddSingleton<IConverter>(
    new SynchronizedConverter(new PdfTools())
);

builder.Services.AddHttpClient();

builder.Services.Scan(scan => scan
    .FromAssemblies(Assembly.GetExecutingAssembly())
    .AddClasses(c => c.Where(t => t.Name.EndsWith("Helper")))
        .AsSelf()
        .WithSingletonLifetime()
    .AddClasses(c => c.Where(t => t.Name.EndsWith("Repository")))
        .AsImplementedInterfaces()
        .WithScopedLifetime()
    .AddClasses(c => c.Where(t => t.Name.EndsWith("Service")))
        .AsSelf()
        .WithScopedLifetime()
);



// Đăng ký DbContext với Dependency Injection (DI)
builder.Services.AddDbContext<ProductFormContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DbConnectionString")));

builder.Services.AddDbContext<ProductDataMasterDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("MasterDbConnection")));

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 🔑 1. STATIC FILES (React build)
app.UseStaticFiles();

// 🔑 2. API
app.MapControllers();

// 🔑 3. SWAGGER (OK)
app.UseSwagger();
app.UseSwaggerUI();

// 🔑 4. SPA FALLBACK – QUAN TRỌNG NHẤT
app.MapFallbackToFile("index.html");

// 🔑 5. CORS (nếu cần)
app.UseCors("AllowAllOrigins");

// ❌ KHÔNG HTTPS REDIRECTION
// ❌ KHÔNG UseDefaultFiles
// ❌ KHÔNG MapControllers lần 2

app.Run();

