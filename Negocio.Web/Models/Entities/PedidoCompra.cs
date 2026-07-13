using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Negocio.Web.Models.Entities
{
    public class PedidoCompra
    {
        [Key] // 🔥 CLAVE
        public int IdPedidoCompra { get; set; }

        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

        // PENDIENTE | CUMPLIDO
        public string Estado { get; set; } = "PENDIENTE";

        public string Usuario { get; set; } = "";

        public List<PedidoCompraDetalle> Detalles { get; set; } = new();
    }
}
