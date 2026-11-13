
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Tuzagym.API.Data;

namespace Tuzagym.API.Controllers
{
    [ApiController]
    [Route("api/reportes")]
    [Authorize(Roles = "Administrador")]
    public class ReportesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        private static readonly CultureInfo Cultura =
            CultureInfo.GetCultureInfo("es-MX");

        private const string Verde = "#39E600";
        private const string VerdeOscuro = "#168A25";
        private const string Negro = "#111820";
        private const string Gris = "#64748B";
        private const string GrisClaro = "#F1F5F9";
        private const string Rojo = "#E5484D";
        private const string Blanco = "#FFFFFF";

        public ReportesController(
            ApplicationDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [HttpGet("financiero-pdf")]
        public async Task<IActionResult> DescargarReporte()
        {
            DateTime ahora = DateTime.Now;

            DateTime inicio = new DateTime(
                ahora.Year, ahora.Month, 1);

            DateTime fin = inicio.AddMonths(1);

            // Consultar los últimos seis meses.
            DateTime inicioHistorico = inicio.AddMonths(-5);

            var pagos = await _context.Pagos
                .AsNoTracking()
                .Where(p =>
                    p.Fecha >= inicioHistorico &&
                    p.Fecha < fin)
                .Select(p => new
                {
                    p.Fecha,
                    p.Monto
                })
                .ToListAsync();

            var egresos = await _context.Gastos
                .AsNoTracking()
                .Where(g =>
                    g.Fecha >= inicioHistorico &&
                    g.Fecha < fin)
                .Select(g => new
                {
                    g.Fecha,
                    g.Monto
                })
                .ToListAsync();

            decimal ventas = pagos
                .Where(p => p.Fecha >= inicio)
                .Sum(p => p.Monto);

            decimal gastos = egresos
                .Where(g => g.Fecha >= inicio)
                .Sum(g => g.Monto);

            decimal ganancias = ventas - gastos;

            int cantidadPagos = pagos.Count(
                p => p.Fecha >= inicio);

            int cantidadGastos = egresos.Count(
                g => g.Fecha >= inicio);

            // Datos históricos de seis meses.
            var historial = Enumerable.Range(0, 6)
                .Select(i =>
                {
                    DateTime mes = inicioHistorico.AddMonths(i);
                    DateTime siguiente = mes.AddMonths(1);

                    return new
                    {
                        Nombre = mes.ToString("MMM yy", Cultura),
                        Ventas = pagos
                            .Where(p =>
                                p.Fecha >= mes &&
                                p.Fecha < siguiente)
                            .Sum(p => p.Monto),

                        Gastos = egresos
                            .Where(g =>
                                g.Fecha >= mes &&
                                g.Fecha < siguiente)
                            .Sum(g => g.Monto)
                    };
                })
                .ToList();

            // Buscar el logotipo en el servidor.
            string rutaLogo = Path.Combine(
                _environment.ContentRootPath,
                "wwwroot",
                "images",
                "logo_tuza.png");

            byte[]? logo = System.IO.File.Exists(rutaLogo)
                ? await System.IO.File.ReadAllBytesAsync(rutaLogo)
                : null;

            QuestPDF.Settings.License = LicenseType.Community;

            byte[] pdf = QuestPDF.Fluent.Document.Create(doc =>
            {
                doc.Page(pagina =>
                {
                    pagina.Size(PageSizes.A4);
                    pagina.Margin(30);

                    pagina.DefaultTextStyle(estilo =>
                        estilo.FontSize(10)
                            .FontColor(Negro));

                    // ENCABEZADO
                    pagina.Header()
                        .Background(Negro)
                        .Padding(15)
                        .Row(fila =>
                        {
                            fila.ConstantItem(85)
                                .Height(75)
                                .Element(contenedor =>
                                {
                                    if (logo != null)
                                    {
                                        contenedor.Image(logo)
                                            .FitArea();
                                    }
                                    else
                                    {
                                        contenedor
                                            .AlignCenter()
                                            .AlignMiddle()
                                            .Text("TUZA")
                                            .Bold()
                                            .FontSize(18)
                                            .FontColor(Verde);
                                    }
                                });

                            fila.RelativeItem()
                                .PaddingLeft(15)
                                .AlignMiddle()
                                .Column(col =>
                                {
                                    col.Item()
                                        .Text("TUZA GYM")
                                        .Bold()
                                        .FontSize(22)
                                        .FontColor(Verde);

                                    col.Item()
                                        .Text("REPORTE FINANCIERO")
                                        .Bold()
                                        .FontSize(15)
                                        .FontColor(Blanco);

                                    col.Item()
                                        .Text(
                                            inicio.ToString(
                                                "MMMM yyyy", Cultura))
                                        .FontSize(10)
                                        .FontColor("#CBD5E1");
                                });
                        });

                    // CONTENIDO
                    pagina.Content()
                        .PaddingTop(20)
                        .Column(col =>
                        {
                            col.Spacing(15);

                            // TITULO
                            col.Item()
                                .Text("RESUMEN GENERAL")
                                .Bold()
                                .FontSize(16);

                            // TARJETAS FINANCIERAS
                            col.Item().Row(fila =>
                            {
                                fila.Spacing(8);

                                Tarjeta(
                                    fila.RelativeItem(),
                                    "INGRESOS",
                                    Dinero(ventas),
                                    VerdeOscuro,
                                    "#EAF9EC");

                                Tarjeta(
                                    fila.RelativeItem(),
                                    "GASTOS",
                                    Dinero(gastos),
                                    Rojo,
                                    "#FDEDEE");

                                Tarjeta(
                                    fila.RelativeItem(),
                                    "GANANCIA NETA",
                                    Dinero(ganancias),
                                    ganancias >= 0
                                        ? VerdeOscuro
                                        : Rojo,
                                    GrisClaro);
                            });

                            // INDICADORES
                            col.Item()
                                .Background(GrisClaro)
                                .Padding(12)
                                .Row(fila =>
                                {
                                    fila.RelativeItem()
                                        .Column(c =>
                                        {
                                            c.Item()
                                                .Text("PAGOS REGISTRADOS")
                                                .FontSize(9)
                                                .FontColor(Gris);

                                            c.Item()
                                                .Text(
                                                    cantidadPagos.ToString())
                                                .Bold()
                                                .FontSize(18);
                                        });

                                    fila.RelativeItem()
                                        .Column(c =>
                                        {
                                            c.Item()
                                                .Text("GASTOS REGISTRADOS")
                                                .FontSize(9)
                                                .FontColor(Gris);

                                            c.Item()
                                                .Text(
                                                    cantidadGastos.ToString())
                                                .Bold()
                                                .FontSize(18);
                                        });

                                    fila.RelativeItem()
                                        .Column(c =>
                                        {
                                            c.Item()
                                                .Text("MARGEN NETO")
                                                .FontSize(9)
                                                .FontColor(Gris);

                                            decimal margen = ventas == 0
                                                ? 0
                                                : ganancias / ventas * 100;

                                            c.Item()
                                                .Text($"{margen:N1}%")
                                                .Bold()
                                                .FontSize(18);
                                        });
                                });

                            // GRAFICA HISTORICA
                            col.Item()
                                .Text("INGRESOS VS. GASTOS")
                                .Bold()
                                .FontSize(15);

                            col.Item()
                                .Text("Comparativo de los últimos 6 meses")
                                .FontSize(9)
                                .FontColor(Gris);

                            decimal maximo = historial
                                .SelectMany(h => new[]
                                {
                                    h.Ventas,
                                    h.Gastos
                                })
                                .DefaultIfEmpty(0)
                                .Max();

                            if (maximo <= 0)
                                maximo = 1;

                            col.Item()
                                .Border(1)
                                .BorderColor("#E2E8F0")
                                .Padding(12)
                                .Column(grafica =>
                                {
                                    grafica.Spacing(9);

                                    foreach (var mes in historial)
                                    {
                                        grafica.Item()
                                            .Text(mes.Nombre.ToUpper(Cultura))
                                            .Bold()
                                            .FontSize(9);

                                        Barra(
                                            grafica.Item(),
                                            "Ingresos",
                                            mes.Ventas,
                                            maximo,
                                            VerdeOscuro);

                                        Barra(
                                            grafica.Item(),
                                            "Gastos",
                                            mes.Gastos,
                                            maximo,
                                            Rojo);
                                    }
                                });

                            // DISTRIBUCION DEL MES
                            col.Item()
                                .Text("DISTRIBUCIÓN FINANCIERA")
                                .Bold()
                                .FontSize(15);

                            decimal totalMovimientos =
                                ventas + gastos;

                            decimal porcentajeVentas =
                                totalMovimientos > 0
                                    ? ventas / totalMovimientos * 100
                                    : 0;

                            decimal porcentajeGastos =
                                totalMovimientos > 0
                                    ? gastos / totalMovimientos * 100
                                    : 0;

                            col.Item()
                                .Border(1)
                                .BorderColor("#E2E8F0")
                                .Padding(12)
                                .Column(distribucion =>
                                {
                                    distribucion.Spacing(8);

                                    distribucion.Item()
                                        .Text(
                                            $"Ingresos: {porcentajeVentas:N1}%");

                                    BarraPorcentaje(
                                        distribucion.Item(),
                                        porcentajeVentas,
                                        VerdeOscuro);

                                    distribucion.Item()
                                        .Text(
                                            $"Gastos: {porcentajeGastos:N1}%");

                                    BarraPorcentaje(
                                        distribucion.Item(),
                                        porcentajeGastos,
                                        Rojo);
                                });

                            // RESULTADO FINAL
                            col.Item()
                                .Background(
                                    ganancias >= 0
                                        ? "#EAF9EC"
                                        : "#FDEDEE")
                                .Border(1)
                                .BorderColor(
                                    ganancias >= 0
                                        ? VerdeOscuro
                                        : Rojo)
                                .Padding(15)
                                .Column(resultado =>
                                {
                                    resultado.Item()
                                        .Text("RESULTADO NETO DEL MES")
                                        .Bold()
                                        .FontSize(12);

                                    resultado.Item()
                                        .Text(Dinero(ganancias))
                                        .Bold()
                                        .FontSize(25)
                                        .FontColor(
                                            ganancias >= 0
                                                ? VerdeOscuro
                                                : Rojo);

                                    resultado.Item()
                                        .Text(
                                            "Resultado calculado como " +
                                            "ingresos menos gastos registrados.")
                                        .FontSize(9)
                                        .FontColor(Gris);
                                });

                            col.Item()
                                .Text(
                                    "Este documento refleja los movimientos " +
                                    "registrados en el sistema para el periodo " +
                                    "indicado. Los importes están expresados " +
                                    "en pesos mexicanos.")
                                .FontSize(8)
                                .FontColor(Gris);
                        });

                    // PIE DE PAGINA
                    pagina.Footer()
                        .BorderTop(1)
                        .BorderColor("#D1D5DB")
                        .PaddingTop(8)
                        .Row(fila =>
                        {
                            fila.RelativeItem()
                                .Text(
                                    $"TUZA GYM | {ahora:dd/MM/yyyy HH:mm}")
                                .FontSize(8)
                                .FontColor(Gris);

                            fila.ConstantItem(100)
                                .AlignRight()
                                .Text(texto =>
                                {
                                    texto.Span("Página ");
                                    texto.CurrentPageNumber();
                                    texto.Span(" de ");
                                    texto.TotalPages();
                                });
                        });
                });
            }).GeneratePdf();

            return File(
                pdf,
                "application/pdf",
                $"TUZA_Reporte_{inicio:yyyy_MM}.pdf");
        }

        // FORMATO MONETARIO
        private static string Dinero(decimal valor)
        {
            return valor.ToString("C2", Cultura);
        }

        // TARJETA DE INDICADOR
        private static void Tarjeta(
            IContainer contenedor,
            string titulo,
            string valor,
            string color,
            string fondo)
        {
            contenedor
                .Background(fondo)
                .Border(1)
                .BorderColor("#E2E8F0")
                .Padding(10)
                .Column(col =>
                {
                    col.Spacing(7);

                    col.Item()
                        .Text(titulo)
                        .Bold()
                        .FontSize(9)
                        .FontColor(Gris);

                    col.Item()
                        .Text(valor)
                        .Bold()
                        .FontSize(15)
                        .FontColor(color);
                });
        }

        // BARRA COMPARATIVA
        private static void Barra(
            IContainer contenedor,
            string etiqueta,
            decimal valor,
            decimal maximo,
            string color)
        {
            float porcentaje = (float)(
                Math.Max(0, valor) / maximo);

            porcentaje = Math.Clamp(
                porcentaje, 0f, 1f);

            contenedor.Row(fila =>
            {
                fila.ConstantItem(48)
                    .AlignMiddle()
                    .Text(etiqueta)
                    .FontSize(8);

                fila.RelativeItem()
                    .Height(12)
                    .Background(GrisClaro)
                    .Row(barra =>
                    {
                        if (porcentaje > 0)
                        {
                            barra.RelativeItem(porcentaje)
                                .Background(color);
                        }

                        if (porcentaje < 1)
                        {
                            barra.RelativeItem(1 - porcentaje);
                        }
                    });

                fila.ConstantItem(85)
                    .AlignRight()
                    .AlignMiddle()
                    .Text(Dinero(valor))
                    .FontSize(8);
            });
        }

        // BARRA DE PORCENTAJE
        private static void BarraPorcentaje(
            IContainer contenedor,
            decimal porcentaje,
            string color)
        {
            float proporcion = Math.Clamp(
                (float)(porcentaje / 100m),
                0f,
                1f);

            contenedor
                .Height(14)
                .Background(GrisClaro)
                .Row(fila =>
                {
                    if (proporcion > 0)
                    {
                        fila.RelativeItem(proporcion)
                            .Background(color);
                    }

                    if (proporcion < 1)
                    {
                        fila.RelativeItem(1 - proporcion);
                    }
                });
        }
    }
}
