using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Negocio.Web.Data;
using Negocio.Web.Models.Entities;
using Negocio.Web.Models.ViewModels;
using System.Globalization;
using Negocio.Web.Services;

namespace Negocio.Web.Controllers
{
    public class VentaController : Controller
    {
        private readonly NegocioDbContext _context;
        private readonly RielCortinaService _rielService;
        private readonly TornilloService _tornilloService;

        public VentaController(NegocioDbContext context, RielCortinaService rielService, TornilloService tornilloService)
        {
            _context = context;
            _rielService = rielService;
            _tornilloService = tornilloService;
        }

        // =========================
        // LISTADO
        // =========================
        public IActionResult Index(
    DateTime? desde,
    DateTime? hasta,
    string area,
    string destino)
        {
            // =========================
            // FECHA ACTUAL (PERÚ - AZURE SAFE)
            // =========================
            var zonaPeru = TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
            var hoy = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zonaPeru).Date;

            if (!desde.HasValue)
                desde = hoy;

            if (!hasta.HasValue)
                hasta = hoy;

            // =========================
            // QUERY BASE
            // =========================
            var query = _context.Ventas.AsQueryable();

            // =========================
            // FILTROS (SIN PROBLEMAS DE HORA)
            // =========================
            query = query.Where(v =>
                v.FechaRegistro.Date >= desde.Value.Date &&
                v.FechaRegistro.Date <= hasta.Value.Date
            );

            if (!string.IsNullOrEmpty(area) && area != "TODAS")
                query = query.Where(v => v.Area == area);

            if (!string.IsNullOrEmpty(destino) && destino != "TODOS")
                query = query.Where(v => v.Destino == destino);

            // =========================
            // LISTA DE VENTAS
            // =========================
            var ventas = query
                .OrderBy(v => v.FechaRegistro)
                .Select(v => new VentaProductoViewModel
                {
                    IdVenta = v.IdVenta,
                    Fecha = v.FechaRegistro,
                    Area = v.Area,
                    Producto = v.Descripcion,
                    Dimensiones = v.Dimensiones,
                    Cantidad = v.Cantidad,
                    Unidad = v.Unidad,
                    Precio = v.Precio,
                    Destino = v.Destino
                })
                .ToList();

            // =========================
            // AREAS (FUERA DE EF)
            // =========================
            var areasDb = _context.Productos
                .Select(p => p.Area)
                .Distinct()
                .OrderBy(a => a)
                .ToList();

            var areasSelect = new List<SelectListItem>
    {
        new SelectListItem("TODAS", "TODAS")
    };

            areasSelect.AddRange(
                areasDb.Select(a => new SelectListItem(a, a))
            );

            // =========================
            // VIEWMODEL
            // =========================
            var vm = new VentaIndexViewModel
            {
                Desde = desde,
                Hasta = hasta,
                Area = area ?? "TODAS",
                Destino = destino ?? "TODOS",

                Ventas = ventas,
                TotalGeneral = ventas.Sum(v => v.Precio),

                Areas = areasSelect,

                Destinos = new List<SelectListItem>
        {
            new SelectListItem("TODOS","TODOS"),
            new SelectListItem("EFECTIVO","EFECTIVO"),
            new SelectListItem("YAPE","YAPE")
        }
            };

            return View(vm);


          
        }


        private void CargarCombosVenta(RegistrarVentaViewModel vm)
        {
            vm.Areas = _context.Productos
                .Select(p => p.Area)
                .Distinct()
                .OrderBy(a => a)
                .Select(a => new SelectListItem(a, a))
                .ToList();

            vm.Unidades = new List<SelectListItem>
    {
        new("METROS","METROS"),
        new("VARILLAS","VARILLAS"),
        new("UNIDAD","UNIDAD"),
        new("PAR","PAR")
    };

            vm.Destinos = new List<SelectListItem>
    {
        new("EFECTIVO","EFECTIVO"),
        new("YAPE","YAPE")
    };
        }


        // =========================
        // FORMULARIO
        // =========================
        public IActionResult Registrar()
        {
            var zonaPeru = TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
            var ahoraPeru = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zonaPeru);

            var vm = new RegistrarVentaViewModel
            {
                Dia = ahoraPeru
                    .ToString("dddd", new CultureInfo("es-PE"))
                    .ToUpper(),

                Fecha = ahoraPeru.Date,

                Areas = _context.Productos
                    .Select(p => p.Area)
                    .Distinct()
                    .OrderBy(a => a)
                    .Select(a => new SelectListItem(a, a))
                    .ToList(),

                Unidades = new()
        {
            new("METROS","METROS"),
            new("VARILLAS","VARILLAS"),
            new("UNIDAD","UNIDAD"),
            new("PAR","PAR")
        },

                Destinos = new()
        {
            new("EFECTIVO","EFECTIVO"),
            new("YAPE","YAPE")
        }
            };

            return View(vm);
        }

        // =========================
        // GUARDAR
        // =========================
        [HttpPost]
        public IActionResult Registrar(RegistrarVentaViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                CargarCombosVenta(vm);
                return View(vm);
            }

            Producto? producto = null;

            if (vm.ActualizarStock && vm.IdProducto.HasValue)
            {
                producto = _context.Productos
                    .FirstOrDefault(p => p.IdProducto == vm.IdProducto.Value);

                if (producto == null)
                {
                    ModelState.AddModelError("", "Producto no encontrado");
                    CargarCombosVenta(vm);
                    return View(vm);
                }

                decimal cantidadEnVarillas;

                // 👇 LA CLAVE: usar vm.Unidad, NO producto.Unidad
                if (vm.Unidad.Trim().ToUpper() == "METROS")
                {
                    cantidadEnVarillas = vm.Cantidad / producto.ConversionFactor;
                }
                else
                {
                    // VARILLAS / UNIDAD
                    cantidadEnVarillas = vm.Cantidad;
                }

                if (producto.StockActual < cantidadEnVarillas)
                {
                    ModelState.AddModelError("", "Stock insuficiente");
                    CargarCombosVenta(vm);
                    return View(vm);
                }

                // ✅ Descuento correcto
                producto.StockActual -= cantidadEnVarillas;
            }


            var venta = new Venta
            {
                FechaRegistro = vm.Fecha,
                Dia = vm.Fecha.ToString("dddd"),

                Area = producto?.Area ?? vm.Area,
                Descripcion = producto?.Descripcion ?? "",
                Dimensiones = producto?.Dimensiones,
                Cantidad = vm.Cantidad,
                Unidad = vm.Unidad,
                Precio = vm.Precio,
                Destino = vm.Destino,
                IdProducto = producto?.IdProducto
            };

            _context.Ventas.Add(venta);
            _context.SaveChanges();

            TempData["Success"] = "Venta registrada con éxito";
            return RedirectToAction(nameof(Registrar));
        }

        // =========================
        // AJAX
        // =========================
        [HttpGet]
        public IActionResult ObtenerProductosPorArea(string area)
        {
            var productos = _context.Productos
                .Where(p => p.Area == area)
                .GroupBy(p => p.Descripcion)
                .Select(g => new
                {
                    Descripcion = g.Key
                })
                .OrderBy(p => p.Descripcion)
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


        public IActionResult Editar(int id)
        {
            var venta = _context.Ventas.FirstOrDefault(v => v.IdVenta == id);
            if (venta == null) return NotFound();

            // 🔹 SI ES VENTA LIBRE
            if (!venta.IdProducto.HasValue)
            {
                var vmLibre = new RegistrarVentaViewModel
                {
                    IdVenta = venta.IdVenta,
                    Area = venta.Area,
                    Descripcion = venta.Descripcion,
                    Dimensiones = venta.Dimensiones,
                    Cantidad = venta.Cantidad,
                    Unidad = venta.Unidad,
                    Precio = venta.Precio,
                    Destino = venta.Destino,
                    Fecha = venta.FechaRegistro,
                    Dia = venta.FechaRegistro
                        .ToString("dddd", new CultureInfo("es-PE"))
                        .ToUpper()
                };

                return View("VentaLibre", vmLibre);
            }

            // 🔹 SI ES VENTA NORMAL
            var vm = new RegistrarVentaViewModel
            {
                IdVenta = venta.IdVenta,
                Area = venta.Area,
                Cantidad = venta.Cantidad,
                Unidad = venta.Unidad,
                Precio = venta.Precio,
                Destino = venta.Destino,
                Fecha = venta.FechaRegistro,
                Dia = venta.FechaRegistro
                    .ToString("dddd", new CultureInfo("es-PE"))
                    .ToUpper(),
                IdProducto = venta.IdProducto,

                Areas = _context.Productos
                    .Select(p => p.Area)
                    .Distinct()
                    .OrderBy(a => a)
                    .Select(a => new SelectListItem(a, a))
                    .ToList(),

                Unidades = new()
        {
            new("METROS","METROS"),
            new("VARILLAS","VARILLAS"),
            new("UNIDAD","UNIDAD"),
            new("PAR","PAR")
        },

                Destinos = new()
        {
            new("EFECTIVO","EFECTIVO"),
            new("YAPE","YAPE")
        }
            };

            return View("Registrar", vm);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Editar(RegistrarVentaViewModel vm)
        {
            if (!vm.IdVenta.HasValue)
                return BadRequest();

            var venta = _context.Ventas
                .FirstOrDefault(v => v.IdVenta == vm.IdVenta.Value);

            if (venta == null)
                return NotFound();

            // =========================
            // 🔹 VENTA LIBRE
            // =========================
            if (!venta.IdProducto.HasValue)
            {
                venta.Area = vm.Area;
                venta.Descripcion = vm.Descripcion ?? "";
                venta.Dimensiones = vm.Dimensiones;
                venta.Cantidad = vm.Cantidad;
                venta.Unidad = vm.Unidad;
                venta.Precio = vm.Precio;
                venta.Destino = vm.Destino;
                venta.FechaRegistro = vm.Fecha;
                venta.Dia = vm.Fecha
                    .ToString("dddd", new CultureInfo("es-PE"))
                    .ToUpper();

                _context.SaveChanges();

                TempData["Success"] = "Venta libre editada correctamente";
                return RedirectToAction("Index");
            }

            // =========================
            // 🔹 VENTA CON INVENTARIO
            // =========================
            var productoViejo = _context.Productos
                .FirstOrDefault(p => p.IdProducto == venta.IdProducto);

            if (productoViejo == null)
                return NotFound();

            using var tx = _context.Database.BeginTransaction();

            try
            {
                // DEVOLVER STOCK VIEJO
                decimal devolverViejo;

                if (venta.Unidad.Trim().ToUpper() == "METROS")
                    devolverViejo = venta.Cantidad / productoViejo.ConversionFactor;
                else
                    devolverViejo = venta.Cantidad;

                productoViejo.StockActual += devolverViejo;

                var productoNuevo = _context.Productos
                    .FirstOrDefault(p => p.IdProducto == vm.IdProducto.Value);

                if (productoNuevo == null)
                    throw new Exception("Producto no encontrado");

                decimal descontarNuevo;

                if (vm.Unidad.Trim().ToUpper() == "METROS")
                    descontarNuevo = vm.Cantidad / productoNuevo.ConversionFactor;
                else
                    descontarNuevo = vm.Cantidad;

                if (productoNuevo.StockActual < descontarNuevo)
                {
                    tx.Rollback();
                    TempData["Error"] = "Stock insuficiente";
                    return RedirectToAction("Index");
                }

                productoNuevo.StockActual -= descontarNuevo;

                venta.IdProducto = productoNuevo.IdProducto;
                venta.Area = productoNuevo.Area;
                venta.Descripcion = productoNuevo.Descripcion;
                venta.Dimensiones = productoNuevo.Dimensiones;

                venta.Cantidad = vm.Cantidad;
                venta.Unidad = vm.Unidad;
                venta.Precio = vm.Precio;
                venta.Destino = vm.Destino;
                venta.FechaRegistro = vm.Fecha;
                venta.Dia = vm.Fecha
                    .ToString("dddd", new CultureInfo("es-PE"))
                    .ToUpper();

                _context.SaveChanges();
                tx.Commit();

                TempData["Success"] = "Venta editada correctamente";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                tx.Rollback();
                TempData["Error"] = ex.Message;
                return RedirectToAction("Index");
            }
        }



        [HttpPost]
        public IActionResult Eliminar(int id)
        {
            var venta = _context.Ventas
                .FirstOrDefault(v => v.IdVenta == id);

            if (venta == null)
                return NotFound();

            // 👉 DEVOLVER STOCK
            if (venta.IdProducto.HasValue)
            {
                var producto = _context.Productos
                    .FirstOrDefault(p => p.IdProducto == venta.IdProducto.Value);

                if (producto != null)
                {
                    decimal cantidadEnVarillas;

                    if (venta.Unidad.ToUpper() == "METROS")
                        cantidadEnVarillas = venta.Cantidad / producto.ConversionFactor;
                    else
                        cantidadEnVarillas = venta.Cantidad;

                    producto.StockActual += cantidadEnVarillas;
                }
            }

            _context.Ventas.Remove(venta);
            _context.SaveChanges();

            TempData["Success"] = "Venta eliminada y stock devuelto correctamente";

            return RedirectToAction("Index");
        }


        public IActionResult VentaLibre()
        {
            var zonaPeru = TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
            var ahoraPeru = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zonaPeru);

            var vm = new RegistrarVentaViewModel
            {
                Dia = ahoraPeru
                    .ToString("dddd", new CultureInfo("es-PE"))
                    .ToUpper(),

                Fecha = ahoraPeru.Date,

                Areas = new List<SelectListItem>
        {
            new("ALUMINIO","ALUMINIO"),
            new("ACCESORIO","ACCESORIO"),
            new("ARC","ARC")
        }
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult VentaLibre(RegistrarVentaViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.Areas = new List<SelectListItem>
        {
            new("ALUMINIO","ALUMINIO"),
            new("ACCESORIO","ACCESORIO"),
            new("ARC","ARC")
        };

                return View(vm);
            }

            var venta = new Venta
            {
                FechaRegistro = vm.Fecha,

                Dia = vm.Fecha
                    .ToString("dddd", new CultureInfo("es-PE"))
                    .ToUpper(),

                Area = vm.Area,
                Descripcion = vm.Descripcion ?? "",
                Dimensiones = vm.Dimensiones,
                Cantidad = vm.Cantidad,
                Unidad = vm.Unidad,
                Precio = vm.Precio,
                Destino = vm.Destino,

                IdProducto = null
            };

            _context.Ventas.Add(venta);
            _context.SaveChanges();
            TempData["Success"] = "Venta libre registrada con éxito";
            return RedirectToAction(nameof(VentaLibre));
        }


        public IActionResult SeleccionTipoVenta()
        {
            return View();
        }

        // =========================================
        // RIEL CORTINA - GET (Inicializa Fechas)
        // =========================================
        public IActionResult RielCortina()
        {
            // ⚡ Capturamos la hora oficial de Perú exacta y segura para Azure
            var zonaPeru = TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
            var ahoraPeru = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zonaPeru);

            var vm = new RielCortinaViewModel
            {
                Fecha = ahoraPeru.Date,
                Dia = ahoraPeru.ToString("dddd", new CultureInfo("es-PE")).ToUpper()
            };

            return View(vm);
        }

        // =========================================
        // RIEL CORTINA - POST (Envía Fechas al Service)
        // =========================================
        [HttpPost]
        public async Task<IActionResult> RielCortina(RielCortinaViewModel vm)
        {
            if (!vm.AccesorioAparte)
            {
                ModelState.Remove(nameof(vm.TipoUnera));
                ModelState.Remove(nameof(vm.CantidadUneraExtra));
            }

            if (!ModelState.IsValid)
                return View(vm);

            try
            {
                // 🔥 Agregamos vm.Fecha y vm.Dia al final de los parámetros del Service
                var resultado = await _rielService.RegistrarVentaRielAsync(
                    vm.Metros,
                    vm.TipoCruce,
                    vm.AccesorioAparte,
                    vm.TipoUnera,
                    vm.CantidadUneraExtra ?? 0,
                    vm.Precio,
                    vm.Destino,
                    vm.Fecha, // ⚡ NUEVO
                    vm.Dia    // ⚡ NUEVO
                );

                return View("ResultadoRiel", resultado);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(vm);
            }
        }


        [HttpGet]
        public IActionResult Tornillos()
        {
            // ⚡ Hora oficial de Perú segura para la nube
            var zonaPeru = TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
            var ahoraPeru = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zonaPeru);

            var vm = new VentaTornilloViewModel
            {
                Fecha = ahoraPeru.Date,
                Dia = ahoraPeru.ToString("dddd", new CultureInfo("es-PE")).ToUpper(),

                Tornillos = _context.Productos
                    .Where(p => p.Descripcion == "TORNILLO" && p.Activo)
                    .OrderBy(p => p.Dimensiones)
                    .ToList(),

                Tarubos = _context.Productos
                    .Where(p => p.Descripcion == "TARUBO" && p.Activo)
                    .OrderBy(p => p.Dimensiones)
                    .ToList()
            };

            return View(vm);
        }


        [HttpPost]
        public async Task<IActionResult> Tornillos(VentaTornilloViewModel model)
        {
            model.Tornillos = _context.Productos
                .Where(p => p.Descripcion == "TORNILLO" && p.Activo)
                .OrderBy(p => p.Dimensiones)
                .ToList();

            model.Tarubos = _context.Productos
                .Where(p => p.Descripcion == "TARUBO" && p.Activo)
                .OrderBy(p => p.Dimensiones)
                .ToList();

            var resultado = await _tornilloService.RegistrarVentaAsync(model);

            if (resultado != "OK")
            {
                ModelState.AddModelError("", resultado);
                return View(model);
            }

            TempData["ok"] = "Venta registrada correctamente";

            return RedirectToAction("Index");
        }
    }
}
