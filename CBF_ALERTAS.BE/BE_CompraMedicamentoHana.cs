using System;

namespace BE
{
    public class BE_CompraMedicamentoHana
    {
        public string ClaveDocumento { get; set; } = string.Empty;
        public int LineaDocumento { get; set; }
        public string NumeroDocumento { get; set; } = string.Empty;
        public DateTime FechaDocumento { get; set; }
        public string? ProveedorCodigo { get; set; }
        public string? ProveedorNombre { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string? ItemName { get; set; }
        public decimal PrecioCompra { get; set; }
        public decimal CostoRegistrado { get; set; }
        public decimal DiferenciaMonto { get; set; }
        public string? AlmacenCodigo { get; set; }
        public string EnvWms { get; set; } = "Y";
    }
}

