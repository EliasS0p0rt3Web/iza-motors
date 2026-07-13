using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Negocio.Web.Data;
using Negocio.Web.Models.Entities;

namespace Negocio.Web.Controllers
{
    [Authorize]
    public class PedidoCompraController : Controller
    {
        private readonly NegocioDbContext _context;

        public PedidoCompraController(NegocioDbContext context)
        {
            _context = context;
        }

        // =========================
        // LISTADO
        // =========================
        public IActionResult Index()
        {
            var pedidos = _context.PedidosCompra
                .Include(p => p.Detalles)
                .OrderByDescending(p => p.FechaRegistro)
                .ToList();

            return View(pedidos);
        }

        // =========================
        // CREAR (GET)
        // =========================
        [Authorize(Roles = "ADMINISTRADOR")]
        public IActionResult Crear()
        {
            ViewBag.Areas = _context.Productos
                .Where(p => p.Activo)
                .Select(p => p.Area)
                .Distinct()
                .OrderBy(a => a)
                .ToList();

            return View(new PedidoCompra());
        }

        // =========================
        // CREAR (POST)
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Crear(PedidoCompra pedido)
        {
            if (pedido.Detalles == null || !pedido.Detalles.Any())
            {
                ModelState.AddModelError("", "Debe agregar al menos un producto.");
                ViewBag.Areas = _context.Productos
                    .Where(p => p.Activo)
                    .Select(p => p.Area)
                    .Distinct()
                    .OrderBy(a => a)
                    .ToList();

                return View(pedido);
            }

            foreach (var detalle in pedido.Detalles)
            {
                var producto = _context.Productos
                    .FirstOrDefault(p => p.IdProducto == detalle.IdProducto);

                if (producto != null)
                {
                    detalle.Producto = producto.Descripcion;
                    detalle.Dimensiones = producto.Dimensiones;
                    detalle.Area = producto.Area;
                    detalle.StockActual = producto.StockActual;
                }
            }

            pedido.FechaRegistro = DateTime.UtcNow;
            pedido.Estado = "PENDIENTE";
            pedido.Usuario = User.Identity?.Name ?? "Sistema";

            _context.PedidosCompra.Add(pedido);
            _context.SaveChanges();

            return RedirectToAction(nameof(Index));
        }


        // =========================
        // AJAX: PRODUCTOS POR ÁREA
        // =========================
        [HttpGet]
        public IActionResult ObtenerProductosPorArea(string area)
        {
            var productos = _context.Productos
                .Where(p => p.Area == area && p.Activo)
                .GroupBy(p => p.Descripcion)
                .Select(g => new { descripcion = g.Key })
                .OrderBy(p => p.descripcion)
                .ToList();

            return Json(productos);
        }

        // =========================
        // AJAX: DIMENSIONES
        // =========================
        [HttpGet]
        public IActionResult ObtenerDimensiones(string area, string descripcion)
        {
            var dimensiones = _context.Productos
                .Where(p =>
                    p.Area == area &&
                    p.Descripcion == descripcion &&
                    p.Activo)
                .Select(p => p.Dimensiones)
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            return Json(dimensiones);
        }

        // =========================
        // AJAX: PRODUCTO FINAL + STOCK
        // =========================
        [HttpGet]
        public IActionResult ObtenerProductoFinal(string area, string descripcion, string dimensiones)
        {
            var producto = _context.Productos.FirstOrDefault(p =>
                p.Area == area &&
                p.Descripcion == descripcion &&
                p.Dimensiones == dimensiones &&
                p.Activo);

            if (producto == null)
                return NotFound();

            return Json(new
            {
                producto.IdProducto,
                producto.StockActual
            });
        }

        // =========================
        // CUMPLIR PEDIDO (JEFE)
        // =========================
        [Authorize(Roles = "JEFE")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Cumplir(int id)
        {
            var pedido = _context.PedidosCompra
                .FirstOrDefault(p => p.IdPedidoCompra == id);

            if (pedido == null)
                return NotFound();

            pedido.Estado = "CUMPLIDO";
            _context.SaveChanges();

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // ELIMINAR PEDIDO (ADMIN)
        // =========================
        [Authorize(Roles = "ADMINISTRADOR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Eliminar(int id)
        {
            var pedido = _context.PedidosCompra
                .Include(p => p.Detalles)
                .FirstOrDefault(p => p.IdPedidoCompra == id);

            if (pedido == null)
                return NotFound();

            if (pedido.Estado != "CUMPLIDO")
                return BadRequest("Solo se pueden eliminar pedidos cumplidos.");

            _context.PedidoCompraDetalles.RemoveRange(pedido.Detalles);
            _context.PedidosCompra.Remove(pedido);
            _context.SaveChanges();

            return RedirectToAction(nameof(Index));
        }


    }
}
