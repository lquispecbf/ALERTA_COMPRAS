using System;

namespace DA.Configuracion
{
    public class ConfiguracionConexion
    {
        public string CadenaSQL { get; set; } = string.Empty;
        public string CadenaSAP_ODBC { get; set; } = string.Empty;
        public string CompanyDB { get; set; } = "SBO_CBF";

        // Parámetros SMTP para notificaciones por correo
        public string SmtpHost { get; set; } = "mail.cbf.com.pe";
        public int SmtpPort { get; set; } = 587;
        public bool SmtpEnableSsl { get; set; } = true;
        public string SmtpUser { get; set; } = "info@cbf.com.pe";
        public string SmtpPassword { get; set; } = string.Empty;
        public string RemitenteCorreo { get; set; } = "info@cbf.com.pe";
        public string RemitenteNombre { get; set; } = "Alertas CBF";

        // Ruta local de logs para Serilog
        public string LogDirectory { get; set; } = @"D:\LogsAlertasCBF\";
    }
}

