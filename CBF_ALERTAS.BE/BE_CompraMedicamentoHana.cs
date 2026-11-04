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
        public decimal PrecioVentaGeneral { get; set; }
        public string MotivoAlerta { get; set; } = "COMPRA_GE_COSTO";
        public string? AlmacenCodigo { get; set; }
        public string EnvWms { get; set; } = "Y";
    }
}

