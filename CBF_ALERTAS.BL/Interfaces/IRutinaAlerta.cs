using BE;
using System.Threading;
using System.Threading.Tasks;

namespace BL.Interfaces
{
    public interface IRutinaAlerta
    {
        string CodigoRutina { get; }
        Task EjecutarCicloAsync(BE_AlertaRutina configuracion, CancellationToken stoppingToken);
    }
}

