using Data;
using Microsoft.EntityFrameworkCore;
using Syncfusion.Licensing;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<DataContext>(op => op.UseInMemoryDatabase("PocSync"));
SyncfusionLicenseProvider.RegisterLicense(builder.Configuration.GetValue<string>("SyncfusionKey"));
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();