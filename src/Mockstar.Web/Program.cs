using Microsoft.EntityFrameworkCore;
using Mockstar.Web.Persistence;
using Mockstar.Web.Services.Imports;
using Mockstar.Web.Services.Heats;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.Configure<ParserApiOptions>(
    builder.Configuration.GetSection(ParserApiOptions.SectionName));
builder.Services.AddHttpClient<ParserApiClient>((serviceProvider, client) =>
{
    var options = serviceProvider
        .GetRequiredService<Microsoft.Extensions.Options.IOptions<ParserApiOptions>>()
        .Value;
    client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
});

// Persistence
var connectionString = builder.Configuration.GetConnectionString("HeatDb")
    ?? "Data Source=mockstar.db";
builder.Services.AddDbContext<HeatDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddScoped<IHeatRepository, HeatRepository>();
builder.Services.AddScoped<HeatApiClient>();

var app = builder.Build();

// Apply migrations on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<HeatDbContext>();
    dbContext.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
