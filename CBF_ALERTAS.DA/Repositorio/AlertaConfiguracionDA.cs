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
    public class AlertaConfiguracionDA : IAlertaConfiguracionDA
    {
        private readonly ConfiguracionConexion _conexion;

        public AlertaConfiguracionDA(IOptions<ConfiguracionConexion> conexion)
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

        public async Task<BE_AlertaConfiguracionGlobal> ObtenerVersionConfig()
        {
            var config = new BE_AlertaConfiguracionGlobal { IdConfig = 1, VersionConfig = 1, FechaModificacion = DateTime.Now };

            using var con = CrearConexion();
            using var cmd = new SqlCommand("dbo.SP_OBTENER_VERSION_CONFIG_ALERTAS", con)
            {
                CommandType = CommandType.StoredProcedure
            };

            await con.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                config.IdConfig = Convert.ToInt32(reader["ID_CONFIG"]);
                config.VersionConfig = Convert.ToInt32(reader["VERSION_CONFIG"]);
                config.FechaModificacion = Convert.ToDateTime(reader["FECHA_MODIFICACION"]);
                config.UsuarioModificacion = reader["USUARIO_MODIFICACION"]?.ToString() ?? string.Empty;
            }

            return config;
        }

        public async Task<List<BE_AlertaRutina>> ListarRutinas(bool soloActivas = false)
        {
            var lista = new List<BE_AlertaRutina>();

            using var con = CrearConexion();
            using var cmd = new SqlCommand("dbo.SP_LISTAR_ALERTAS_RUTINAS", con)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.Add(new SqlParameter("@SOLO_ACTIVAS", SqlDbType.Bit) { Value = soloActivas });

            await con.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                lista.Add(new BE_AlertaRutina
                {
                    IdRutina = Convert.ToInt32(reader["ID_RUTINA"]),
                    CodigoRutina = reader["CODIGO_RUTINA"]?.ToString() ?? string.Empty,
                    Nombre = reader["NOMBRE"]?.ToString() ?? string.Empty,
                    Descripcion = reader["DESCRIPCION"] != DBNull.Value ? reader["DESCRIPCION"].ToString() : null,
                    IntervaloSegundos = Convert.ToInt32(reader["INTERVALO_SEGUNDOS"]),
                    DiasAtras = reader["DIAS_ATRAS"] != DBNull.Value ? Convert.ToInt32(reader["DIAS_ATRAS"]) : 3,
                    Habilitado = Convert.ToBoolean(reader["HABILITADO"]),
                    CanalIntranet = Convert.ToBoolean(reader["CANAL_INTRANET"]),
                    CanalCorreo = Convert.ToBoolean(reader["CANAL_CORREO"]),
                    ParametrosJson = reader["PARAMETROS_JSON"] != DBNull.Value ? reader["PARAMETROS_JSON"].ToString() : null,
                    Estado = Convert.ToBoolean(reader["ESTADO"]),
                    UsuarioCreacion = reader["USUARIO_CREACION"]?.ToString() ?? string.Empty,
                    FechaCreacion = Convert.ToDateTime(reader["FECHA_CREACION"]),
                    UsuarioModificacion = reader["USUARIO_MODIFICACION"] != DBNull.Value ? reader["USUARIO_MODIFICACION"].ToString() : null,
                    FechaModificacion = reader["FECHA_MODIFICACION"] != DBNull.Value ? Convert.ToDateTime(reader["FECHA_MODIFICACION"]) : null,
                    TotalSuscriptores = Convert.ToInt32(reader["TOTAL_SUSCRIPTORES"])
                });
            }

            return lista;
        }

        public async Task<BE_AlertaRutina?> ObtenerRutinaPorId(int idRutina)
        {
            using var con = CrearConexion();
            using var cmd = new SqlCommand("dbo.SP_OBTENER_ALERTA_RUTINA_POR_ID", con)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.Add(new SqlParameter("@ID_RUTINA", SqlDbType.Int) { Value = idRutina });

            await con.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new BE_AlertaRutina
                {
                    IdRutina = Convert.ToInt32(reader["ID_RUTINA"]),
                    CodigoRutina = reader["CODIGO_RUTINA"]?.ToString() ?? string.Empty,
                    Nombre = reader["NOMBRE"]?.ToString() ?? string.Empty,
                    Descripcion = reader["DESCRIPCION"] != DBNull.Value ? reader["DESCRIPCION"].ToString() : null,
                    IntervaloSegundos = Convert.ToInt32(reader["INTERVALO_SEGUNDOS"]),
                    DiasAtras = reader["DIAS_ATRAS"] != DBNull.Value ? Convert.ToInt32(reader["DIAS_ATRAS"]) : 3,
                    Habilitado = Convert.ToBoolean(reader["HABILITADO"]),
                    CanalIntranet = Convert.ToBoolean(reader["CANAL_INTRANET"]),
                    CanalCorreo = Convert.ToBoolean(reader["CANAL_CORREO"]),
                    ParametrosJson = reader["PARAMETROS_JSON"] != DBNull.Value ? reader["PARAMETROS_JSON"].ToString() : null,
                    Estado = Convert.ToBoolean(reader["ESTADO"]),
                    UsuarioCreacion = reader["USUARIO_CREACION"]?.ToString() ?? string.Empty,
                    FechaCreacion = Convert.ToDateTime(reader["FECHA_CREACION"])
                };
            }

            return null;
        }

        public async Task<BE_RespuestaBD> InsertarRutina(BE_AlertaRutina obj)
        {
            var res = new BE_RespuestaBD();
            using var con = CrearConexion();
            using var cmd = new SqlCommand("dbo.SP_INSERTAR_ALERTA_RUTINA", con)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add(new SqlParameter("@CODIGO_RUTINA", SqlDbType.VarChar, 50) { Value = obj.CodigoRutina });
            cmd.Parameters.Add(new SqlParameter("@NOMBRE", SqlDbType.VarChar, 150) { Value = obj.Nombre });
            cmd.Parameters.Add(new SqlParameter("@DESCRIPCION", SqlDbType.VarChar, 500) { Value = (object?)obj.Descripcion ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@INTERVALO_SEGUNDOS", SqlDbType.Int) { Value = obj.IntervaloSegundos });
            cmd.Parameters.Add(new SqlParameter("@DIAS_ATRAS", SqlDbType.Int) { Value = obj.DiasAtras <= 0 ? 3 : obj.DiasAtras });
            cmd.Parameters.Add(new SqlParameter("@HABILITADO", SqlDbType.Bit) { Value = obj.Habilitado });
            cmd.Parameters.Add(new SqlParameter("@CANAL_INTRANET", SqlDbType.Bit) { Value = obj.CanalIntranet });
            cmd.Parameters.Add(new SqlParameter("@CANAL_CORREO", SqlDbType.Bit) { Value = obj.CanalCorreo });
            cmd.Parameters.Add(new SqlParameter("@PARAMETROS_JSON", SqlDbType.NVarChar, -1) { Value = (object?)obj.ParametrosJson ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@USUARIO_REGISTRO", SqlDbType.VarChar, 50) { Value = obj.UsuarioCreacion });

            await con.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                res.Codigo = Convert.ToInt32(reader["Resultado"]);
                res.Mensaje = reader["Mensaje"]?.ToString() ?? string.Empty;
                res.IdGenerado = Convert.ToInt64(reader["ID_RUTINA"]);
            }

            return res;
        }

        public async Task<BE_RespuestaBD> ActualizarRutina(BE_AlertaRutina obj)
        {
            var res = new BE_RespuestaBD();
            using var con = CrearConexion();
            using var cmd = new SqlCommand("dbo.SP_ACTUALIZAR_ALERTA_RUTINA", con)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add(new SqlParameter("@ID_RUTINA", SqlDbType.Int) { Value = obj.IdRutina });
            cmd.Parameters.Add(new SqlParameter("@NOMBRE", SqlDbType.VarChar, 150) { Value = obj.Nombre });
            cmd.Parameters.Add(new SqlParameter("@DESCRIPCION", SqlDbType.VarChar, 500) { Value = (object?)obj.Descripcion ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@INTERVALO_SEGUNDOS", SqlDbType.Int) { Value = obj.IntervaloSegundos });
            cmd.Parameters.Add(new SqlParameter("@DIAS_ATRAS", SqlDbType.Int) { Value = obj.DiasAtras <= 0 ? 3 : obj.DiasAtras });
            cmd.Parameters.Add(new SqlParameter("@HABILITADO", SqlDbType.Bit) { Value = obj.Habilitado });
            cmd.Parameters.Add(new SqlParameter("@CANAL_INTRANET", SqlDbType.Bit) { Value = obj.CanalIntranet });
            cmd.Parameters.Add(new SqlParameter("@CANAL_CORREO", SqlDbType.Bit) { Value = obj.CanalCorreo });
            cmd.Parameters.Add(new SqlParameter("@PARAMETROS_JSON", SqlDbType.NVarChar, -1) { Value = (object?)obj.ParametrosJson ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@USUARIO_REGISTRO", SqlDbType.VarChar, 50) { Value = obj.UsuarioModificacion ?? "SISTEMA" });

            await con.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                res.Codigo = Convert.ToInt32(reader["Resultado"]);
                res.Mensaje = reader["Mensaje"]?.ToString() ?? string.Empty;
                res.IdGenerado = Convert.ToInt64(reader["ID_RUTINA"]);
            }

            return res;
        }

        public async Task<BE_RespuestaBD> CambiarEstadoRutina(int idRutina, bool habilitado, string usuario)
        {
            var res = new BE_RespuestaBD();
            using var con = CrearConexion();
            using var cmd = new SqlCommand("dbo.SP_CAMBIAR_ESTADO_ALERTA_RUTINA", con)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add(new SqlParameter("@ID_RUTINA", SqlDbType.Int) { Value = idRutina });
            cmd.Parameters.Add(new SqlParameter("@HABILITADO", SqlDbType.Bit) { Value = habilitado });
            cmd.Parameters.Add(new SqlParameter("@USUARIO_REGISTRO", SqlDbType.VarChar, 50) { Value = usuario });

            await con.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                res.Codigo = Convert.ToInt32(reader["Resultado"]);
                res.Mensaje = reader["Mensaje"]?.ToString() ?? string.Empty;
                res.IdGenerado = Convert.ToInt64(reader["ID_RUTINA"]);
            }

            return res;
        }

        public async Task<List<BE_AlertaSuscriptor>> ListarSuscriptores(int idRutina)
        {
            var lista = new List<BE_AlertaSuscriptor>();

            using var con = CrearConexion();
            using var cmd = new SqlCommand("dbo.SP_LISTAR_ALERTAS_SUSCRIPTORES", con)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.Add(new SqlParameter("@ID_RUTINA", SqlDbType.Int) { Value = idRutina });

            await con.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                lista.Add(new BE_AlertaSuscriptor
                {
                    IdSuscriptor = Convert.ToInt32(reader["ID_SUSCRIPTOR"]),
                    IdRutina = Convert.ToInt32(reader["ID_RUTINA"]),
                    IdUsuario = reader["ID_USUARIO"]?.ToString() ?? string.Empty,
                    NombreCompleto = reader["NOMBRE_COMPLETO"] != DBNull.Value ? reader["NOMBRE_COMPLETO"].ToString() : null,
                    Correo = reader["CORREO"] != DBNull.Value ? reader["CORREO"].ToString() : null,
                    RecibeIntranet = Convert.ToBoolean(reader["RECIBE_INTRANET"]),
                    RecibeCorreo = Convert.ToBoolean(reader["RECIBE_CORREO"]),
                    Estado = Convert.ToBoolean(reader["ESTADO"]),
                    UsuarioCreacion = reader["USUARIO_CREACION"]?.ToString() ?? string.Empty,
                    FechaCreacion = Convert.ToDateTime(reader["FECHA_CREACION"])
                });
            }

            return lista;
        }

        public async Task<BE_RespuestaBD> InsertarSuscriptor(BE_AlertaSuscriptor obj)
        {
            var res = new BE_RespuestaBD();
            using var con = CrearConexion();
            using var cmd = new SqlCommand("dbo.SP_INSERTAR_ALERTA_SUSCRIPTOR", con)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add(new SqlParameter("@ID_RUTINA", SqlDbType.Int) { Value = obj.IdRutina });
            cmd.Parameters.Add(new SqlParameter("@ID_USUARIO", SqlDbType.VarChar, 50) { Value = obj.IdUsuario });
            cmd.Parameters.Add(new SqlParameter("@NOMBRE_COMPLETO", SqlDbType.VarChar, 150) { Value = (object?)obj.NombreCompleto ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@CORREO", SqlDbType.VarChar, 150) { Value = (object?)obj.Correo ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@RECIBE_INTRANET", SqlDbType.Bit) { Value = obj.RecibeIntranet });
            cmd.Parameters.Add(new SqlParameter("@RECIBE_CORREO", SqlDbType.Bit) { Value = obj.RecibeCorreo });
            cmd.Parameters.Add(new SqlParameter("@USUARIO_REGISTRO", SqlDbType.VarChar, 50) { Value = obj.UsuarioCreacion });

            await con.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                res.Codigo = Convert.ToInt32(reader["Resultado"]);
                res.Mensaje = reader["Mensaje"]?.ToString() ?? string.Empty;
                res.IdGenerado = Convert.ToInt64(reader["ID_SUSCRIPTOR"]);
            }

            return res;
        }

        public async Task<BE_RespuestaBD> EliminarSuscriptor(int idSuscriptor, string usuario)
        {
            var res = new BE_RespuestaBD();
            using var con = CrearConexion();
            using var cmd = new SqlCommand("dbo.SP_ELIMINAR_ALERTA_SUSCRIPTOR", con)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add(new SqlParameter("@ID_SUSCRIPTOR", SqlDbType.Int) { Value = idSuscriptor });
            cmd.Parameters.Add(new SqlParameter("@USUARIO_REGISTRO", SqlDbType.VarChar, 50) { Value = usuario });

            await con.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                res.Codigo = Convert.ToInt32(reader["Resultado"]);
                res.Mensaje = reader["Mensaje"]?.ToString() ?? string.Empty;
                res.IdGenerado = Convert.ToInt64(reader["ID_SUSCRIPTOR"]);
            }

            return res;
        }
    }
}
