using System;

namespace BE
{
    public class BE_AlertaSuscriptor
    {
        public int IdSuscriptor { get; set; }
        public int IdRutina { get; set; }
        public string IdUsuario { get; set; } = string.Empty;
        public string? NombreCompleto { get; set; }
        public string? Correo { get; set; }
        public bool RecibeIntranet { get; set; } = true;
        public bool RecibeCorreo { get; set; } = true;
        public bool Estado { get; set; } = true;
        public string UsuarioCreacion { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; }
    }
}

