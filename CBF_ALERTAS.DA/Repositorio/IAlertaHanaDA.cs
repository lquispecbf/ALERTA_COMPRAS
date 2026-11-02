using BE;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DA.Repositorio
{
    public interface IAlertaHanaDA
    {
        Task<List<BE_CompraMedicamentoHana>> ConsultarComprasMedicamentoVsCosto(DateTime? fechaEvaluacion = null, int docEntry = 0);
    }
}

