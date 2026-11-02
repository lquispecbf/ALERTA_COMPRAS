using System;

namespace BE
{
    public class BE_AlertaColaCorreo
    {
        public long IdCola { get; set; }
        public long IdEvento { get; set; }
        public string DestinatarioCorreo { get; set; } = string.Empty;
        public string? DestinatarioNombre { get; set; }
        public string Asunto { get; set; } = string.Empty;
        public string CuerpoHtml { get; set; } = string.Empty;
        public string EstadoEnvio { get; set; } = "PENDIENTE";
        public DateTime? FechaEnvio { get; set; }
        public string? ErrorMensaje { get; set; }
        public int Intentos { get; set; }
        public DateTime FechaCreacion { get; set; }
    }
}

