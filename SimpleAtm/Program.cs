using SimpleAtm.Repositories;
using SimpleAtm.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<ITransactionRepository, InMemoryTransactionRepository>();
builder.Services.AddScoped<IAtmService, AtmService>();
builder.Services.AddSingleton<TransactionValidator>();

var app = builder.Build();
app.UseStaticFiles();
app.MapControllers();
app.Run();
