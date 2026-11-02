using System;

namespace BE
{
    public class BE_AlertaConfiguracionGlobal
    {
        public int IdConfig { get; set; }
        public int VersionConfig { get; set; }
        public DateTime FechaModificacion { get; set; }
        public string UsuarioModificacion { get; set; } = string.Empty;
    }
}

