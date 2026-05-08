using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebInventory.Application.Interfaces;
using WebInventory.Domain.Identity;
using WebInventory.Infrastructure.Data;
using WebInventory.Infrastructure.Services;
using WebInventory.Infrastructure.Services.CustomId;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IItemService, ItemService>();
builder.Services.AddScoped<IAccessControlService, AccessControlService>();
builder.Services.AddScoped<ISearchService, SearchService>();
builder.Services.AddScoped<ICustomIdGenerator, CustomIdGenerator>();
builder.Services.AddSingleton<IIdPartGenerator, FixedTextGenerator>();
builder.Services.AddSingleton<IIdPartGenerator, Random20BitGenerator>();
builder.Services.AddSingleton<IIdPartGenerator, Random32BitGenerator>();
builder.Services.AddSingleton<IIdPartGenerator, Random6DigitGenerator>();
builder.Services.AddSingleton<IIdPartGenerator, Random9DigitGenerator>();
builder.Services.AddSingleton<IIdPartGenerator, GuidGenerator>();
builder.Services.AddSingleton<IIdPartGenerator, DateTimeGenerator>();
builder.Services.AddSingleton<IIdPartGenerator, SequenceGenerator>();
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseStaticFiles();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();


app.Run();
