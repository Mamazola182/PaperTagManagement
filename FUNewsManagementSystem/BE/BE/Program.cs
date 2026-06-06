using BE.DataAccess;
using BE.DTO;
using BE.Models;
using BE.Services;
using CoreAPI.DataAccess;
using CoreAPI.Hubs;
using CoreAPI.Models;
using CoreAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OData.ModelBuilder;
using System.Text;
var builder = WebApplication.CreateBuilder(args);
    
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Add services to the container.
builder.Services.AddDbContext<FunewsManagementContext>(options =>
    options.UseSqlServer(connectionString));

// Build EDM Model first
var edmBuilder = new ODataConventionModelBuilder();
edmBuilder.EntitySet<CategoryDTO>("Category"); 
edmBuilder.EntitySet<NewsArticle>("NewsArticle");
edmBuilder.EntitySet<SystemAccount>("SystemAccount");
edmBuilder.EntitySet<Tag>("Tag");
edmBuilder.EntitySet<ActiveNews>("ActiveNews"); 
edmBuilder.EntitySet<AuditLog>("AuditLog");
// Add Controllers with OData 
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var problemDetails = new ValidationProblemDetails(context.ModelState)
            {
                Status = StatusCodes.Status400BadRequest,
            };
            return new BadRequestObjectResult(problemDetails);
        };
    })
    .AddOData(opt => opt
        .Select()
        .Filter()
        .OrderBy()
        .Count()
        .Expand()
        .SetMaxTop(100)
        .AddRouteComponents("api/", edmBuilder.GetEdmModel()));

//add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins("https://localhost:7036")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

//JWT configuration
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]);
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) &&
                path.StartsWithSegments("/hubs/notifications"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };

});

//Register services
builder.Services.AddSignalR();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ISystemAccountDA, SystemAccountDASqlServer>();
builder.Services.AddScoped<INewsArticleDA, NewsArticleDASqlServer>();
builder.Services.AddScoped<ITagDA, TagDASqlServer>();
builder.Services.AddScoped<ICategoryDA, CategoryDASqlServer>();
builder.Services.AddScoped<ISystemAccountServices, SystemAccountServicesVer1>();
builder.Services.AddScoped<INewArticleServices, NewsArticleServicesVer1>();
builder.Services.AddScoped<ICategoryServices, CategoryServicesVer1>();
builder.Services.AddScoped<ITokenServices, TokenServiceVer1>();
builder.Services.AddScoped<ITagServices, TagServicesVer1>();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<TagCacheService>();
builder.Services.AddSingleton<GeminiService>();
builder.Services.AddMemoryCache();
builder.Services.AddHostedService<CacheStarupService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuditLogsServices, AuditLogsServicesVer1>();
builder.Services.AddScoped<IAuditLogDA, AuditLogDASqlServer>();

var app = builder.Build();

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");
app.Run();