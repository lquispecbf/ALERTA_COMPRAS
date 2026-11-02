namespace BE
{
    public class BE_RespuestaBD
    {
        public int Codigo { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public long IdGenerado { get; set; }
        public int FilasAfectadas { get; set; }
    }
}

