using Negocio.Web.Models.Entities;
using System.Collections.Generic;

namespace Negocio.Web.Models.ViewModels
{
    public class RegistrarIngresoViewModel
    {
        public Ingreso Ingreso { get; set; } = new();

        public string? Area { get; set; }
        public string? Dimensiones { get; set; }

        public List<string> Areas { get; set; } = new();
        public List<Producto> Productos { get; set; } = new();
        public List<string> DimensionesList { get; set; } = new();
    }
}
