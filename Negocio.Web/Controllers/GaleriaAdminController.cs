using Microsoft.AspNetCore.Mvc;
using Negocio.Web.Data;
using Negocio.Web.Models.Entities;

namespace Negocio.Web.Controllers
{
    public class GaleriaAdminController : Controller
    {
        private readonly NegocioDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public GaleriaAdminController(NegocioDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // ===============================
        // 🔐 VALIDAR ROL (JEFE)
        // ===============================
        private bool TienePermiso()
        {
            var rol = HttpContext.Session.GetString("ROL");
            return rol == "JEFE" || rol == "ADMINISTRADOR";
        }

        // ===============================
        // LISTADO GLOBAL (PANEL ADMIN)
        // ===============================
        public IActionResult Index()
        {
            if (!TienePermiso()) return RedirectToAction("Index", "Login");

            var items = _context.TrabajosMarketing
                .OrderByDescending(x => x.FechaCreacion)
                .ToList();

            return View(items);
        }

        // ===============================
        // FORM CREAR
        // ===============================
        public IActionResult Create()
        {
            if (!TienePermiso()) return RedirectToAction("Index", "Login");
            return View();
        }

        // ===============================
        // CREAR (POST) + SUBIR IMAGEN
        // ===============================
        [HttpPost]
        public async Task<IActionResult> Create(string titulo, string descripcion, string categoria, IFormFile imagen)
        {
            if (!TienePermiso()) return RedirectToAction("Index", "Login");

            if (imagen == null || imagen.Length == 0)
            {
                ModelState.AddModelError("", "Debe subir una imagen válida.");
                return View();
            }

            // 📂 Carpeta destino global para los trabajos en wwwroot/img/trabajos
            var folder = Path.Combine(_webHostEnvironment.WebRootPath, "img", "trabajos");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            // 🖼 Nombre único
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(imagen.FileName)}";
            var filePath = Path.Combine(folder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imagen.CopyToAsync(stream);
            }

            var item = new TrabajoMarketing
            {
                Titulo = titulo,
                Descripcion = descripcion,
                Categoria = categoria,
                ImagenUrl = $"/img/trabajos/{fileName}",
                Activo = true,
                FechaCreacion = DateTime.Now
            };

            _context.TrabajosMarketing.Add(item);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // ===============================
        // 🔄 ACTIVAR / DESACTIVAR
        // ===============================
        [HttpPost]
        public IActionResult Toggle(int id)
        {
            if (!TienePermiso()) return RedirectToAction("Index", "Login");

            var item = _context.TrabajosMarketing.FirstOrDefault(x => x.Id == id);
            if (item == null) return NotFound();

            item.Activo = !item.Activo;
            _context.SaveChanges();

            return RedirectToAction(nameof(Index));
        }

        // ===============================
        // ✏️ EDITAR (GET)
        // ===============================
        public IActionResult Edit(int id)
        {
            if (!TienePermiso()) return RedirectToAction("Index", "Login");

            var item = _context.TrabajosMarketing.Find(id);
            if (item == null) return NotFound();

            return View(item);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, string titulo, string descripcion, string categoria, IFormFile? imagen)
        {
            if (!TienePermiso()) return RedirectToAction("Index", "Login");

            var item = _context.TrabajosMarketing.FirstOrDefault(x => x.Id == id);
            if (item == null) return NotFound();

            item.Titulo = titulo;
            item.Descripcion = descripcion;
            item.Categoria = categoria;

            if (imagen != null && imagen.Length > 0)
            {
                // Borrar imagen vieja
                if (!string.IsNullOrEmpty(item.ImagenUrl))
                {
                    var oldPath = Path.Combine(_webHostEnvironment.WebRootPath, item.ImagenUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                }

                // Guardar nueva imagen
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(imagen.FileName)}";
                var folder = Path.Combine(_webHostEnvironment.WebRootPath, "img", "trabajos");
                var filePath = Path.Combine(folder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imagen.CopyToAsync(stream);
                }
                item.ImagenUrl = $"/img/trabajos/{fileName}";
            }

            _context.SaveChanges();
            return RedirectToAction(nameof(Index));
        }


        // ===============================
        // ❌ ELIMINAR (BD + ARCHIVO)
        // ===============================
        [HttpPost]
        public IActionResult Delete(int id)
        {
            if (!TienePermiso()) return RedirectToAction("Index", "Login");

            var item = _context.TrabajosMarketing.FirstOrDefault(x => x.Id == id);
            if (item == null) return NotFound();

            // Borrar archivo físico del servidor
            if (!string.IsNullOrEmpty(item.ImagenUrl))
            {
                var filePath = Path.Combine(_webHostEnvironment.WebRootPath, item.ImagenUrl.TrimStart('/').Replace("/img/", "img/"));
                if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);
            }

            _context.TrabajosMarketing.Remove(item);
            _context.SaveChanges();

            return RedirectToAction(nameof(Index));
        }
    }
}