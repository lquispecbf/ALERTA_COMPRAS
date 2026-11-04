using BE;
using DA.Configuracion;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.Threading.Tasks;

namespace DA.Repositorio
{
    public class AlertaHanaDA : IAlertaHanaDA
    {
        private readonly ConfiguracionConexion _conexion;
        private readonly AsyncRetryPolicy _retryPolicy;

        public AlertaHanaDA(IOptions<ConfiguracionConexion> conexion)
        {
            _conexion = conexion.Value;

            // Política de reintentos para desconexiones transitorias en SAP HANA
            _retryPolicy = Policy
                .Handle<OdbcException>()
                .Or<TimeoutException>()
                .WaitAndRetryAsync(
                    3,
                    intento => TimeSpan.FromSeconds(Math.Pow(2, intento)),
                    (exception, timeSpan, retryCount, context) =>
                    {
                        Console.WriteLine($"[HANA RETRY {retryCount}] Esperando {timeSpan.TotalSeconds}s - Error: {exception.Message}");
                    });
        }

        public async Task<List<BE_CompraMedicamentoHana>> ConsultarComprasMedicamentoVsCosto(DateTime? fechaEvaluacion = null, int docEntry = 0)
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                var lista = new List<BE_CompraMedicamentoHana>();

                using var con = new OdbcConnection(_conexion.CadenaSAP_ODBC);
                string proc = string.IsNullOrWhiteSpace(_conexion.CompanyDB)
                    ? "CALL \"CBF_SP_ALERTAS_COMPRAS_MEDICAMENTO_VS_COSTO\"(?, ?)"
                    : $"CALL \"{_conexion.CompanyDB.Trim()}\".\"CBF_SP_ALERTAS_COMPRAS_MEDICAMENTO_VS_COSTO\"(?, ?)";

                using var cmd = new OdbcCommand(proc, con)
                {
                    CommandType = CommandType.Text,
                    CommandTimeout = 45
                };

                cmd.Parameters.Add(new OdbcParameter
                {
                    ParameterName = "?",
                    OdbcType = OdbcType.Date,
                    Value = fechaEvaluacion.HasValue ? fechaEvaluacion.Value.Date : (object)DBNull.Value
                });

                cmd.Parameters.Add(new OdbcParameter
                {
                    ParameterName = "?",
                    OdbcType = OdbcType.Int,
                    Value = docEntry
                });

                await con.OpenAsync();
                using var reader = await cmd.ExecuteReaderAsync();

                bool tienePrecioVenta = false;
                bool tieneMotivo = false;
                try { tienePrecioVenta = reader.GetOrdinal("PRECIO_VENTA_GENERAL") >= 0; } catch { }
                try { tieneMotivo = reader.GetOrdinal("MOTIVO_ALERTA") >= 0; } catch { }

                while (await reader.ReadAsync())
                {
                    lista.Add(new BE_CompraMedicamentoHana
                    {
                        ClaveDocumento = reader["CLAVE_DOCUMENTO"]?.ToString() ?? string.Empty,
                        LineaDocumento = reader["LINEA_DOCUMENTO"] != DBNull.Value ? Convert.ToInt32(reader["LINEA_DOCUMENTO"]) : 0,
                        NumeroDocumento = reader["NUMERO_DOCUMENTO"]?.ToString() ?? string.Empty,
                        FechaDocumento = reader["FECHA_DOCUMENTO"] != DBNull.Value ? Convert.ToDateTime(reader["FECHA_DOCUMENTO"]) : DateTime.Today,
                        ProveedorCodigo = reader["PROVEEDOR_CODIGO"]?.ToString() ?? string.Empty,
                        ProveedorNombre = reader["PROVEEDOR_NOMBRE"]?.ToString() ?? string.Empty,
                        ItemCode = reader["ITEM_CODE"]?.ToString() ?? string.Empty,
                        ItemName = reader["ITEM_NAME"]?.ToString() ?? string.Empty,
                        PrecioCompra = reader["PRECIO_COMPRA"] != DBNull.Value ? Convert.ToDecimal(reader["PRECIO_COMPRA"]) : 0,
                        CostoRegistrado = reader["COSTO_REGISTRADO"] != DBNull.Value ? Convert.ToDecimal(reader["COSTO_REGISTRADO"]) : 0,
                        DiferenciaMonto = reader["DIFERENCIA_MONTO"] != DBNull.Value ? Convert.ToDecimal(reader["DIFERENCIA_MONTO"]) : 0,
                        PrecioVentaGeneral = tienePrecioVenta && reader["PRECIO_VENTA_GENERAL"] != DBNull.Value ? Convert.ToDecimal(reader["PRECIO_VENTA_GENERAL"]) : 0,
                        MotivoAlerta = tieneMotivo && reader["MOTIVO_ALERTA"] != DBNull.Value ? reader["MOTIVO_ALERTA"]?.ToString() ?? "COMPRA_GE_COSTO" : "COMPRA_GE_COSTO",
                        AlmacenCodigo = reader["ALMACEN_CODIGO"]?.ToString() ?? string.Empty,
                        EnvWms = reader["ENV_WMS"]?.ToString() ?? "Y"
                    });
                }

                return lista;
            });
        }
    }
}

