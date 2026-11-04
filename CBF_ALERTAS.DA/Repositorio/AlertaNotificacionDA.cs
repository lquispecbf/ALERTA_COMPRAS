using BE;
using DA.Configuracion;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace DA.Repositorio
{
    public class AlertaNotificacionDA : IAlertaNotificacionDA
    {
        private readonly ConfiguracionConexion _conexion;

        public AlertaNotificacionDA(IOptions<ConfiguracionConexion> conexion)
        {
            _conexion = conexion.Value;
        }

        private SqlConnection CrearConexion()
        {
            var cs = _conexion.CadenaSQL ?? string.Empty;
            if (!cs.Contains("TrustServerCertificate", StringComparison.OrdinalIgnoreCase))
            {
                cs = cs.TrimEnd(';') + ";TrustServerCertificate=True;";
            }
            return new SqlConnection(cs);
        }

        public async Task<BE_RespuestaBD> RegistrarEventoCompra(BE_CompraMedicamentoHana evento, int idRutina)
        {
            var res = new BE_RespuestaBD();
            using var con = CrearConexion();
            using var cmd = new SqlCommand("dbo.SP_REGISTRAR_ALERTA_EVENTOS_COMPRA", con)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add(new SqlParameter("@ID_RUTINA", SqlDbType.Int) { Value = idRutina });
            cmd.Parameters.Add(new SqlParameter("@CLAVE_DOCUMENTO", SqlDbType.VarChar, 50) { Value = evento.ClaveDocumento });
            cmd.Parameters.Add(new SqlParameter("@LINEA_DOCUMENTO", SqlDbType.Int) { Value = evento.LineaDocumento });
            cmd.Parameters.Add(new SqlParameter("@NUMERO_DOCUMENTO", SqlDbType.VarChar, 50) { Value = evento.NumeroDocumento });
            cmd.Parameters.Add(new SqlParameter("@FECHA_DOCUMENTO", SqlDbType.Date) { Value = evento.FechaDocumento });
            cmd.Parameters.Add(new SqlParameter("@PROVEEDOR_CODIGO", SqlDbType.VarChar, 50) { Value = (object?)evento.ProveedorCodigo ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@PROVEEDOR_NOMBRE", SqlDbType.NVarChar, 250) { Value = (object?)evento.ProveedorNombre ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@ITEM_CODE", SqlDbType.VarChar, 50) { Value = evento.ItemCode });
            cmd.Parameters.Add(new SqlParameter("@ITEM_NAME", SqlDbType.NVarChar, 250) { Value = (object?)evento.ItemName ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@PRECIO_COMPRA", SqlDbType.Decimal) { Precision = 18, Scale = 4, Value = evento.PrecioCompra });
            cmd.Parameters.Add(new SqlParameter("@COSTO_REGISTRADO", SqlDbType.Decimal) { Precision = 18, Scale = 4, Value = evento.CostoRegistrado });
            cmd.Parameters.Add(new SqlParameter("@ALMACEN_CODIGO", SqlDbType.VarChar, 50) { Value = (object?)evento.AlmacenCodigo ?? DBNull.Value });
            string detalleJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                motivoAlerta = string.IsNullOrWhiteSpace(evento.MotivoAlerta) ? "COMPRA_GE_COSTO" : evento.MotivoAlerta,
                precioVentaGeneral = evento.PrecioVentaGeneral
            });

            cmd.Parameters.Add(new SqlParameter("@ENV_WMS", SqlDbType.Char, 1) { Value = evento.EnvWms });
            cmd.Parameters.Add(new SqlParameter("@DETALLE_JSON", SqlDbType.NVarChar, -1) { Value = (object)detalleJson ?? DBNull.Value });

            await con.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                res.Codigo = Convert.ToInt32(reader["Resultado"]);
                res.Mensaje = reader["Mensaje"]?.ToString() ?? string.Empty;
                res.IdGenerado = Convert.ToInt64(reader["ID_EVENTO"]);
            }

            return res;
        }

        public async Task<(int TotalNoLeidas, List<BE_AlertaNotificacionIntranet> Alertas)> ObtenerNotificacionesUsuario(string idUsuario, int top = 10)
        {
            int totalNoLeidas = 0;
            var lista = new List<BE_AlertaNotificacionIntranet>();

            using var con = CrearConexion();
            using var cmd = new SqlCommand("dbo.SP_OBTENER_NOTIFICACIONES_INTRANET_USUARIO", con)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.Add(new SqlParameter("@ID_USUARIO", SqlDbType.VarChar, 50) { Value = idUsuario });
            cmd.Parameters.Add(new SqlParameter("@TOP", SqlDbType.Int) { Value = top });

            await con.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            // Result set 1: Total no leídas
            if (await reader.ReadAsync())
            {
                totalNoLeidas = Convert.ToInt32(reader["TOTAL_NO_LEIDAS"]);
            }

            // Result set 2: Lista de notificaciones
            if (await reader.NextResultAsync())
            {
                while (await reader.ReadAsync())
                {
                    lista.Add(new BE_AlertaNotificacionIntranet
                    {
                        IdNotificacion = Convert.ToInt64(reader["ID_NOTIFICACION"]),
                        IdEvento = Convert.ToInt64(reader["ID_EVENTO"]),
                        IdUsuario = reader["ID_USUARIO"]?.ToString() ?? string.Empty,
                        Titulo = reader["TITULO"]?.ToString() ?? string.Empty,
                        Mensaje = reader["MENSAJE"]?.ToString() ?? string.Empty,
                        NivelAlerta = reader["NIVEL_ALERTA"]?.ToString() ?? "WARNING",
                        Leido = Convert.ToBoolean(reader["LEIDO"]),
                        FechaLectura = reader["FECHA_LECTURA"] != DBNull.Value ? Convert.ToDateTime(reader["FECHA_LECTURA"]) : null,
                        Confirmado = Convert.ToBoolean(reader["CONFIRMADO"]),
                        FechaConfirmacion = reader["FECHA_CONFIRMACION"] != DBNull.Value ? Convert.ToDateTime(reader["FECHA_CONFIRMACION"]) : null,
                        ComentarioConfirmacion = reader["COMENTARIO_CONFIRMACION"] != DBNull.Value ? reader["COMENTARIO_CONFIRMACION"].ToString() : null,
                        FechaCreacion = Convert.ToDateTime(reader["FECHA_CREACION"]),
                        NumeroDocumento = reader["NUMERO_DOCUMENTO"]?.ToString(),
                        ItemCode = reader["ITEM_CODE"]?.ToString(),
                        ItemName = reader["ITEM_NAME"]?.ToString(),
                        PrecioCompra = reader["PRECIO_COMPRA"] != DBNull.Value ? Convert.ToDecimal(reader["PRECIO_COMPRA"]) : null,
                        CostoRegistrado = reader["COSTO_REGISTRADO"] != DBNull.Value ? Convert.ToDecimal(reader["COSTO_REGISTRADO"]) : null
                    });
                }
            }

            return (totalNoLeidas, lista);
        }

        public async Task<BE_RespuestaBD> MarcarLeida(long idNotificacion, string idUsuario)
        {
            var res = new BE_RespuestaBD();
            using var con = CrearConexion();
            using var cmd = new SqlCommand("dbo.SP_MARCAR_ALERTA_INTRANET_LEIDA", con)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.Add(new SqlParameter("@ID_NOTIFICACION", SqlDbType.BigInt) { Value = idNotificacion });
            cmd.Parameters.Add(new SqlParameter("@ID_USUARIO", SqlDbType.VarChar, 50) { Value = idUsuario });

            await con.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                res.Codigo = Convert.ToInt32(reader["Resultado"]);
                res.Mensaje = reader["Mensaje"]?.ToString() ?? string.Empty;
                res.IdGenerado = idNotificacion;
            }

            return res;
        }

        public async Task<BE_RespuestaBD> ConfirmarAlerta(long idNotificacion, string idUsuario, string? comentario = null)
        {
            var res = new BE_RespuestaBD();
            using var con = CrearConexion();
            using var cmd = new SqlCommand("dbo.SP_CONFIRMAR_ALERTA_INTRANET", con)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.Add(new SqlParameter("@ID_NOTIFICACION", SqlDbType.BigInt) { Value = idNotificacion });
            cmd.Parameters.Add(new SqlParameter("@ID_USUARIO", SqlDbType.VarChar, 50) { Value = idUsuario });
            cmd.Parameters.Add(new SqlParameter("@COMENTARIO", SqlDbType.NVarChar, 500) { Value = (object?)comentario ?? DBNull.Value });

            await con.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                res.Codigo = Convert.ToInt32(reader["Resultado"]);
                res.Mensaje = reader["Mensaje"]?.ToString() ?? string.Empty;
                res.IdGenerado = idNotificacion;
            }

            return res;
        }

        public async Task<List<BE_AlertaColaCorreo>> ObtenerCorreosPendientes(int top = 50)
        {
            var lista = new List<BE_AlertaColaCorreo>();

            using var con = CrearConexion();
            using var cmd = new SqlCommand("dbo.SP_OBTENER_CORREOS_PENDIENTES_ENVIO", con)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.Add(new SqlParameter("@TOP", SqlDbType.Int) { Value = top });

            await con.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                lista.Add(new BE_AlertaColaCorreo
                {
                    IdCola = Convert.ToInt64(reader["ID_COLA"]),
                    IdEvento = Convert.ToInt64(reader["ID_EVENTO"]),
                    DestinatarioCorreo = reader["DESTINATARIO_CORREO"]?.ToString() ?? string.Empty,
                    DestinatarioNombre = reader["DESTINATARIO_NOMBRE"] != DBNull.Value ? reader["DESTINATARIO_NOMBRE"].ToString() : null,
                    Asunto = reader["ASUNTO"]?.ToString() ?? string.Empty,
                    CuerpoHtml = reader["CUERPO_HTML"]?.ToString() ?? string.Empty,
                    EstadoEnvio = reader["ESTADO_ENVIO"]?.ToString() ?? "PENDIENTE",
                    Intentos = Convert.ToInt32(reader["INTENTOS"]),
                    FechaCreacion = Convert.ToDateTime(reader["FECHA_CREACION"])
                });
            }

            return lista;
        }

        public async Task<BE_RespuestaBD> ActualizarEstadoCorreo(long idCola, string estadoEnvio, string? errorMensaje = null)
        {
            var res = new BE_RespuestaBD();
            using var con = CrearConexion();
            using var cmd = new SqlCommand("dbo.SP_ACTUALIZAR_ESTADO_ENVIO_CORREO", con)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.Add(new SqlParameter("@ID_COLA", SqlDbType.BigInt) { Value = idCola });
            cmd.Parameters.Add(new SqlParameter("@ESTADO_ENVIO", SqlDbType.VarChar, 20) { Value = estadoEnvio });
            cmd.Parameters.Add(new SqlParameter("@ERROR_MENSAJE", SqlDbType.NVarChar, -1) { Value = (object?)errorMensaje ?? DBNull.Value });

            await con.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                res.Codigo = Convert.ToInt32(reader["Resultado"]);
                res.Mensaje = reader["Mensaje"]?.ToString() ?? string.Empty;
                res.IdGenerado = idCola;
            }

            return res;
        }

        public async Task<List<BE_AlertaEventoHistorial>> ListarHistorial(DateTime? fechaInicio, DateTime? fechaFin, int idRutina = 0, string? filtroTexto = null)
        {
            var lista = new List<BE_AlertaEventoHistorial>();

            using var con = CrearConexion();
            using var cmd = new SqlCommand("dbo.SP_LISTAR_HISTORIAL_ALERTAS", con)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.Add(new SqlParameter("@FECHA_INICIO", SqlDbType.Date) { Value = (object?)fechaInicio ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@FECHA_FIN", SqlDbType.Date) { Value = (object?)fechaFin ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@ID_RUTINA", SqlDbType.Int) { Value = idRutina });
            cmd.Parameters.Add(new SqlParameter("@FILTRO_TEXTO", SqlDbType.VarChar, 100) { Value = (object?)filtroTexto ?? DBNull.Value });

            await con.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                lista.Add(new BE_AlertaEventoHistorial
                {
                    IdEvento = Convert.ToInt64(reader["ID_EVENTO"]),
                    IdRutina = Convert.ToInt32(reader["ID_RUTINA"]),
                    NombreRutina = reader["NOMBRE_RUTINA"]?.ToString() ?? string.Empty,
                    ClaveDocumento = reader["CLAVE_DOCUMENTO"]?.ToString() ?? string.Empty,
                    LineaDocumento = Convert.ToInt32(reader["LINEA_DOCUMENTO"]),
                    NumeroDocumento = reader["NUMERO_DOCUMENTO"]?.ToString() ?? string.Empty,
                    FechaDocumento = Convert.ToDateTime(reader["FECHA_DOCUMENTO"]),
                    ProveedorCodigo = reader["PROVEEDOR_CODIGO"] != DBNull.Value ? reader["PROVEEDOR_CODIGO"].ToString() : null,
                    ProveedorNombre = reader["PROVEEDOR_NOMBRE"] != DBNull.Value ? reader["PROVEEDOR_NOMBRE"].ToString() : null,
                    ItemCode = reader["ITEM_CODE"]?.ToString() ?? string.Empty,
                    ItemName = reader["ITEM_NAME"] != DBNull.Value ? reader["ITEM_NAME"].ToString() : null,
                    PrecioCompra = Convert.ToDecimal(reader["PRECIO_COMPRA"]),
                    CostoRegistrado = Convert.ToDecimal(reader["COSTO_REGISTRADO"]),
                    DiferenciaMonto = Convert.ToDecimal(reader["DIFERENCIA_MONTO"]),
                    AlmacenCodigo = reader["ALMACEN_CODIGO"] != DBNull.Value ? reader["ALMACEN_CODIGO"].ToString() : null,
                    EnvWms = reader["ENV_WMS"]?.ToString() ?? "Y",
                    FechaDeteccion = Convert.ToDateTime(reader["FECHA_DETECCION"]),
                    TotalNotificadosIntranet = Convert.ToInt32(reader["TOTAL_NOTIFICADOS_INTRANET"]),
                    TotalConfirmados = Convert.ToInt32(reader["TOTAL_CONFIRMADOS"]),
                    TotalCorreosEnviados = Convert.ToInt32(reader["TOTAL_CORREOS_ENVIADOS"])
                });
            }

            return lista;
        }
    }
}

