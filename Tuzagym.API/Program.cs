
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System;
using System.Collections.Generic;
using System.Text;
using Tuzagym.API.Data;
using Tuzagym.API.Services;

var builder = WebApplication.CreateBuilder(args);

// ======================================
// SERVIDOR HTTP EN RED LOCAL
// ======================================

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5192);
});

// ======================================
// CONTROLADORES
// ======================================

builder.Services.AddControllers();

// ======================================
// SERVICIOS
// ======================================

// Correo de verificacion y recordatorios
builder.Services.AddScoped<EmailService>();

// Servicio automatico de membresias
builder.Services.AddHostedService<RecordatoriosMembresiaService>();

// ======================================
// SQL SERVER
// ======================================

builder.Services.AddDbContext<ApplicationDbContext>(
    options =>
        options.UseSqlServer(
            builder.Configuration.GetConnectionString(
                "DefaultConnection")));

builder.Services.AddMemoryCache();

builder.Services.AddSingleton<RecuperacionContrasenaService>();

// ======================================
// AUTENTICACION JWT
// ======================================

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException(
        "Falta configurar Jwt:Key.");

var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException(
        "Falta configurar Jwt:Issuer.");

var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException(
        "Falta configurar Jwt:Audience.");

if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException(
        "La clave JWT debe tener al menos 32 bytes.");
}

builder.Services
    .AddAuthentication(
        JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtKey)),

                ClockSkew = TimeSpan.FromMinutes(1)
            };
    });

builder.Services.AddAuthorization();

// ======================================
// OPENAPI Y SWAGGER
// ======================================

builder.Services.AddOpenApi();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TuzaGym API",
        Version = "v1",
        Description =
            "API de gestión del gimnasio TuzaGym"
    });

    // DEFINIR AUTENTICACION JWT
    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description =
                "Introduce tu token JWT sin escribir Bearer."
        });

    // APLICAR JWT A LAS SOLICITUDES
    options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(
                "Bearer", document)] = new List<string>()
        });
});

// ======================================
// CREAR APLICACION
// ======================================

var app = builder.Build();

// ======================================
// SWAGGER EN DESARROLLO
// ======================================

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwagger();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/swagger/v1/swagger.json",
            "TuzaGym API v1");

        options.RoutePrefix = "swagger";
    });
}

// ======================================
// MIDDLEWARE
// ======================================

// HTTP LOCAL PARA PRUEBAS
// app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// ======================================
// CONTROLADORES
// ======================================

app.MapControllers();



// ======================================
// PRUEBA TEMPORAL DE CORREO
// ======================================

if (app.Environment.IsDevelopment())
{
    app.MapPost("/api/prueba-correo-yair", async (
        ApplicationDbContext db,
        EmailService email) =>
    {
        var socio = await db.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(u =>
                u.IdUsuario == 7 &&
                u.Rol == "Socio");

        if (socio == null)
        {
            return Results.NotFound(
                "No se encontró al socio Yair.");
        }

        if (string.IsNullOrWhiteSpace(socio.Correo))
        {
            return Results.BadRequest(
                "Yair no tiene correo registrado.");
        }

        if (socio.FechaVencimiento == null)
        {
            return Results.BadRequest(
                "Yair no tiene fecha de vencimiento.");
        }

        bool enviado =
            await email.EnviarMembresiaVencidaAsync(
                socio.Correo,
                socio.Nombre,
                socio.TipoMembresia ?? "Normal",
                socio.FechaVencimiento.Value);

        return enviado
            ? Results.Ok(
                "El servidor SMTP aceptó el correo de prueba.")
            : Results.Problem(
                "Falló el envío. Revisa la consola de la API.");
    });
}

// ======================================
// INICIAR SERVIDOR
// ======================================

app.Run();
