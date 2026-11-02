using System;

namespace BE
{
    public class BE_AlertaNotificacionIntranet
    {
        public long IdNotificacion { get; set; }
        public long IdEvento { get; set; }
        public string IdUsuario { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
        public string NivelAlerta { get; set; } = "WARNING";
        public bool Leido { get; set; }
        public DateTime? FechaLectura { get; set; }
        public bool Confirmado { get; set; }
        public DateTime? FechaConfirmacion { get; set; }
        public string? ComentarioConfirmacion { get; set; }
        public DateTime FechaCreacion { get; set; }
        public string? NumeroDocumento { get; set; }
        public string? ItemCode { get; set; }
        public string? ItemName { get; set; }
        public decimal? PrecioCompra { get; set; }
        public decimal? CostoRegistrado { get; set; }
    }
}

