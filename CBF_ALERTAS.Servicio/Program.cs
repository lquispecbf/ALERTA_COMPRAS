using BL.Interfaces;
using BL.Rutinas;
using BL.Servicios;
using DA.Configuracion;
using DA.Repositorio;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.IO;

namespace CBF_ALERTAS.Servicio
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Host.CreateDefaultBuilder(args)
                .UseWindowsService()
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                    config.AddJsonFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    // 1. Opciones de configuración
                    services.Configure<ConfiguracionConexion>(hostContext.Configuration.GetSection("ConfiguracionConexion"));

                    // 2. Inyección de dependencias Capa DA (Acceso a Datos)
                    services.AddSingleton<IAlertaConfiguracionDA, AlertaConfiguracionDA>();
                    services.AddSingleton<IAlertaHanaDA, AlertaHanaDA>();
                    services.AddSingleton<IAlertaNotificacionDA, AlertaNotificacionDA>();

                    // 3. Inyección de dependencias Capa BL (Lógica de Negocio y Rutinas)
                    services.AddSingleton<IEmailSenderBL, EmailSenderBL>();
                    services.AddSingleton<IRutinaAlerta, RutinaCompraMedicamentoVsCosto>();
                    services.AddSingleton<IOrquestadorAlertasBL, OrquestadorAlertasBL>();

                    // 4. Servicio en segundo plano
                    services.AddHostedService<Worker>();
                })
                .UseSerilog((hostingContext, services, loggerConfiguration) =>
                {
                    var cfg = hostingContext.Configuration;
                    var logDirectory = cfg.GetValue<string>("ConfiguracionConexion:LogDirectory") ?? @"D:\LogsAlertasCBF\";

                    if (!Directory.Exists(logDirectory))
                    {
                        try { Directory.CreateDirectory(logDirectory); } catch { /* Serilog manejará error si no puede crear */ }
                    }

                    var logFilePath = Path.Combine(logDirectory, "alertas-log-.txt");

                    loggerConfiguration
                        .MinimumLevel.Information()
                        .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
                        .WriteTo.Console()
                        .WriteTo.File(
                            path: logFilePath,
                            rollingInterval: RollingInterval.Day,
                            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}"
                        )
                        .ReadFrom.Configuration(hostingContext.Configuration);
                })
                .Build()
                .Run();
        }
    }
}
