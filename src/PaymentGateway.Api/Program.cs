using PaymentGateway.Api.Helpers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services
    .AddAspNetCoreServices(builder.Configuration)
    .AddSingletons()
    .AddHttpClients(builder.Configuration);

builder.AddAppConfig();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();

app.UseHttpsRedirection();

app.UseRouting();

app.UseRateLimiter();

app.MapControllers();

app.Run();

public partial class Program { }