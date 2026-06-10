using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using ffis_web_api.Repositories;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Menambahkan Repository ke DI container
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<ITStockRepository>();
builder.Services.AddScoped<DriverRepository>();
builder.Services.AddScoped<ICbmRepository, CbmRepository>();

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
