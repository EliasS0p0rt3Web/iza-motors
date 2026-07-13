using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Negocio.Web.Data;
using Negocio.Web.Models.Entities;
using Negocio.Web.Models.ViewModels;

namespace Negocio.Web.Controllers
{
    public class CatalogoAdminController : Controller
    {
        private readonly NegocioDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public CatalogoAdminController(NegocioDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Muestra el formulario
        public IActionResult ConfigurarImagen()
        {
            var vm = new ConfigurarImagenVm
            {
                Areas = _context.Productos
                    .Select(p => p.Area)
                    .Distinct()
                    .Select(a => new SelectListItem(a, a))
                    .ToList()
            };
            return View(vm);
        }

        // POST: Procesa la subida
        [HttpPost]
        public async Task<IActionResult> ConfigurarImagen(ConfigurarImagenVm vm)
        {
            if (vm.Archivo == null || string.IsNullOrEmpty(vm.Descripcion))
            {
                ModelState.AddModelError("", "Debes seleccionar una descripción y una imagen.");
                return View(vm);
            }

            // 1. Guardar el archivo físicamente en wwwroot/img/public
            string carpetaCarga = Path.Combine(_webHostEnvironment.WebRootPath, "img", "public");
            if (!Directory.Exists(carpetaCarga)) Directory.CreateDirectory(carpetaCarga);

            // Nombre único para evitar conflictos: area_descripcion.jpg
            string nombreLimpio = $"{vm.Area}_{vm.Descripcion}".Replace(" ", "_").ToLower();
            string extension = Path.GetExtension(vm.Archivo.FileName);
            string nombreArchivo = nombreLimpio + extension;
            string rutaCompleta = Path.Combine(carpetaCarga, nombreArchivo);

            using (var fileStream = new FileStream(rutaCompleta, FileMode.Create))
            {
                await vm.Archivo.CopyToAsync(fileStream);
            }

            // 2. Registrar en la Base de Datos (Upsert: Update or Insert)
            var registroExistente = await _context.DescripcionImagenes
                .FirstOrDefaultAsync(x => x.Area == vm.Area && x.Descripcion == vm.Descripcion);

            if (registroExistente != null)
            {
                registroExistente.ImagenUrl = nombreArchivo;
                registroExistente.FechaActualizacion = DateTime.Now;
            }
            else
            {
                var nuevo = new DescripcionImagen
                {
                    Area = vm.Area,
                    Descripcion = vm.Descripcion,
                    ImagenUrl = nombreArchivo
                };
                _context.DescripcionImagenes.Add(nuevo);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Imagen asignada correctamente a la familia.";

            return RedirectToAction(nameof(ConfigurarImagen));
        }
    }
}