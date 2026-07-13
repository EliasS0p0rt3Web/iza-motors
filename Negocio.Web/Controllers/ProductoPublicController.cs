using Microsoft.AspNetCore.Mvc;
using Negocio.Web.Data;
using Negocio.Web.Helpers;
using Negocio.Web.Models.ViewModels;
using System.Linq;

namespace Negocio.Web.Controllers
{
    public class ProductoPublicController : Controller
    {
        private readonly NegocioDbContext _context;

        public ProductoPublicController(NegocioDbContext context)
        {
            _context = context;
        }

        // =========================
        // HOME PRODUCTOS (CATEGORÍAS)
        // =========================
        public IActionResult Index()
        {
            return View();
        }

        // =========================
        // TIPOS POR CATEGORÍA
        // (ALUMINIO / ACCESORIOS / ARC)
        // =========================
        public IActionResult Categoria(string area)
        {
            if (string.IsNullOrWhiteSpace(area))
                return RedirectToAction(nameof(Index));

            // 1. Obtenemos los nombres únicos de los productos (lo que ya tenías)
            var tiposDesdeDb = _context.Productos
                .Where(p => p.Area == area && p.Activo)
                .Select(p => p.Descripcion)
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            // 2. Traemos las imágenes que el administrador ya configuró para esta área
            var mapeoImagenes = _context.DescripcionImagenes
                .Where(x => x.Area == area)
                .ToList();

            // 3. 🔥 TRANSFORMACIÓN: Convertimos los strings en el objeto que la vista pide
            // Usamos el nombre exacto que salió en tu error: DescripcionImagen
            var model = tiposDesdeDb.Select(t => new Negocio.Web.Models.ViewModels.DescripcionImagen
            {
                Descripcion = t,
                Area = area,
                // 🔥 Si no hay mapeo, mandamos un nombre que no existe para forzar el fallback
                ImagenUrl = mapeoImagenes.FirstOrDefault(m => m.Descripcion == t)?.ImagenUrl ?? "none.jpg"
            }).ToList();

            ViewBag.Area = area;

            // 4. Enviamos el 'model' que ahora sí es del tipo correcto
            return View("TiposProducto", model);
        }

        // =========================
        // DETALLE DEL PRODUCTO
        // (variantes por dimensión)
        // =========================
        public IActionResult ProductoDetalle(string area, string nombre)
        {
            if (string.IsNullOrWhiteSpace(area) || string.IsNullOrWhiteSpace(nombre))
                return RedirectToAction(nameof(Index));

            var productos = _context.Productos
                .Where(p => p.Area == area && p.Descripcion == nombre && p.Activo)
                .Select(p => new ProductoPublicVm
                {
                    IdProducto = p.IdProducto,
                    Descripcion = p.Descripcion,
                    Dimensiones = p.Dimensiones,
                    StockTotal = p.StockActual,
                    ImagenUrl = p.ImagenUrl,
                    PrecioUnitario = p.PrecioVenta,
                    // ✅ MAPEA LA UNIDAD AQUÍ:
                    Unidad = p.Unidad,
                    // ✅ AGREGA ESTA LÍNEA PARA EL MAPEO:
                    ConversionFactor = p.ConversionFactor
                })
                .OrderBy(p => p.Dimensiones)
                .ToList();

            ViewBag.Area = area;
            ViewBag.Nombre = nombre;

            return View(productos);
        }


        //agregar al carro

        // Agregar al carro con lógica de redondeo de Cotización
        [HttpPost]
        public IActionResult AgregarAlCarrito(int idProducto, decimal cantidad, string tipoVenta)
        {
            if (cantidad <= 0)
                cantidad = 1;

            var producto = _context.Productos
                .FirstOrDefault(p => p.IdProducto == idProducto && p.Activo);

            if (producto == null)
                return RedirectToAction(nameof(Index));

            var carrito = HttpContext.Session.GetObjectFromJson<List<CarritoItemVm>>("CARRITO")
                           ?? new List<CarritoItemVm>();

            // Diferenciamos por ID y por Tipo de Venta para no mezclar metros con unidades
            var existente = carrito.FirstOrDefault(x => x.IdProducto == idProducto && x.TipoVenta == tipoVenta);

            if (existente != null)
            {
                existente.Cantidad += cantidad;
            }
            else
            {
                decimal precioCalculado = producto.PrecioVenta;

                // ============================================================
                // 🔹 LÓGICA DE PRECIO (REPLICADA DE COTIZACIÓN)
                // ============================================================
                if (tipoVenta == "METRO")
                {
                    if (producto.ConversionFactor > 0)
                    {
                        // 🔥 REDONDEO COMERCIAL: Replicamos la fórmula de CalcularCortina
                        precioCalculado = Math.Ceiling(producto.PrecioVenta / producto.ConversionFactor);
                    }
                }
                // Si es VARILLA, el precioUnitario ya es producto.PrecioVenta (el precio base)

                carrito.Add(new CarritoItemVm
                {
                    IdProducto = producto.IdProducto,
                    Area = producto.Area,
                    Descripcion = producto.Descripcion,
                    Dimensiones = producto.Dimensiones,
                    ImagenUrl = producto.ImagenUrl,
                    PrecioUnitario = precioCalculado,
                    Cantidad = cantidad,
                    TipoVenta = tipoVenta // Guardamos la etiqueta (METRO/VARILLA/UNIDAD)
                });
            }

            HttpContext.Session.SetObjectAsJson("CARRITO", carrito);

            // 🚀 AQUÍ ESTÁ EL TRUCO PARA LA ANIMACIÓN:
            // Verificamos si la petición es AJAX (vía Fetch/JavaScript)
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new
                {
                    success = true,
                    totalItems = carrito.Count // Usamos .Count para que el badge marque 1, 2, 3...
                });
            }

            // Si por alguna razón falla el JS o es un post normal, recargamos (fallback)
            return RedirectToAction(nameof(ProductoDetalle), new
            {
                area = producto.Area,
                nombre = producto.Descripcion
            });
        }

        // =========================
        // VER CARRITO
        // =========================
        [HttpGet]
        public IActionResult Carrito()
        {
            var carrito = HttpContext.Session.GetObjectFromJson<List<CarritoItemVm>>("CARRITO")
                          ?? new List<CarritoItemVm>();

            return View(carrito);
        }


        [HttpGet]
        public IActionResult Buscar(string termino)
        {
            if (string.IsNullOrWhiteSpace(termino))
                return Json(new List<object>());

            // 1. Limpiamos espacios extras para evitar búsquedas fallidas
            var busqueda = termino.Trim().ToLower();

            var productos = _context.Productos
                .Where(p => p.Activo && p.Descripcion.ToLower().Contains(busqueda))
                // 2. Agrupamos por descripción para no duplicar nombres idénticos en los resultados
                .GroupBy(p => p.Descripcion)
                .Select(g => new {
                    // 3. Proyectamos de forma limpia sin usar .First() dentro del select, optimizando el SQL generado
                    IdProducto = g.Max(p => p.IdProducto), // Tomamos un ID representativo
                    Descripcion = g.Key,
                    ImagenUrl = g.Max(p => p.ImagenUrl), // Evita fallos si algunas variantes no tienen foto
                    Area = g.Max(p => p.Area),
                    PrecioVenta = g.Min(p => p.PrecioVenta) // Mostramos el precio "Desde S/" (el más económico del grupo)
                })
                .Take(10) // Limitamos a 10 para no saturar la respuesta de red
                .ToList();

            return Json(productos);
        }

        [HttpGet]
        public IActionResult ObtenerMiniCarrito()
        {
            var carrito = HttpContext.Session.GetObjectFromJson<List<CarritoItemVm>>("CARRITO")
                          ?? new List<CarritoItemVm>();
            return PartialView("_MiniCartItems", carrito);
        }

        // =========================
        // QUITAR DEL CARRITO
        // =========================
        [HttpPost]
        public IActionResult QuitarDelCarrito(int idProducto, string tipoVenta)
        {
            var carrito = HttpContext.Session.GetObjectFromJson<List<CarritoItemVm>>("CARRITO")
                          ?? new List<CarritoItemVm>();

            // 🔍 Buscamos la combinación exacta: ID + METRO o ID + VARILLA
            var item = carrito.FirstOrDefault(x => x.IdProducto == idProducto && x.TipoVenta == tipoVenta);

            if (item != null)
            {
                carrito.Remove(item);
                HttpContext.Session.SetObjectAsJson("CARRITO", carrito);
            }

            return RedirectToAction(nameof(Carrito));
        }


    }

    // =========================
    // VIEWMODEL PÚBLICO
    // =========================
    public class ProductoPublicVm
    {
        public int IdProducto { get; set; }
        public string Descripcion { get; set; } = "";
        public string? Dimensiones { get; set; }
        public decimal StockTotal { get; set; }
        public string? ImagenUrl { get; set; }
        public decimal PrecioUnitario { get; set; }

        // 🚀 AGREGA ESTA LÍNEA:
        public string Unidad { get; set; } = null!;

        public decimal ConversionFactor { get; set; }
    }
}