using Microsoft.EntityFrameworkCore;
using Negocio.Web.Data;
using Negocio.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
var builder = WebApplication.CreateBuilder(args);

// =======================
// SERVICES
// =======================

// MVC
builder.Services.AddControllersWithViews();

// 🔐 REQUIRED FOR SESSION (ESTO FALTABA)
builder.Services.AddDistributedMemoryCache();


// 🔥 ESTE ES EL QUE FALTA PARA EL GMAIL
builder.Services.AddScoped<EmailService>();
// Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/Login/Index";
        options.AccessDeniedPath = "/Login/Index";
    }) // ← Le quitamos el punto y coma aquí para seguir la cadena
    .AddGoogle(options =>
    {
        // 🔥 INYECTAMOS TUS LLAVES CLIENTE DE GOOGLE CLOUD
        options.ClientId = "nada";
        options.ClientSecret = "todavia";

        // Sincroniza el inicio de Google con tu esquema de cookies actual
        options.SignInScheme = "Cookies";
    }); // ← El punto y coma va al final de todo el bloque

builder.Services.AddAuthorization();


// DbContext
builder.Services.AddDbContext<NegocioDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("NegocioConnection")
    )
);

// Services
builder.Services.AddScoped<VentaService>();




// 🔥 LA LÍNEA MÁGICA AQUÍ
builder.Services.AddHttpContextAccessor();

// =======================
var app = builder.Build();
// =======================


//=======================
// SEEDER
// =======================
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<NegocioDbContext>();
    DbSeeder.SeedUsuarios(context);
}

// =======================
// MIDDLEWARE
// =======================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// 🔴 EN SOMEe RECOMENDADO DESACTIVAR HTTPS
// app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

// Session SIEMPRE antes de Authorization
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=HomePublic}/{action=Index}/{id?}");

app.Run();
