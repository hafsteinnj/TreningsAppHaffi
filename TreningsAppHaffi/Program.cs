using Microsoft.EntityFrameworkCore;
using TreningsAppHaffi.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Needed for SweeperGame's per-user game state.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();

var connectionString = builder.Configuration.GetConnectionString("MyAzureSqlConnectionString")
    ?? throw new InvalidOperationException("Connection string 'MyAzureSqlConnectionString' not found.");

builder.Services.AddDbContext<MyDatabaseContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        // Recommended: Enable resilience for Azure SQL (handles transient failures)
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
    }));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseSession();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
