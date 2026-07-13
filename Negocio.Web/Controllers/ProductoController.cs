using Microsoft.AspNetCore.Mvc;
using Negocio.Web.Data;
using Negocio.Web.Models.Entities;
using Negocio.Web.Models.ViewModels;

namespace Negocio.Web.Controllers
{
    public class ProductoController : Controller
    {
        private readonly NegocioDbContext _context;

        public ProductoController(NegocioDbContext context)
        {
            _context = context;
        }

        // LISTADO
        public IActionResult Index(string? buscar, string? area)
        {
            if (HttpContext.Session.GetString("ROL") != "ADMINISTRADOR")
                return RedirectToAction("Index", "Login");

            var productos = _context.Productos.AsQueryable();

            // 👇 IMPORTANTE: si no escribe nada, no carga nada
            if (string.IsNullOrWhiteSpace(buscar))
            {
                ViewBag.AreaSeleccionada = area;
                ViewBag.Busqueda = buscar;
                return View(new List<Producto>());
            }

            // 🔍 Filtro por descripción
            productos = productos.Where(p => p.Descripcion.Contains(buscar));

            // 📂 Filtro por área (opcional)
            if (!string.IsNullOrWhiteSpace(area))
            {
                productos = productos.Where(p => p.Area == area);
            }

            ViewBag.AreaSeleccionada = area;
            ViewBag.Busqueda = buscar;

            return View(productos.ToList());
        }

        [HttpGet]
        public IActionResult BuscarProductos(string termino, string? area)
        {
            if (string.IsNullOrWhiteSpace(termino))
                return Json(new List<object>());

            var productos = _context.Productos
                .Where(p => p.Descripcion.Contains(termino));

            if (!string.IsNullOrWhiteSpace(area))
            {
                productos = productos.Where(p => p.Area == area);
            }

            var resultado = productos
                .Select(p => new
                {
                    p.IdProducto,
                    p.Descripcion,
                    p.Unidad,
                    p.StockActual,
                    p.PrecioVenta,
                    p.Dimensiones,
                    p.ImagenUrl
                })
                .Take(20)
                .ToList();

            return Json(resultado);
        }

        // GET
        public IActionResult Registrar()
        {
            if (HttpContext.Session.GetString("ROL") != "ADMINISTRADOR")
                return RedirectToAction("Index", "Login");

            return View(new RegistrarProductoViewModel());
        }

        // POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registrar(
    RegistrarProductoViewModel vm,
    IFormFile? imagen)
        {
            if (HttpContext.Session.GetString("ROL") != "ADMINISTRADOR")
                return RedirectToAction("Index", "Login");

            if (!ModelState.IsValid)
                return View(vm);

            string? imagenUrl = null;

            if (imagen != null && imagen.Length > 0)
            {
                var carpeta = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot/img/productos"
                );

                if (!Directory.Exists(carpeta))
                    Directory.CreateDirectory(carpeta);

                var nombreArchivo = $"{Guid.NewGuid()}{Path.GetExtension(imagen.FileName)}";
                var rutaFisica = Path.Combine(carpeta, nombreArchivo);

                using var stream = new FileStream(rutaFisica, FileMode.Create);
                await imagen.CopyToAsync(stream);

                imagenUrl = $"/img/productos/{nombreArchivo}";
            }

            var producto = new Producto
            {
                Descripcion = vm.Descripcion,
                Area = vm.Area,
                Unidad = vm.Unidad,
                Dimensiones = vm.Dimensiones,
                PrecioCompra = vm.PrecioCompra,
                PrecioVenta = vm.PrecioVenta,
                StockActual = vm.StockInicial,
                ConversionFactor = vm.ConversionFactor,
                ImagenUrl = imagenUrl
            };

            _context.Productos.Add(producto);
            await _context.SaveChangesAsync();

            TempData["ok"] = "Producto registrado correctamente";
            return RedirectToAction(nameof(Index));
        }


        // GET: Editar
        public IActionResult Editar(int id)
        {
            if (HttpContext.Session.GetString("ROL") != "ADMINISTRADOR")
                return RedirectToAction("Index", "Login");

            var producto = _context.Productos.Find(id);
            if (producto == null)
                return NotFound();

            return View(producto);
        }


        [HttpPost]
        public async Task<IActionResult> Editar(Producto model, IFormFile? imagen)
        {
            var producto = _context.Productos.Find(model.IdProducto);
            if (producto == null)
                return NotFound();

            // ===== IMAGEN =====
            if (imagen != null && imagen.Length > 0)
            {
                var carpeta = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot/img/productos"
                );

                if (!Directory.Exists(carpeta))
                    Directory.CreateDirectory(carpeta);

                var nombreArchivo = $"{Guid.NewGuid()}{Path.GetExtension(imagen.FileName)}";
                var rutaFisica = Path.Combine(carpeta, nombreArchivo);

                using var stream = new FileStream(rutaFisica, FileMode.Create);
                await imagen.CopyToAsync(stream);

                producto.ImagenUrl = $"/img/productos/{nombreArchivo}";
            }

            // ===== CAMPOS =====
            producto.Descripcion = model.Descripcion;
            producto.Area = model.Area;
            producto.Unidad = model.Unidad;
            producto.Activo = model.Activo;
            producto.Dimensiones = model.Dimensiones;
            producto.PrecioVenta = model.PrecioVenta;
            producto.StockActual = model.StockActual;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
