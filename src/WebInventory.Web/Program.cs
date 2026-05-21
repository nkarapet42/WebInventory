using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using System.Globalization;
using WebInventory.Application.Interfaces;
using WebInventory.Domain.Constants;
using WebInventory.Domain.Identity;
using WebInventory.Infrastructure.Data;
using WebInventory.Infrastructure.Services;
using WebInventory.Infrastructure.Services.CustomId;
using WebInventory.Web.Hubs;
using WebInventory.Web.Services;

var builder = WebApplication.CreateBuilder(args);
var supportedCultures = new[]
{
    new CultureInfo("en"),
    new CultureInfo("es")
};

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/app/keys"))
    .SetApplicationName("WebInventory");
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.AccessDeniedPath = "/";
});
ConfigureExternalAuthentication(builder.Services, builder.Configuration);
builder.Services.AddScoped<IClaimsTransformation, DatabaseRoleClaimsTransformation>();
builder.Services.AddSingleton<MarkdownService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IItemService, ItemService>();
builder.Services.AddScoped<IAccessControlService, AccessControlService>();
builder.Services.AddScoped<ISearchService, SearchService>();
builder.Services.AddScoped<ICustomIdGenerator, CustomIdGenerator>();
builder.Services.Configure<CloudinaryOptions>(builder.Configuration.GetSection("Cloudinary"));
builder.Services.AddScoped<IImageUploadService, CloudinaryImageUploadService>();
builder.Services.AddSingleton<IIdPartGenerator, FixedTextGenerator>();
builder.Services.AddSingleton<IIdPartGenerator, Random20BitGenerator>();
builder.Services.AddSingleton<IIdPartGenerator, Random32BitGenerator>();
builder.Services.AddSingleton<IIdPartGenerator, Random6DigitGenerator>();
builder.Services.AddSingleton<IIdPartGenerator, Random9DigitGenerator>();
builder.Services.AddSingleton<IIdPartGenerator, GuidGenerator>();
builder.Services.AddSingleton<IIdPartGenerator, DateTimeGenerator>();
builder.Services.AddSingleton<IIdPartGenerator, SequenceGenerator>();
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture("en");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    options.RequestCultureProviders =
    [
        new CookieRequestCultureProvider(),
        new AcceptLanguageHeaderRequestCultureProvider()
    ];
});
builder.Services.AddControllersWithViews()
    .AddViewLocalization();
builder.Services.AddRazorPages();
builder.Services.AddSignalR();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();
    await SeedIdentityAsync(scope.ServiceProvider, app.Configuration);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseRequestLocalization(app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value);

app.UseRouting();
app.UseAuthentication();
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        var userManager = context.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
        var signInManager = context.RequestServices.GetRequiredService<SignInManager<ApplicationUser>>();
        var user = await userManager.GetUserAsync(context.User);
        if (user is null || await userManager.IsLockedOutAsync(user))
        {
            await signInManager.SignOutAsync();
            if (string.Equals(context.Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            context.Response.Redirect("/");
            return;
        }
    }

    await next();
});
app.UseAuthorization();

app.UseStaticFiles();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();
app.MapHub<DiscussionHub>("/hubs/discussion");


app.Run();

static async Task SeedIdentityAsync(IServiceProvider services, IConfiguration configuration)
{
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    if (!await roleManager.RoleExistsAsync(RoleNames.Admin))
    {
        await roleManager.CreateAsync(new IdentityRole(RoleNames.Admin));
    }

    var configuredEmails = configuration.GetSection("Admin:Emails").Get<string[]>()
        ?? SplitEmails(configuration["ADMIN_EMAILS"]);
    if (configuredEmails.Length == 0)
    {
        return;
    }

    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    foreach (var email in configuredEmails)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is not null && !await userManager.IsInRoleAsync(user, RoleNames.Admin))
        {
            await userManager.AddToRoleAsync(user, RoleNames.Admin);
        }
    }
}

static string[] SplitEmails(string? value)
{
    return string.IsNullOrWhiteSpace(value)
        ? Array.Empty<string>()
        : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

static void ConfigureExternalAuthentication(IServiceCollection services, IConfiguration configuration)
{
    var authentication = services.AddAuthentication();
    var google = GetExternalProviderOptions(configuration, "Google");
    if (google is not null)
    {
        authentication.AddGoogle(options =>
        {
            options.ClientId = google.Value.ClientId;
            options.ClientSecret = google.Value.ClientSecret;
        });
    }

    var facebook = GetExternalProviderOptions(configuration, "Facebook");
    if (facebook is not null)
    {
        authentication.AddFacebook(options =>
        {
            options.AppId = facebook.Value.ClientId;
            options.AppSecret = facebook.Value.ClientSecret;
        });
    }
}

static ExternalProviderOptions? GetExternalProviderOptions(IConfiguration configuration, string provider)
{
    var clientId = FirstConfiguredValue(
        configuration[$"Authentication:{provider}:ClientId"],
        configuration[$"{provider.ToUpperInvariant()}_CLIENT_ID"]);
    var clientSecret = FirstConfiguredValue(
        configuration[$"Authentication:{provider}:ClientSecret"],
        configuration[$"{provider.ToUpperInvariant()}_CLIENT_SECRET"]);

    return string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret)
        ? null
        : new ExternalProviderOptions(clientId, clientSecret);
}

static string? FirstConfiguredValue(params string?[] values)
{
    return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
}

readonly record struct ExternalProviderOptions(string ClientId, string ClientSecret);
