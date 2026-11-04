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
                int diasAtras = Math.Max(0, configuracion.DiasAtras);
                int nuevosAlertadosTotal = 0;

                for (int d = diasAtras; d >= 0; d--)
                {
                    if (stoppingToken.IsCancellationRequested)
                        break;

                    var fechaEval = DateTime.Today.AddDays(-d);
                    _logger.LogInformation("Ejecutando escaneo en SAP HANA para rutina {Rutina} (Fecha: {Fecha:yyyy-MM-dd}, Días Atrás: {Dias})...", 
                        CodigoRutina, fechaEval, d);

                    // Consulta de solo lectura a SAP HANA
                    var compras = await _hanaDa.ConsultarComprasMedicamentoVsCosto(fechaEvaluacion: fechaEval);
                    if (compras == null || compras.Count == 0)
                    {
                        _logger.LogDebug("Sin registros que cumplan condiciones en SAP HANA para fecha {Fecha:yyyy-MM-dd}.", fechaEval);
                        continue;
                    }

                    _logger.LogInformation("Detectados {Total} registros potenciales en SAP HANA para {Fecha:yyyy-MM-dd}. Verificando idempotencia en SQL Server...", 
                        compras.Count, fechaEval);

                    int nuevosAlertadosDia = 0;
                    foreach (var compra in compras)
                    {
                        if (stoppingToken.IsCancellationRequested)
                            break;

                        // Registro transaccional en SQL Server con control de idempotencia
                        var res = await _notificacionDa.RegistrarEventoCompra(compra, configuracion.IdRutina);
                        if (res.Codigo == 1)
                        {
                            nuevosAlertadosDia++;
                            nuevosAlertadosTotal++;

                            if (compra.MotivoAlerta == "COMPRA_EQ_VENTA_GRAL")
                            {
                                _logger.LogWarning("¡ALERTA GENERADA! [COMPRA = VENTA GENERAL] OC #{DocNum} (DocEntry: {DocEntry}, Line: {LineNum}) | Item: {ItemCode} - {ItemName} | Compra: S/ {Precio} == Venta Gral: S/ {VentaGral}",
                                    compra.NumeroDocumento, compra.ClaveDocumento, compra.LineaDocumento, compra.ItemCode, compra.ItemName, compra.PrecioCompra, compra.PrecioVentaGeneral);
                            }
                            else if (compra.MotivoAlerta == "AMBAS_CONDICIONES")
                            {
                                _logger.LogWarning("¡ALERTA GENERADA! [AMBAS CONDICIONES] OC #{DocNum} (DocEntry: {DocEntry}, Line: {LineNum}) | Item: {ItemCode} - {ItemName} | Compra: S/ {Precio} >= Costo: S/ {Costo} Y == Venta Gral: S/ {VentaGral}",
                                    compra.NumeroDocumento, compra.ClaveDocumento, compra.LineaDocumento, compra.ItemCode, compra.ItemName, compra.PrecioCompra, compra.CostoRegistrado, compra.PrecioVentaGeneral);
                            }
                            else
                            {
                                _logger.LogWarning("¡ALERTA GENERADA! [COMPRA >= COSTO SAP] OC #{DocNum} (DocEntry: {DocEntry}, Line: {LineNum}) | Item: {ItemCode} - {ItemName} | Compra: S/ {Precio} >= Costo: S/ {Costo}",
                                    compra.NumeroDocumento, compra.ClaveDocumento, compra.LineaDocumento, compra.ItemCode, compra.ItemName, compra.PrecioCompra, compra.CostoRegistrado);
                            }
                        }
                        else
                        {
                            _logger.LogDebug("OC #{DocNum} Line {LineNum} ya fue alertada previamente. Omitiendo.", compra.NumeroDocumento, compra.LineaDocumento);
                        }
                    }

                    if (nuevosAlertadosDia > 0)
                    {
                        _logger.LogInformation("Fecha {Fecha:yyyy-MM-dd}: {Total} nuevas alertas registradas y encoladas.", fechaEval, nuevosAlertadosDia);
                    }
                }

                if (nuevosAlertadosTotal > 0)
                {
                    _logger.LogInformation("Ciclo completado. {Total} nuevas alertas registradas en total.", nuevosAlertadosTotal);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante la ejecución de la rutina {Rutina}", CodigoRutina);
            }
        }
    }
}

