using BE;
using BL.Interfaces;
using DA.Repositorio;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BL.Rutinas
{
    public class RutinaCompraMedicamentoVsCosto : IRutinaAlerta
    {
        public string CodigoRutina => "COMPRA_MEDICAMENTO_VS_COSTO";

        private readonly IAlertaHanaDA _hanaDa;
        private readonly IAlertaNotificacionDA _notificacionDa;
        private readonly ILogger<RutinaCompraMedicamentoVsCosto> _logger;

        public RutinaCompraMedicamentoVsCosto(
            IAlertaHanaDA hanaDa, 
            IAlertaNotificacionDA notificacionDa, 
            ILogger<RutinaCompraMedicamentoVsCosto> logger)
        {
            _hanaDa = hanaDa;
            _notificacionDa = notificacionDa;
            _logger = logger;
        }

        public async Task EjecutarCicloAsync(BE_AlertaRutina configuracion, CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation("Ejecutando escaneo en SAP HANA para rutina {Rutina} (Fecha: {Fecha:yyyy-MM-dd})...", CodigoRutina, DateTime.Today);

                // Consulta de solo lectura a SAP HANA
                var compras = await _hanaDa.ConsultarComprasMedicamentoVsCosto(fechaEvaluacion: DateTime.Today);
                if (compras == null || compras.Count == 0)
                {
                    _logger.LogInformation("No se detectaron compras que cumplan la condición en este ciclo.");
                    return;
                }

                _logger.LogInformation("Detectados {Total} registros potenciales en SAP HANA. Verificando idempotencia en SQL Server...", compras.Count);

                int nuevosAlertados = 0;
                foreach (var compra in compras)
                {
                    if (stoppingToken.IsCancellationRequested)
                        break;

                    // Registro transaccional en SQL Server con control de idempotencia
                    var res = await _notificacionDa.RegistrarEventoCompra(compra, configuracion.IdRutina);
                    if (res.Codigo == 1)
                    {
                        nuevosAlertados++;
                        _logger.LogWarning("¡ALERTA GENERADA! OC #{DocNum} (DocEntry: {DocEntry}, Line: {LineNum}) | Item: {ItemCode} - {ItemName} | Compra: S/ {Precio} >= Costo: S/ {Costo}",
                            compra.NumeroDocumento, compra.ClaveDocumento, compra.LineaDocumento, compra.ItemCode, compra.ItemName, compra.PrecioCompra, compra.CostoRegistrado);
                    }
                    else
                    {
                        _logger.LogDebug("OC #{DocNum} Line {LineNum} ya fue alertada previamente. Omitiendo.", compra.NumeroDocumento, compra.LineaDocumento);
                    }
                }

                if (nuevosAlertados > 0)
                {
                    _logger.LogInformation("Ciclo completado. {Total} nuevas alertas registradas y encoladas para notificación.", nuevosAlertados);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante la ejecución de la rutina {Rutina}", CodigoRutina);
            }
        }
    }
}

