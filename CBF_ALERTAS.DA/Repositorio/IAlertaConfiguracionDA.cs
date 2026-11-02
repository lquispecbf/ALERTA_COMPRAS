using BE;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DA.Repositorio
{
    public interface IAlertaConfiguracionDA
    {
        Task<BE_AlertaConfiguracionGlobal> ObtenerVersionConfig();
        Task<List<BE_AlertaRutina>> ListarRutinas(bool soloActivas = false);
        Task<BE_AlertaRutina?> ObtenerRutinaPorId(int idRutina);
        Task<BE_RespuestaBD> InsertarRutina(BE_AlertaRutina obj);
        Task<BE_RespuestaBD> ActualizarRutina(BE_AlertaRutina obj);
        Task<BE_RespuestaBD> CambiarEstadoRutina(int idRutina, bool habilitado, string usuario);
        Task<List<BE_AlertaSuscriptor>> ListarSuscriptores(int idRutina);
        Task<BE_RespuestaBD> InsertarSuscriptor(BE_AlertaSuscriptor obj);
        Task<BE_RespuestaBD> EliminarSuscriptor(int idSuscriptor, string usuario);
    }
}

