using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Negocio.Web.Data;
using Negocio.Web.Models.Entities;

namespace Negocio.Web.Controllers
{
    public class GaleriaAdminController : Controller
    {
        private const long MaxImageSize = 5 * 1024 * 1024; // 5 MB

        private static readonly HashSet<string> AllowedExtensions =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ".jpg",
                ".jpeg",
                ".png",
                ".webp"
            };

        private static readonly HashSet<string> AllowedContentTypes =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "image/jpeg",
                "image/png",
                "image/webp"
            };

        private readonly NegocioDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<GaleriaAdminController> _logger;

        public GaleriaAdminController(
            NegocioDbContext context,
            IWebHostEnvironment webHostEnvironment,
            ILogger<GaleriaAdminController> logger)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        // =========================================================
        // VALIDACIÓN DE PERMISOS
        // =========================================================
        private bool TienePermiso()
        {
            var rol = HttpContext.Session
                .GetString("ROL")?
                .Trim()
                .ToUpperInvariant();

            return rol is
                "ADMINISTRADOR" or
                "JEFE" or
                "JEFE_TALLER" or
                "JEFE DE TALLER";
        }

        private IActionResult RedirigirSinPermiso()
        {
            TempData["Error"] = "No tienes permiso para acceder a la galería.";

            return RedirectToAction(
                actionName: "Index",
                controllerName: "Login"
            );
        }

        // =========================================================
        // LISTADO DE SERVICIOS PUBLICADOS
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Index(
            CancellationToken cancellationToken)
        {
            if (!TienePermiso())
            {
                return RedirigirSinPermiso();
            }

            var items = await _context.TrabajosMarketing
                .AsNoTracking()
                .OrderByDescending(x => x.FechaCreacion)
                .ToListAsync(cancellationToken);

            return View(items);
        }

        // =========================================================
        // FORMULARIO DE CREACIÓN
        // =========================================================
        [HttpGet]
        public IActionResult Create()
        {
            if (!TienePermiso())
            {
                return RedirigirSinPermiso();
            }

            return View();
        }

        // =========================================================
        // REGISTRAR SERVICIO Y SUBIR IMAGEN
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            string titulo,
            string? descripcion,
            string categoria,
            IFormFile? imagen,
            CancellationToken cancellationToken)
        {
            if (!TienePermiso())
            {
                return RedirigirSinPermiso();
            }

            titulo = titulo?.Trim() ?? string.Empty;
            categoria = categoria?.Trim() ?? string.Empty;
            descripcion = descripcion?.Trim();

            ValidarDatosPublicacion(
                titulo,
                categoria,
                descripcion
            );

            var imageError = ValidarImagen(
                imagen,
                requerida: true
            );

            if (imageError != null)
            {
                ModelState.AddModelError(
                    nameof(imagen),
                    imageError
                );
            }

            if (!ModelState.IsValid)
            {
                return View();
            }

            string? nuevaImagenUrl = null;

            try
            {
                nuevaImagenUrl = await GuardarImagenAsync(
                    imagen!,
                    cancellationToken
                );

                var item = new TrabajoMarketing
                {
                    Titulo = titulo,
                    Descripcion = descripcion,
                    Categoria = categoria,
                    ImagenUrl = nuevaImagenUrl,
                    Activo = true,
                    FechaCreacion = DateTime.Now
                };

                _context.TrabajosMarketing.Add(item);

                await _context.SaveChangesAsync(
                    cancellationToken
                );

                TempData["Exito"] =
                    "El servicio fue publicado correctamente.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrWhiteSpace(nuevaImagenUrl))
                {
                    EliminarImagenFisica(nuevaImagenUrl);
                }

                _logger.LogError(
                    ex,
                    "Error al registrar un servicio en la galería."
                );

                ModelState.AddModelError(
                    string.Empty,
                    "No se pudo guardar la publicación. Inténtalo nuevamente."
                );

                return View();
            }
        }

        // =========================================================
        // ACTIVAR O DESACTIVAR PUBLICACIÓN
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(
            int id,
            CancellationToken cancellationToken)
        {
            if (!TienePermiso())
            {
                return RedirigirSinPermiso();
            }

            var item = await _context.TrabajosMarketing
                .FirstOrDefaultAsync(
                    x => x.Id == id,
                    cancellationToken
                );

            if (item == null)
            {
                return NotFound();
            }

            item.Activo = !item.Activo;

            await _context.SaveChangesAsync(
                cancellationToken
            );

            TempData["Exito"] = item.Activo
                ? "La publicación ahora es visible en la web."
                : "La publicación fue ocultada de la web.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // EDICIÓN GET
        // Puede mantenerse por si más adelante se crea una vista
        // independiente. El modal actual utiliza directamente POST.
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Edit(
            int id,
            CancellationToken cancellationToken)
        {
            if (!TienePermiso())
            {
                return RedirigirSinPermiso();
            }

            var item = await _context.TrabajosMarketing
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.Id == id,
                    cancellationToken
                );

            if (item == null)
            {
                return NotFound();
            }

            return View(item);
        }

        // =========================================================
        // EDITAR SERVICIO
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            string titulo,
            string? descripcion,
            string categoria,
            IFormFile? imagen,
            CancellationToken cancellationToken)
        {
            if (!TienePermiso())
            {
                return RedirigirSinPermiso();
            }

            titulo = titulo?.Trim() ?? string.Empty;
            categoria = categoria?.Trim() ?? string.Empty;
            descripcion = descripcion?.Trim();

            if (string.IsNullOrWhiteSpace(titulo))
            {
                TempData["Error"] =
                    "El título del servicio es obligatorio.";

                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(categoria))
            {
                TempData["Error"] =
                    "La categoría del servicio es obligatoria.";

                return RedirectToAction(nameof(Index));
            }

            var imageError = ValidarImagen(
                imagen,
                requerida: false
            );

            if (imageError != null)
            {
                TempData["Error"] = imageError;

                return RedirectToAction(nameof(Index));
            }

            var item = await _context.TrabajosMarketing
                .FirstOrDefaultAsync(
                    x => x.Id == id,
                    cancellationToken
                );

            if (item == null)
            {
                return NotFound();
            }

            var imagenAnteriorUrl = item.ImagenUrl;
            string? nuevaImagenUrl = null;

            try
            {
                if (imagen != null && imagen.Length > 0)
                {
                    nuevaImagenUrl = await GuardarImagenAsync(
                        imagen,
                        cancellationToken
                    );
                }

                item.Titulo = titulo;
                item.Descripcion = descripcion;
                item.Categoria = categoria;

                if (!string.IsNullOrWhiteSpace(nuevaImagenUrl))
                {
                    item.ImagenUrl = nuevaImagenUrl;
                }

                await _context.SaveChangesAsync(
                    cancellationToken
                );

                /*
                 * La imagen anterior se elimina solamente después
                 * de confirmar que los cambios fueron guardados
                 * correctamente en la base de datos.
                 */
                if (!string.IsNullOrWhiteSpace(nuevaImagenUrl) &&
                    !string.IsNullOrWhiteSpace(imagenAnteriorUrl))
                {
                    EliminarImagenFisica(imagenAnteriorUrl);
                }

                TempData["Exito"] =
                    "El servicio fue actualizado correctamente.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                /*
                 * Si la base de datos falla, eliminamos únicamente
                 * la imagen nueva y conservamos la anterior.
                 */
                if (!string.IsNullOrWhiteSpace(nuevaImagenUrl))
                {
                    EliminarImagenFisica(nuevaImagenUrl);
                }

                _logger.LogError(
                    ex,
                    "Error al editar la publicación con ID {Id}.",
                    id
                );

                TempData["Error"] =
                    "No se pudo actualizar la publicación.";

                return RedirectToAction(nameof(Index));
            }
        }

        // =========================================================
        // ELIMINAR PUBLICACIÓN
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(
            int id,
            CancellationToken cancellationToken)
        {
            if (!TienePermiso())
            {
                return RedirigirSinPermiso();
            }

            var item = await _context.TrabajosMarketing
                .FirstOrDefaultAsync(
                    x => x.Id == id,
                    cancellationToken
                );

            if (item == null)
            {
                return NotFound();
            }

            var imagenUrl = item.ImagenUrl;

            try
            {
                /*
                 * Primero se elimina el registro. Si la operación
                 * de base de datos falla, no se pierde la imagen.
                 */
                _context.TrabajosMarketing.Remove(item);

                await _context.SaveChangesAsync(
                    cancellationToken
                );

                /*
                 * Después de confirmar la eliminación en la base
                 * de datos se intenta borrar el archivo físico.
                 */
                if (!string.IsNullOrWhiteSpace(imagenUrl))
                {
                    EliminarImagenFisica(imagenUrl);
                }

                TempData["Exito"] =
                    "La publicación fue eliminada correctamente.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al eliminar la publicación con ID {Id}.",
                    id
                );

                TempData["Error"] =
                    "No se pudo eliminar la publicación.";

                return RedirectToAction(nameof(Index));
            }
        }

        // =========================================================
        // VALIDAR CAMPOS DE LA PUBLICACIÓN
        // =========================================================
        private void ValidarDatosPublicacion(
            string titulo,
            string categoria,
            string? descripcion)
        {
            if (string.IsNullOrWhiteSpace(titulo))
            {
                ModelState.AddModelError(
                    nameof(titulo),
                    "El título del servicio es obligatorio."
                );
            }
            else if (titulo.Length > 150)
            {
                ModelState.AddModelError(
                    nameof(titulo),
                    "El título no puede superar los 150 caracteres."
                );
            }

            if (string.IsNullOrWhiteSpace(categoria))
            {
                ModelState.AddModelError(
                    nameof(categoria),
                    "Debe seleccionar una categoría."
                );
            }
            else if (categoria.Length > 100)
            {
                ModelState.AddModelError(
                    nameof(categoria),
                    "La categoría no puede superar los 100 caracteres."
                );
            }

            if (!string.IsNullOrWhiteSpace(descripcion) &&
                descripcion.Length > 1000)
            {
                ModelState.AddModelError(
                    nameof(descripcion),
                    "La descripción no puede superar los 1000 caracteres."
                );
            }
        }

        // =========================================================
        // VALIDAR ARCHIVO DE IMAGEN
        // =========================================================
        private static string? ValidarImagen(
            IFormFile? imagen,
            bool requerida)
        {
            if (imagen == null || imagen.Length == 0)
            {
                return requerida
                    ? "Debe seleccionar una fotografía válida."
                    : null;
            }

            if (imagen.Length > MaxImageSize)
            {
                return "La imagen no puede superar los 5 MB.";
            }

            var extension = Path.GetExtension(
                imagen.FileName
            );

            if (string.IsNullOrWhiteSpace(extension) ||
                !AllowedExtensions.Contains(extension))
            {
                return "Solo se permiten imágenes JPG, JPEG, PNG o WEBP.";
            }

            if (string.IsNullOrWhiteSpace(imagen.ContentType) ||
                !AllowedContentTypes.Contains(imagen.ContentType))
            {
                return "El tipo de archivo seleccionado no es válido.";
            }

            return null;
        }

        // =========================================================
        // GUARDAR IMAGEN EN WWWROOT
        // =========================================================
        private async Task<string> GuardarImagenAsync(
            IFormFile imagen,
            CancellationToken cancellationToken)
        {
            var relativeFolder = Path.Combine(
                "img",
                "trabajos"
            );

            var physicalFolder = Path.Combine(
                _webHostEnvironment.WebRootPath,
                relativeFolder
            );

            Directory.CreateDirectory(physicalFolder);

            var extension = Path.GetExtension(
                imagen.FileName
            ).ToLowerInvariant();

            var fileName =
                $"{Guid.NewGuid():N}{extension}";

            var physicalPath = Path.Combine(
                physicalFolder,
                fileName
            );

            await using var stream = new FileStream(
                physicalPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true
            );

            await imagen.CopyToAsync(
                stream,
                cancellationToken
            );

            return $"/img/trabajos/{fileName}";
        }

        // =========================================================
        // ELIMINAR IMAGEN FÍSICA DE FORMA SEGURA
        // =========================================================
        private void EliminarImagenFisica(
            string? imagenUrl)
        {
            if (string.IsNullOrWhiteSpace(imagenUrl))
            {
                return;
            }

            try
            {
                var relativePath = imagenUrl
                    .TrimStart('/')
                    .Replace(
                        '/',
                        Path.DirectorySeparatorChar
                    );

                var webRootFullPath = Path.GetFullPath(
                    _webHostEnvironment.WebRootPath
                );

                var fileFullPath = Path.GetFullPath(
                    Path.Combine(
                        webRootFullPath,
                        relativePath
                    )
                );

                /*
                 * Seguridad: impedir que una ruta almacenada en
                 * la base de datos salga fuera de wwwroot.
                 */
                var protectedRoot =
                    webRootFullPath.TrimEnd(
                        Path.DirectorySeparatorChar
                    ) + Path.DirectorySeparatorChar;

                if (!fileFullPath.StartsWith(
                        protectedRoot,
                        StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning(
                        "Se rechazó una ruta de imagen insegura: {Ruta}",
                        imagenUrl
                    );

                    return;
                }

                if (System.IO.File.Exists(fileFullPath))
                {
                    System.IO.File.Delete(fileFullPath);
                }
            }
            catch (Exception ex)
            {
                /*
                 * Un error eliminando el archivo no debe deshacer
                 * una operación correcta en la base de datos.
                 */
                _logger.LogWarning(
                    ex,
                    "No se pudo eliminar la imagen física {ImagenUrl}.",
                    imagenUrl
                );
            }
        }
    }
}