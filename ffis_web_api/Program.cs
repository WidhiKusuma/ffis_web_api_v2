using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using ffis_web_api.Repositories;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Stopgap thread-pool starvation: banyak akses DB & validasi AD masih sinkron/blocking
// (semua ke host 172.19.160.4). Menaikkan lantai thread mencegah worker kehabisan thread
// saat host itu lambat -> worker tetap bisa membalas health-ping WAS -> tidak kena recycle (event 5078).
// Ini penambal; solusi sebenarnya = async-kan akses DB + validasi AD.
ThreadPool.SetMinThreads(200, 200);

// Menambahkan Repository ke DI container
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<ITStockRepository>();
builder.Services.AddScoped<DriverRepository>();
builder.Services.AddScoped<ICbmRepository, CbmRepository>();
builder.Services.AddScoped<ICmsYlidRepository, CmsYlidRepository>();
builder.Services.AddScoped<ffis_web_api.Services.GraphMailService>();

// Initialize Firebase Admin
var fcmKeyPath = Path.Combine(builder.Environment.ContentRootPath, "fcm_key.json");
if (File.Exists(fcmKeyPath))
{
    FirebaseAdmin.FirebaseApp.Create(new FirebaseAdmin.AppOptions()
    {
        Credential = Google.Apis.Auth.OAuth2.GoogleCredential.FromFile(fcmKeyPath)
    });
}

builder.Services.AddHttpClient("YLIDClient")
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
    });

// Background job: pengingat harian masa berlaku SIM driver (H-60/30/15/7/0)
builder.Services.AddSingleton<ffis_web_api.Services.SimExpiryJob>();
builder.Services.AddHostedService<ffis_web_api.Services.SimExpiryNotificationService>();

// Tambahkan koneksi database SQL Server (P2H)
builder.Services.AddScoped<IDbConnection>(sp =>
    new SqlConnection(builder.Configuration.GetConnectionString("FFISDB")));

// Tambahkan koneksi database Postgres (CBM)
builder.Services.AddScoped<NpgsqlConnection>(sp =>
    new NpgsqlConnection(builder.Configuration.GetConnectionString("dbpath")));

// Tambahkan autentikasi JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

//if (app.Environment.IsDevelopment())
//{

//}

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseMiddleware<ffis_web_api.AppVersionMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
