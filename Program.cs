using F.Controllers;
using F.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSignalR();
builder.Services.AddHostedService<BackgroundDataChecker>();
builder.Services.AddControllersWithViews();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Dura��o da sess�o
    options.Cookie.HttpOnly = true; // Seguran�a do cookie
    options.Cookie.IsEssential = true; // Necess�rio para garantir que o cookie seja enviado mesmo que o usu�rio n�o consinta com o uso de cookies
});

builder.Services.AddHttpClient<HomeController>();
builder.Services.AddHttpClient<DashboardController>();
builder.Services.AddAuthentication("Bearer");
builder.Services.AddMemoryCache();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseSession();
app.UseRouting();

app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");
    endpoints.MapHub<DashboardHub>("/dashboardHub");
});


app.Run();
