var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSession(); // Add session services

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession(); // Enable session middleware
app.UseAuthorization();

// Set up route endpoints.
app.UseEndpoints(endpoints =>
{
    // Default route to the Login page.
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Login}/{id?}");

    // Route for the Status page.
    endpoints.MapControllerRoute(
        name: "status",
        pattern: "Status",
        defaults: new { controller = "Home", action = "Status" });
});

app.Run();
