using System.Threading;
using System.Threading.Tasks;

namespace BL.Servicios
{
    public interface IOrquestadorAlertasBL
    {
        Task IniciarAsync(CancellationToken stoppingToken);
    }
}

