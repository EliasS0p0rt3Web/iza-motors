using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Negocio.Web.Data;
using Negocio.Web.Models.Entities;
using Negocio.Web.Models.ViewModels;

namespace Negocio.Web.Controllers
{
    public class IngresoController : Controller
    {
        private readonly NegocioDbContext _context;

        public IngresoController(NegocioDbContext context)
        {
            _context = context;
        }

        // LISTADO
        public IActionResult Index(DateTime? desde, DateTime? hasta)
        {
            var hoy = DateTime.Today;

            // 🔥 Si no vienen fechas, usar mes actual
            if (!desde.HasValue)
                desde = new DateTime(hoy.Year, hoy.Month, 1);

            if (!hasta.HasValue)
                hasta = new DateTime(hoy.Year, hoy.Month,
                    DateTime.DaysInMonth(hoy.Year, hoy.Month));

            var ingresos = _context.Ingresos
                .Include(i => i.Producto)
                .Where(i =>
                    i.FechaIngreso.Date >= desde.Value.Date &&
                    i.FechaIngreso.Date <= hasta.Value.Date
                )
                .OrderByDescending(i => i.FechaIngreso)
                .ToList();

            ViewBag.Desde = desde.Value.ToString("yyyy-MM-dd");
            ViewBag.Hasta = hasta.Value.ToString("yyyy-MM-dd");

            return View(ingresos);
        }

        // FORM
        public IActionResult Registrar()
        {
            var vm = new RegistrarIngresoViewModel
            {
                Ingreso = new Ingreso
                {
                    FechaIngreso = DateTime.Today
                },
                Areas = _context.Productos
                    .Select(p => p.Area)
                    .Distinct()
                    .OrderBy(a => a)
                    .ToList()
            };

            return View(vm);
        }

        [HttpPost]
        public IActionResult Registrar(RegistrarIngresoViewModel vm)
        {
            // VALIDAR PRODUCTO
            if (vm.Ingreso.IdProducto == 0)
            {
                ModelState.AddModelError("Ingreso.IdProducto", "Debe seleccionar un producto");
            }

            if (!ModelState.IsValid)
            {
                // 🔥 RECARGAR COMBOS
                vm.Areas = _context.Productos
                    .Select(p => p.Area)
                    .Distinct()
                    .OrderBy(a => a)
                    .ToList();

                vm.Productos = _context.Productos.ToList();
                return View(vm);
            }

            var producto = _context.Productos
                .FirstOrDefault(p => p.IdProducto == vm.Ingreso.IdProducto);

            if (producto == null)
            {
                ModelState.AddModelError("", "Producto no encontrado");

                vm.Areas = _context.Productos
                    .Select(p => p.Area)
                    .Distinct()
                    .OrderBy(a => a)
                    .ToList();

                vm.Productos = _context.Productos.ToList();
                return View(vm);
            }

            // =========================
            // 🔥 GUARDAR DIMENSIONES
            // =========================
            vm.Ingreso.Dimensiones = vm.Dimensiones;

            // =========================
            // 🔥 LIMPIAR PRECIOS
            // =========================
            if (vm.Ingreso.PrecioUnitario.HasValue)
            {
                vm.Ingreso.PrecioPorMetro = null;
                vm.Ingreso.PrecioPorVarilla = null;
            }
            else
            {
                vm.Ingreso.PrecioUnitario = null;
            }

            // =========================
            // 🔥 ACTUALIZAR STOCK
            // =========================
            if (producto.Unidad == "METRO")
            {
                var varillas = vm.Ingreso.Cantidad / producto.ConversionFactor;
                producto.StockActual += varillas;
            }
            else
            {
                producto.StockActual += vm.Ingreso.Cantidad;
            }

         

            _context.Ingresos.Add(vm.Ingreso);
            _context.SaveChanges();

            return RedirectToAction(nameof(Index));
        }



        [HttpGet]
        public IActionResult ObtenerProductosPorArea(string area)
        {
            var productos = _context.Productos
                .Where(p => p.Area == area)
                .Select(p => p.Descripcion)
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            return Json(productos);
        }


        [HttpGet]
        public IActionResult ObtenerDimensiones(string area, string descripcion)
        {
            var dimensiones = _context.Productos
                .Where(p => p.Area == area && p.Descripcion == descripcion)
                .Select(p => p.Dimensiones)
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            return Json(dimensiones);
        }

        [HttpGet]
        public IActionResult ObtenerProductoFinal(string area, string descripcion, string dimensiones)
        {
            var producto = _context.Productos
                .FirstOrDefault(p =>
                    p.Area == area &&
                    p.Descripcion == descripcion &&
                    p.Dimensiones == dimensiones);

            if (producto == null)
                return NotFound();

            return Json(new
            {
                producto.IdProducto,
                producto.Unidad
            });
        }


        [HttpGet]
        public IActionResult ObtenerDimensionesPorProducto(int idProducto)
        {
            var dimensiones = _context.Productos
                .Where(p => p.IdProducto == idProducto)
                .Select(p => p.Dimensiones)
                .Distinct()
                .ToList();

            return Json(dimensiones);
        }

        public IActionResult Editar(int id)
        {
            var ingreso = _context.Ingresos
                .Include(i => i.Producto)
                .FirstOrDefault(i => i.IdIngreso == id);

            if (ingreso == null)
                return NotFound();

            var vm = new RegistrarIngresoViewModel
            {
                Ingreso = ingreso,
                Dimensiones = ingreso.Dimensiones,
                Areas = _context.Productos
                    .Select(p => p.Area)
                    .Distinct()
                    .OrderBy(a => a)
                    .ToList()
            };

            return View("Registrar", vm);
        }

        [HttpPost]
        public IActionResult Editar(RegistrarIngresoViewModel vm)
        {
            var ingreso = _context.Ingresos
                .Include(i => i.Producto)
                .FirstOrDefault(i => i.IdIngreso == vm.Ingreso.IdIngreso);

            if (ingreso == null)
                return NotFound();

            var producto = ingreso.Producto;

            using var tx = _context.Database.BeginTransaction();

            try
            {
                // =========================
                // 1️⃣ QUITAR STOCK ANTERIOR
                // =========================
                decimal quitarAnterior;

                if (producto.Unidad == "METRO")
                    quitarAnterior = ingreso.Cantidad / producto.ConversionFactor;
                else
                    quitarAnterior = ingreso.Cantidad;

                producto.StockActual -= quitarAnterior;

                // =========================
                // 2️⃣ ACTUALIZAR INGRESO
                // =========================
                ingreso.Cantidad = vm.Ingreso.Cantidad;
                ingreso.PrecioUnitario = vm.Ingreso.PrecioUnitario;
                ingreso.PrecioPorMetro = vm.Ingreso.PrecioPorMetro;
                ingreso.PrecioPorVarilla = vm.Ingreso.PrecioPorVarilla;

                // =========================
                // 3️⃣ SUMAR STOCK NUEVO
                // =========================
                decimal sumarNuevo;

                if (producto.Unidad == "METRO")
                    sumarNuevo = ingreso.Cantidad / producto.ConversionFactor;
                else
                    sumarNuevo = ingreso.Cantidad;

                producto.StockActual += sumarNuevo;

                _context.SaveChanges();
                tx.Commit();

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Eliminar(int id)
        {
            var ingreso = _context.Ingresos
                .Include(i => i.Producto)
                .FirstOrDefault(i => i.IdIngreso == id);

            if (ingreso == null)
                return NotFound();

            var producto = ingreso.Producto;

            using var tx = _context.Database.BeginTransaction();

            try
            {
                decimal descontar;

                if (producto.Unidad == "METRO")
                    descontar = ingreso.Cantidad / producto.ConversionFactor;
                else
                    descontar = ingreso.Cantidad;

                producto.StockActual -= descontar;

                _context.Ingresos.Remove(ingreso);
                _context.SaveChanges();

                tx.Commit();

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

    }
}
