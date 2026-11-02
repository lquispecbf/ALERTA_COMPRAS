using BE;
using BL.Interfaces;
using DA.Repositorio;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BL.Servicios
{
    public class OrquestadorAlertasBL : IOrquestadorAlertasBL
    {
        private readonly IAlertaConfiguracionDA _configDa;
        private readonly IEnumerable<IRutinaAlerta> _rutinasImplementadas;
        private readonly IEmailSenderBL _emailSender;
        private readonly ILogger<OrquestadorAlertasBL> _logger;

        private int _versionConfigLocal = -1;
        private readonly Dictionary<int, BE_AlertaRutina> _rutinasActivas = new();
        private readonly Dictionary<int, DateTime> _ultimaEjecucion = new();

        public OrquestadorAlertasBL(
            IAlertaConfiguracionDA configDa,
            IEnumerable<IRutinaAlerta> rutinasImplementadas,
            IEmailSenderBL emailSender,
            ILogger<OrquestadorAlertasBL> logger)
        {
            _configDa = configDa;
            _rutinasImplementadas = rutinasImplementadas;
            _emailSender = emailSender;
            _logger = logger;
        }

        public async Task IniciarAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Iniciando Orquestador de Alertas CBF...");

            // Carga inicial obligatoria de configuraciones
            await RecargarConfiguracionesAsync();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // 1. Verificación periódica de cambios en la configuración (Hot-Reload)
                    await VerificarCambioConfiguracionAsync();

                    // 2. Evaluación de rutinas según sus intervalos en segundos
                    foreach (var kvp in _rutinasActivas.ToList())
                    {
                        if (stoppingToken.IsCancellationRequested)
                            break;

                        int idRutina = kvp.Key;
                        var configRutina = kvp.Value;

                        DateTime ultima = _ultimaEjecucion.ContainsKey(idRutina) ? _ultimaEjecucion[idRutina] : DateTime.MinValue;
                        double segundosTranscurridos = (DateTime.Now - ultima).TotalSeconds;

                        if (segundosTranscurridos >= configRutina.IntervaloSegundos)
                        {
                            var rutinaImpl = _rutinasImplementadas.FirstOrDefault(r => r.CodigoRutina.Equals(configRutina.CodigoRutina, StringComparison.OrdinalIgnoreCase));
                            if (rutinaImpl != null)
                            {
                                await rutinaImpl.EjecutarCicloAsync(configRutina, stoppingToken);
                                _ultimaEjecucion[idRutina] = DateTime.Now;
                            }
                            else
                            {
                                _logger.LogWarning("No se encontró implementación en código para la rutina {CodigoRutina}", configRutina.CodigoRutina);
                                _ultimaEjecucion[idRutina] = DateTime.Now;
                            }
                        }
                    }

                    // 3. Procesamiento de la cola de correos electrónicos
                    await _emailSender.ProcesarColaCorreosAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error en el ciclo principal del Orquestador de Alertas.");
                }

                // Pausa corta de 3 segundos entre iteraciones del bucle maestro
                try
                {
                    await Task.Delay(3000, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            _logger.LogInformation("Orquestador de Alertas CBF detenido correctamente.");
        }

        private async Task VerificarCambioConfiguracionAsync()
        {
            try
            {
                var global = await _configDa.ObtenerVersionConfig();
                if (global != null && (global.VersionConfig > _versionConfigLocal || _rutinasActivas.Count == 0))
                {
                    _logger.LogInformation("Se detectó actualización de configuración en Intranet (Versión BD: {VersionBD}, Local: {VersionLocal}). Recargando en caliente...",
                        global.VersionConfig, _versionConfigLocal);

                    await RecargarConfiguracionesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("No se pudo verificar la versión de configuración en SQL Server: {Mensaje}", ex.Message);
            }
        }

        private async Task RecargarConfiguracionesAsync()
        {
            try
            {
                var global = await _configDa.ObtenerVersionConfig();
                var lista = await _configDa.ListarRutinas(soloActivas: true);

                if (lista != null)
                {
                    _rutinasActivas.Clear();
                    foreach (var r in lista)
                    {
                        // Solo orquestar rutinas que CBF_ALERTAS tiene implementadas en su código
                        // Otras rutinas en ALERTA_RUTINA (como CONTINGENCIA_WMS) son gestionadas por sus propios servicios Windows
                        if (_rutinasImplementadas.Any(impl => impl.CodigoRutina.Equals(r.CodigoRutina, StringComparison.OrdinalIgnoreCase)))
                        {
                            _rutinasActivas[r.IdRutina] = r;
                            if (!_ultimaEjecucion.ContainsKey(r.IdRutina))
                            {
                                // Para que la primera ejecución sea inmediata
                                _ultimaEjecucion[r.IdRutina] = DateTime.MinValue;
                            }
                        }
                    }

                    _versionConfigLocal = global?.VersionConfig ?? 1;
                    _logger.LogInformation("Configuraciones recargadas exitosamente. Rutinas activas: {TotalRutinas}. Versión: {Version}",
                        _rutinasActivas.Count, _versionConfigLocal);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al recargar configuraciones de alertas en SQL Server. Se mantendrá la configuración previa en memoria hasta restablecer conexión.");
            }
        }
    }
}

