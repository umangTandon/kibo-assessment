using InventoryHold.Domain.Options;
using InventoryHold.Domain.Services;
using InventoryHold.Infrastructure.DependencyInjection;
using InventoryHold.WebApi.ExceptionHandlers;
using InventoryHold.WebApi.Extensions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<HoldOptions>(builder.Configuration.GetSection("Hold"));
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<HoldService>();
builder.Services.AddControllers();
builder.Services.AddSingleton<IExceptionHandler, DomainExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        await context.RequestServices.GetRequiredService<IExceptionHandler>().HandleAsync(context);
    });
});

app.UseCors();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();

await app.SeedInventoryAsync();
await app.RunAsync();
