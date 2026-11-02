using System.Threading.Tasks;

namespace BL.Servicios
{
    public interface IEmailSenderBL
    {
        Task ProcesarColaCorreosAsync();
        Task<bool> EnviarCorreoAsync(string destinatario, string asunto, string cuerpoHtml);
    }
}

