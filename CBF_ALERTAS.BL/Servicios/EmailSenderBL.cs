using DA.Configuracion;
using DA.Repositorio;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace BL.Servicios
{
    public class EmailSenderBL : IEmailSenderBL
    {
        private readonly IAlertaNotificacionDA _notificacionDa;
        private readonly ConfiguracionConexion _config;
        private readonly ILogger<EmailSenderBL> _logger;

        public EmailSenderBL(
            IAlertaNotificacionDA notificacionDa, 
            IOptions<ConfiguracionConexion> config, 
            ILogger<EmailSenderBL> logger)
        {
            _notificacionDa = notificacionDa;
            _config = config.Value;
            _logger = logger;
        }

        public async Task ProcesarColaCorreosAsync()
        {
            try
            {
                var pendientes = await _notificacionDa.ObtenerCorreosPendientes(top: 25);
                if (pendientes == null || pendientes.Count == 0)
                    return;

                foreach (var correo in pendientes)
                {
                    bool enviado = await EnviarCorreoAsync(correo.DestinatarioCorreo, correo.Asunto, correo.CuerpoHtml);
                    if (enviado)
                    {
                        await _notificacionDa.ActualizarEstadoCorreo(correo.IdCola, "ENVIADO");
                        _logger.LogInformation("Correo enviado exitosamente a {Destinatario} (IdCola: {IdCola})", correo.DestinatarioCorreo, correo.IdCola);
                    }
                    else
                    {
                        await _notificacionDa.ActualizarEstadoCorreo(correo.IdCola, "ERROR", "Fallo en el envío SMTP");
                        _logger.LogWarning("Error al enviar correo a {Destinatario} (IdCola: {IdCola})", correo.DestinatarioCorreo, correo.IdCola);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante el procesamiento de la cola de correos.");
            }
        }

        public async Task<bool> EnviarCorreoAsync(string destinatario, string asunto, string cuerpoHtml)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(destinatario))
                    return false;

                using var message = new MailMessage
                {
                    From = new MailAddress(_config.RemitenteCorreo, _config.RemitenteNombre),
                    Subject = asunto,
                    Body = cuerpoHtml,
                    IsBodyHtml = true
                };

                message.To.Add(destinatario.Trim());

                using var smtp = new SmtpClient(_config.SmtpHost, _config.SmtpPort)
                {
                    EnableSsl = _config.SmtpEnableSsl,
                    UseDefaultCredentials = false,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Timeout = 20000
                };

                if (!string.IsNullOrWhiteSpace(_config.SmtpUser) && !string.IsNullOrWhiteSpace(_config.SmtpPassword))
                {
                    smtp.Credentials = new NetworkCredential(_config.SmtpUser.Trim(), _config.SmtpPassword.Trim());
                }

                await smtp.SendMailAsync(message);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excepción enviando correo a {Destinatario}: {Mensaje}", destinatario, ex.Message);
                return false;
            }
        }
    }
}

