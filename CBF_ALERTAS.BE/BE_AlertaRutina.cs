using System;

namespace BE
{
    public class BE_AlertaRutina
    {
        public int IdRutina { get; set; }
        public string CodigoRutina { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public int IntervaloSegundos { get; set; } = 60;
        public int DiasAtras { get; set; } = 3;
        public bool Habilitado { get; set; } = true;
        public bool CanalIntranet { get; set; } = true;
        public bool CanalCorreo { get; set; } = true;
        public string? ParametrosJson { get; set; }
        public bool Estado { get; set; } = true;
        public string UsuarioCreacion { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; }
        public string? UsuarioModificacion { get; set; }
        public DateTime? FechaModificacion { get; set; }
        public int TotalSuscriptores { get; set; }
    }
}
