using BL.Servicios;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CBF_ALERTAS.Servicio
{
    public class Worker : BackgroundService
    {
        private readonly IOrquestadorAlertasBL _orquestador;
        private readonly ILogger<Worker> _logger;

        public Worker(IOrquestadorAlertasBL orquestador, ILogger<Worker> logger)
        {
            _orquestador = orquestador;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Servicio Windows de Alertas CBF iniciado a las {Fecha:yyyy-MM-dd HH:mm:ss}", DateTime.Now);

            try
            {
                await _orquestador.IniciarAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Se recibió señal de detención del Servicio Windows de Alertas CBF.");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Error fatal no controlado en el Servicio Windows de Alertas CBF.");
            }
            finally
            {
                _logger.LogInformation("Servicio Windows de Alertas CBF finalizado.");
            }
        }
    }
}
