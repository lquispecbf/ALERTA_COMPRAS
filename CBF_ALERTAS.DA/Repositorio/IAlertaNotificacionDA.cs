using BE;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DA.Repositorio
{
    public interface IAlertaNotificacionDA
    {
        Task<BE_RespuestaBD> RegistrarEventoCompra(BE_CompraMedicamentoHana evento, int idRutina);
        Task<(int TotalNoLeidas, List<BE_AlertaNotificacionIntranet> Alertas)> ObtenerNotificacionesUsuario(string idUsuario, int top = 10);
        Task<BE_RespuestaBD> MarcarLeida(long idNotificacion, string idUsuario);
        Task<BE_RespuestaBD> ConfirmarAlerta(long idNotificacion, string idUsuario, string? comentario = null);
        Task<List<BE_AlertaColaCorreo>> ObtenerCorreosPendientes(int top = 50);
        Task<BE_RespuestaBD> ActualizarEstadoCorreo(long idCola, string estadoEnvio, string? errorMensaje = null);
        Task<List<BE_AlertaEventoHistorial>> ListarHistorial(DateTime? fechaInicio, DateTime? fechaFin, int idRutina = 0, string? filtroTexto = null);
    }
}

