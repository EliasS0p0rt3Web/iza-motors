using MercadoPago.Client.Preference;
using MercadoPago.Config;
using MercadoPago.Resource.Preference;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Negocio.Web.Data;
using Negocio.Web.Models.Entities;
using System.Configuration;

namespace Negocio.Web.Controllers
{
    public class ClienteController : Controller
    {
        private readonly NegocioDbContext _context;
        private readonly IConfiguration _configuration;

        // Cambia los paréntesis para que reciba AMBOS objetos
        public ClienteController(NegocioDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration; // <-- Ahora esto ya no se pintará de rojo

            // Con esto configuras Mercado Pago automáticamente al levantar el controlador
            MercadoPagoConfig.AccessToken = _configuration["MercadoPago:AccessToken"];
        }

        // =================================================================
        // 💳 INICIAR PAGO DIGITAL (MERCADO PAGO CON CÁLCULO DINÁMICO)
        // =================================================================
        [HttpPost]
        [Authorize(AuthenticationSchemes = "Cookies", Roles = "CLIENTE")]
        public async Task<IActionResult> IniciarPago(int id)
        {
            // 1. Buscamos la reserva con tu idUsuario de sesión (Candado de Seguridad)
            int idUsuarioSesion = HttpContext.Session.GetInt32("USER_ID") ?? 0;
            var reserva = await _context.Reservas
                .FirstOrDefaultAsync(r => r.ReservaId == id && r.IdUsuario == idUsuarioSesion);

            if (reserva == null) return NotFound(new { mensaje = "No se encontró la reserva." });

            // 🔒 ESCUDO: Si ya no está PENDIENTE, congelamos cualquier intento de pago nuevo
            if (reserva.Estado != "PENDIENTE")
            {
                return BadRequest(new { mensaje = "Esta reserva ya fue procesada o no está pendiente." });
            }

            // 🧠 LÓGICA INTELIGENTE DE COBRO DINÁMICO
            decimal porcentajeCobro = 0.20m; // Por defecto 20% para productos estándar (Varillas/Accesorios)
            string tipoCobroText = "Adelanto Inicial (20%)";

            if (!string.IsNullOrEmpty(reserva.DetalleJson))
            {
                try
                {
                    // Convertimos a mayúsculas para evitar problemas de tipeo o minúsculas en el JSON
                    string jsonUpper = reserva.DetalleJson.ToUpper();

                    // Si el JSON contiene la palabra METRO, activa obligatoriamente el 50% por cortes de precisión
                    if (jsonUpper.Contains("METRO"))
                    {
                        porcentajeCobro = 0.50m;
                        tipoCobroText = "Adelanto por Corte de Precisión (50%)";
                    }
                }
                catch { }
            }

            // Calculamos el monto exacto congelando el precio de la base de datos
            decimal montoACobrar = (decimal)reserva.Total * porcentajeCobro;

            // 2. Armamos el item con los datos calculados por el sistema
            var item = new PreferenceItemRequest
            {
                Id = reserva.ReservaId.ToString(),
                Title = $"{tipoCobroText} - Orden {reserva.CodigoReserva}",
                Quantity = 1,
                CurrencyId = "PEN", // Soles peruanos
                UnitPrice = Math.Round(montoACobrar, 2) // Mandamos el monto final redondeado
            };

            // 3. Creamos la orden para enviarla a Mercado Pago con URL real de producción
            var request = new PreferenceRequest
            {
                Items = new List<PreferenceItemRequest> { item },
                BackUrls = new PreferenceBackUrlsRequest
                {
                    // 🔥 Cuando esté en producción con tu dominio, Azure usará estas URLs automáticamente
                    Success = "https://www.eealuminios.com/Cliente/MisPedidos",
                    Failure = "https://www.eealuminios.com/Cliente/MisPedidos",
                    Pending = "https://www.eealuminios.com/Cliente/MisPedidos"
                },
                AutoReturn = "approved",
                ExternalReference = reserva.CodigoReserva
            };

            // 4. Disparamos la petición al servidor de Mercado Pago
            var client = new PreferenceClient();
            Preference preference = await client.CreateAsync(request);

            // 5. Respondemos con la URL del checkout para que JavaScript la abra
            return Json(new { urlPago = preference.InitPoint });
        }

        // =================================================================
        // 🏪 NUEVA ACCIÓN: CONFIRMAR RECOJO EN TIENDA (PAGO A CONTRATREGA EN PUESTO)
        // =================================================================
        [HttpPost]
        [Authorize(AuthenticationSchemes = "Cookies", Roles = "CLIENTE")]
        public async Task<IActionResult> ConfirmarRecojoTienda(int id)
        {
            int idUsuarioSesion = HttpContext.Session.GetInt32("USER_ID") ?? 0;

            var reserva = await _context.Reservas
                .FirstOrDefaultAsync(r => r.ReservaId == id && r.IdUsuario == idUsuarioSesion);

            if (reserva == null) return NotFound(new { mensaje = "No se encontró la reserva." });

            // Validaciones de seguridad básicas
            if (reserva.Estado != "PENDIENTE" || reserva.TipoEntrega != "RECOJO")
            {
                return BadRequest(new { mensaje = "Acción no permitida para este tipo de orden." });
            }

            // Seguridad extra: Si intentan meter un hack y contiene metros, rebotamos
            if (!string.IsNullOrEmpty(reserva.DetalleJson) && reserva.DetalleJson.ToUpper().Contains("METRO"))
            {
                return BadRequest(new { mensaje = "Los productos con corte requieren adelanto digital obligatorio." });
            }

            // Al ser recojo físico en tienda y sin cortes, dejamos el Adelanto en 0 
            // pero el flujo avanza para que el Admin valide stock en el puesto
            reserva.Adelanto = 0;
            // Lo dejamos en PENDIENTE o un estado intermedio, pero como tú manejas el panel manual
            // se queda guardado listo para que te llegue el aviso.

            await _context.SaveChangesAsync();

            // Retornamos éxito para que JavaScript arme el link dinámico de WhatsApp
            return Json(new
            {
                success = true,
                codigo = reserva.CodigoReserva,
                total = reserva.Total.ToString("N2")
            });
        }

        // =================================================================
        // 🛠️ SECCIÓN ADMINISTRATIVA (Solo Admin y Jefe)
        // =================================================================

        [Authorize(Roles = "ADMINISTRADOR,JEFE")]
        public async Task<IActionResult> Index()
        {
            // 1️⃣ Jalar solo los usuarios que tengan el Rol de Cliente
            var usuariosClientes = await _context.Usuarios
                .Where(u => u.Rol == "CLIENTE")
                .ToListAsync();

            // 2️⃣ Jalar los perfiles creados para hacer el cruce en la vista
            var perfiles = await _context.PerfilesClientes.ToListAsync();

            // 3️⃣ Cargar minicontadores en los ViewBags para los KPI de la intranet
            ViewBag.TotalClientes = usuariosClientes.Count;
            ViewBag.ClientesActivos = usuariosClientes.Count(u => u.Activo);
            ViewBag.ClientesBloqueados = usuariosClientes.Count(u => !u.Activo);

            // Pasamos los usuarios a la vista (el cruce lo haremos limpio con Linq en el HTML)
            ViewBag.Perfiles = perfiles;
            return View(usuariosClientes);
        }

        // Accion para dar de baja o activar a un cliente desde el panel admin
        [HttpPost]
        [Authorize(Roles = "ADMINISTRADOR,JEFE")]
        public async Task<IActionResult> AlternarEstado(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario != null)
            {
                // Swapeamos el estado (si esta activo lo desactiva, y viceversa)
                usuario.Activo = !usuario.Activo;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // Accion para eliminar por completo la cuenta y su perfil (gracias al Cascade Delete)
        [HttpPost]
        [Authorize(Roles = "ADMINISTRADOR")] // Solo el Admin supremo puede borrar de la BD
        public async Task<IActionResult> Eliminar(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario != null)
            {
                _context.Usuarios.Remove(usuario);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }


        // =================================================================
        // 👤 SECCIÓN DEL CLIENTE (E&E Aluminios - Intranet)
        // =================================================================

        // 🌐 1. NUEVA ACCIÓN: Carga la landing principal incrustada a la derecha
        [Authorize(AuthenticationSchemes = "Cookies", Roles = "CLIENTE")]
        [HttpGet]
        public IActionResult WebPrincipal()
        {
            // Validamos que la sesión no haya expirado
            if (HttpContext.Session.GetInt32("USER_ID") == null)
            {
                return RedirectToAction("Index", "Login");
            }
            return View();
        }

        // 📦 2. HISTORIAL: Tus pedidos/reservas de aluminio y vidrio
        [Authorize(AuthenticationSchemes = "Cookies", Roles = "CLIENTE")]
        [HttpGet]
        public async Task<IActionResult> MisPedidos()
        {
            int idUsuario = HttpContext.Session.GetInt32("USER_ID") ?? 0;
            if (idUsuario == 0) return RedirectToAction("Index", "Login");

            var misReservas = await _context.Reservas
                .Where(r => r.IdUsuario == idUsuario)
                .OrderByDescending(r => r.FechaRegistro)
                .ToListAsync();

            return View(misReservas);
        }

        // 🔥 NUEVA ACCIÓN: Visualizar el detalle automático de una fila seleccionada
        // 🔥 ACCIÓN ACTUALIZADA: Visualizar el detalle automático con tolerancia a Mayús/Minús en JSON
        [Authorize(AuthenticationSchemes = "Cookies", Roles = "CLIENTE")]
        [HttpGet]
        public async Task<IActionResult> VerPedido(int id)
        {
            // 1️⃣ Validar sesión activa del cliente
            int idUsuarioSesion = HttpContext.Session.GetInt32("USER_ID") ?? 0;
            if (idUsuarioSesion == 0) return RedirectToAction("Index", "Login");

            // 2️⃣ Buscar la reserva asegurando el Candado de Seguridad
            var reserva = await _context.Reservas
                .FirstOrDefaultAsync(r => r.ReservaId == id && r.IdUsuario == idUsuarioSesion);

            // 3️⃣ Protección contra manipulaciones de URL
            if (reserva == null)
            {
                return NotFound();
            }

            // 4️⃣ Retorna la vista asegurando que el nombre del archivo sea exacto
            // NOTA: Si tu archivo .cshtml se llama "VerPedido", cambia "DetallePedido" por "VerPedido"
            return View("VerPedido", reserva);
        }


        // 📄 ACCIÓN: Historial de Cotizaciones a Medida (Proyectos/Wizard)
        [Authorize(AuthenticationSchemes = "Cookies", Roles = "CLIENTE")]
        [HttpGet]
        public async Task<IActionResult> MisCotizaciones()
        {
            // 1️⃣ Validar sesión activa del cliente
            int idUsuario = HttpContext.Session.GetInt32("USER_ID") ?? 0;
            if (idUsuario == 0) return RedirectToAction("Index", "Login");

            // 2️⃣ Jalamos todas las reservas del usuario logueado
            var todasMisReservas = await _context.Reservas
                .Where(r => r.IdUsuario == idUsuario)
                .OrderByDescending(r => r.FechaRegistro)
                .ToListAsync();

            // 3️⃣ FILTRADO INTELIGENTE: 
            // Filtramos en memoria solo las que NO sean del catálogo de productos estándar
            var misCotizaciones = todasMisReservas.Where(r =>
            {
                if (string.IsNullOrEmpty(r.DetalleJson)) return true; // Si está vacío, por defecto es cotización antigua
                try
                {
                    using (var doc = System.Text.Json.JsonDocument.Parse(r.DetalleJson))
                    {
                        var root = doc.RootElement;
                        // Si el JSON contiene "tipo" y es "CATALOGO_PRODUCTOS", es un pedido manual, NO una cotización a medida
                        if (root.TryGetProperty("tipo", out var tipoProp) && tipoProp.GetString() == "CATALOGO_PRODUCTOS")
                        {
                            return false;
                        }
                    }
                }
                catch { }
                return true; // Si no tiene el flag de catálogo, se clasifica como Cotización a Medida
            }).ToList();

            return View(misCotizaciones);
        }

        // 👤 3. VER PERFIL (GET): Cargamos datos de Usuario, Perfil y Direcciones juntos
        [Authorize(AuthenticationSchemes = "Cookies", Roles = "CLIENTE")]
        [HttpGet]
        public async Task<IActionResult> MiPerfil()
        {
            int idUsuario = HttpContext.Session.GetInt32("USER_ID") ?? 0;
            if (idUsuario == 0) return RedirectToAction("Index", "Login");

            var perfil = await _context.PerfilesClientes.FirstOrDefaultAsync(p => p.IdUsuario == idUsuario);
            ViewBag.Usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.IdUsuario == idUsuario);

            // 🔥 JALAMOS LAS DIRECCIONES ACTIVAS DEL USUARIO PARA LA VISTA
            ViewBag.Direcciones = await _context.DireccionesClientes
                .Where(d => d.IdUsuario == idUsuario && d.Activo)
                .ToListAsync();

            if (perfil != null)
            {
                // 🛡️ Si ya es otro día diferente al de la última actualización, reseteamos su contador diario a 0
                if (perfil.UltimaActualizacion.HasValue && perfil.UltimaActualizacion.Value.Date < DateTime.Now.Date)
                {
                    perfil.ActualizacionesHoy = 0;
                    _context.PerfilesClientes.Update(perfil);
                    await _context.SaveChangesAsync();
                }
            }

            if (perfil == null) return RedirectToAction("Index", "HomePublic");
            return View(perfil);
        }

        // 💾 4. GUARDAR CAMBIOS (POST): Procesa la actualización del formulario
        [HttpPost]
        [Authorize(AuthenticationSchemes = "Cookies", Roles = "CLIENTE")]
        public async Task<IActionResult> GuardarPerfil(string nombreCompleto, string telefono, string documento)
        {
            int idUsuario = HttpContext.Session.GetInt32("USER_ID") ?? 0;
            if (idUsuario == 0) return RedirectToAction("Index", "Login");

            var perfil = await _context.PerfilesClientes.FirstOrDefaultAsync(p => p.IdUsuario == idUsuario);

            if (perfil != null)
            {
                // 1️⃣ VALIDAR LÍMITE DIARIO (Máximo 3)
                if (perfil.ActualizacionesHoy >= 3)
                {
                    TempData["Error"] = "Ya alcanzaste el límite máximo de 3 actualizaciones por hoy, mi king.";
                    return RedirectToAction(nameof(MiPerfil));
                }

                // 2️⃣ VALIDAR ENFRIAMIENTO (5 Minutos)
                if (perfil.UltimaActualizacion.HasValue)
                {
                    var minutosPasados = (DateTime.Now - perfil.UltimaActualizacion.Value).TotalMinutes;
                    if (minutosPasados < 5)
                    {
                        TempData["Error"] = "Debes esperar 5 minutos entre cada actualización.";
                        return RedirectToAction(nameof(MiPerfil));
                    }
                }

                // Procesamos la actualización normal
                perfil.NombreCompleto = nombreCompleto.Trim();
                perfil.Telefono = string.IsNullOrWhiteSpace(telefono) ? "No especificado" : telefono.Trim();

                if (string.IsNullOrEmpty(perfil.Documento) && !string.IsNullOrWhiteSpace(documento))
                {
                    perfil.Documento = documento.Replace(" ", "").Trim();
                    perfil.TipoCliente = perfil.Documento.Length == 8 ? "PERSONA NATURAL" : "EMPRESA";
                }

                // 🔥 SUMAMOS LA ACTUALIZACIÓN Y GUARDAMOS LA HORA
                perfil.ActualizacionesHoy += 1;
                perfil.UltimaActualizacion = DateTime.Now;

                _context.PerfilesClientes.Update(perfil);
                await _context.SaveChangesAsync();
                // 🚀 🔥 LA CLAVE AQUÍ: Sobreescribimos la sesión en caliente con el nombre recién guardado
                HttpContext.Session.SetString("USER_NAME", perfil.NombreCompleto);

                TempData["Exito"] = "¡Tus datos se actualizaron correctamente!";
            }

            return RedirectToAction(nameof(MiPerfil));
        }

        [HttpPost]
        [Authorize(AuthenticationSchemes = "Cookies", Roles = "CLIENTE")]
        public async Task<IActionResult> RegistrarDireccion(string tipoUbicacion, string direccionTexto, string distrito, string referencia, string datosReceptor)
        {
            int idUsuario = HttpContext.Session.GetInt32("USER_ID") ?? 0;
            if (idUsuario == 0) return RedirectToAction("Index", "Login");

            if (string.IsNullOrWhiteSpace(direccionTexto) || string.IsNullOrWhiteSpace(distrito))
            {
                TempData["ErrorDireccion"] = "La dirección y el distrito son obligatorios.";
                return RedirectToAction(nameof(MiPerfil));
            }

            var nuevaDireccion = new DireccionCliente
            {
                IdUsuario = idUsuario,
                TipoUbicacion = tipoUbicacion?.ToUpper() ?? "DOMICILIO",
                DireccionTexto = direccionTexto.Trim(),
                Distrito = distrito.Trim(),
                Referencia = string.IsNullOrWhiteSpace(referencia) ? null : referencia.Trim(),
                DatosReceptorAgencia = string.IsNullOrWhiteSpace(datosReceptor) ? null : datosReceptor.Trim(),
                Activo = true
            };

            _context.DireccionesClientes.Add(nuevaDireccion);
            await _context.SaveChangesAsync();

            TempData["ExitoDireccion"] = "¡Dirección guardada correctamente!";
            return RedirectToAction(nameof(MiPerfil));
        }

        [HttpPost]
        [Authorize(AuthenticationSchemes = "Cookies", Roles = "CLIENTE")]
        public async Task<IActionResult> EliminarDireccion(int idDireccion)
        {
            int idUsuario = HttpContext.Session.GetInt32("USER_ID") ?? 0;
            if (idUsuario == 0) return RedirectToAction("Index", "Login");

            // Buscamos la dirección asegurando que pertenezca al usuario en sesión (Candado de seguridad)
            var direccion = await _context.DireccionesClientes
                .FirstOrDefaultAsync(d => d.IdDireccion == idDireccion && d.IdUsuario == idUsuario);

            if (direccion != null)
            {
                // Aplicamos borrado lógico para no romper el historial de reservas antiguas
                direccion.Activo = false;
                _context.DireccionesClientes.Update(direccion);
                await _context.SaveChangesAsync();

                TempData["ExitoDireccion"] = "Dirección removida correctamente.";
            }

            return RedirectToAction(nameof(MiPerfil));
        }

        // 🌐 OBTENER DIRECCIONES EN JSON (Para cargar los combos del carrito de compras)
        [Authorize(AuthenticationSchemes = "Cookies", Roles = "CLIENTE")]
        [HttpGet]
        public async Task<IActionResult> ObtenerDirecciones()
        {
            int idUsuario = HttpContext.Session.GetInt32("USER_ID") ?? 0;
            if (idUsuario == 0) return Unauthorized();

            // Jalamos todas las direcciones activas del usuario logueado
            var todas = await _context.DireccionesClientes
                .Where(d => d.IdUsuario == idUsuario && d.Activo)
                .ToListAsync();

            // Las separamos de forma inteligente para que el JavaScript del front las agrupe al toque
            return Json(new
            {
                direcciones = todas.Where(d => d.TipoUbicacion != "SHALOM").Select(d => new {
                    idDireccion = d.IdDireccion,
                    direccionTexto = d.DireccionTexto,
                    distrito = d.Distrito,
                    referencia = d.Referencia
                }).ToList(),

                agencias = todas.Where(d => d.TipoUbicacion == "SHALOM").Select(a => new {
                    idDireccion = a.IdDireccion,
                    agenciaProvincia = a.DireccionTexto, // Ciudad y Agencia guardada
                    datosReceptor = a.DatosReceptorAgencia
                }).ToList()
            });
        }

        // =================================================================
        // 🔐 CAMBIAR CONTRASEÑA (POST): Procesa el cambio de clave con protección antibombas
        // =================================================================
        [HttpPost]
        [Authorize(AuthenticationSchemes = "Cookies", Roles = "CLIENTE")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarPassword(string passwordActual, string passwordNueva, string passwordConfirmar)
        {
            // 1️⃣ Validar sesión activa
            int idUsuario = HttpContext.Session.GetInt32("USER_ID") ?? 0;
            if (idUsuario == 0) return RedirectToAction("Index", "Login");

            // 2️⃣ Validaciones de campos vacíos o nulos
            if (string.IsNullOrWhiteSpace(passwordActual) ||
                string.IsNullOrWhiteSpace(passwordNueva) ||
                string.IsNullOrWhiteSpace(passwordConfirmar))
            {
                TempData["ErrorPassword"] = "Todos los campos de contraseña son obligatorios.";
                return RedirectToAction(nameof(MiPerfil));
            }

            if (passwordNueva.Length < 6)
            {
                TempData["ErrorPassword"] = "La nueva contraseña debe tener al menos 6 caracteres.";
                return RedirectToAction(nameof(MiPerfil));
            }

            if (passwordNueva != passwordConfirmar)
            {
                TempData["ErrorPassword"] = "La nueva contraseña y la confirmación no coinciden.";
                return RedirectToAction(nameof(MiPerfil));
            }

            try
            {
                // 3️⃣ Buscar al usuario y su perfil en la BD
                var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.IdUsuario == idUsuario);
                var perfil = await _context.PerfilesClientes.FirstOrDefaultAsync(p => p.IdUsuario == idUsuario);

                if (usuario == null || perfil == null)
                {
                    TempData["ErrorPassword"] = "No se encontró el registro del usuario en el sistema.";
                    return RedirectToAction(nameof(MiPerfil));
                }

                // 4️⃣ Interceptar cuenta de Google OAuth
                if (usuario.PasswordHash == "OAUTH_GOOGLE_EXTERNAL_ACCOUNT")
                {
                    TempData["ErrorPassword"] = "Tu cuenta está protegida con Google. No manejas contraseña local.";
                    return RedirectToAction(nameof(MiPerfil));
                }

                // =======================================================
                // 🛡️ EL CANDADO DE SEGURIDAD (REUTILIZANDO TU LÓGICA)
                // =======================================================

                // A. Validar límite diario (Máximo 3)
                if (perfil.ActualizacionesHoy >= 3)
                {
                    TempData["ErrorPassword"] = "Ya alcanzaste el límite máximo de 3 modificaciones por hoy, mi king.";
                    return RedirectToAction(nameof(MiPerfil));
                }

                // B. Validar enfriamiento (5 minutos)
                if (perfil.UltimaActualizacion.HasValue)
                {
                    var minutosPasados = (DateTime.Now - perfil.UltimaActualizacion.Value).TotalMinutes;
                    if (minutosPasados < 5)
                    {
                        TempData["ErrorPassword"] = "Debes esperar 5 minutos entre cada modificación de cuenta.";
                        return RedirectToAction(nameof(MiPerfil));
                    }
                }

                // 5️⃣ Validar la contraseña actual usando BCrypt
                bool esValida = BCrypt.Net.BCrypt.Verify(passwordActual, usuario.PasswordHash);
                if (!esValida)
                {
                    TempData["ErrorPassword"] = "La contraseña actual ingresada es incorrecta.";
                    return RedirectToAction(nameof(MiPerfil));
                }

                // 6️⃣ Hashear la nueva clave y actualizar la seguridad
                usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(passwordNueva);
                _context.Usuarios.Update(usuario);

                // 🔥 IMPACTAMOS EL CONTADOR DE TU PERFIL (Consume un intento y actualiza la hora)
                perfil.ActualizacionesHoy += 1;
                perfil.UltimaActualizacion = DateTime.Now;
                _context.PerfilesClientes.Update(perfil);

                // Guardamos todo de un solo golpe asíncrono
                await _context.SaveChangesAsync();

                TempData["Exito"] = "¡Tu contraseña ha sido actualizada con éxito!";
            }
            catch (Exception)
            {
                TempData["ErrorPassword"] = "Ocurrió un error inesperado al procesar el cambio de contraseña.";
            }

            return RedirectToAction(nameof(MiPerfil));
        }

        // =================================================================
        // 🌐 OBTENER PERFIL EN JSON (Para auto-llenar el modal del carrito)
        // =================================================================
        [Authorize(AuthenticationSchemes = "Cookies", Roles = "CLIENTE")]
        [HttpGet]
        public async Task<IActionResult> ObtenerPerfilJson()
        {
            int idUsuario = HttpContext.Session.GetInt32("USER_ID") ?? 0;

            // 🔥 CORREGIDO: Ahora devuelve un UnauthorizedResult correcto
            if (idUsuario == 0) return Unauthorized();

            var perfil = await _context.PerfilesClientes
                .FirstOrDefaultAsync(p => p.IdUsuario == idUsuario);

            if (perfil == null) return NotFound();

            return Json(new
            {
                nombreCompleto = perfil.NombreCompleto,
                telefono = perfil.Telefono == "No Usar" || perfil.Telefono == "No especificado" ? "" : perfil.Telefono,
                correo = HttpContext.Session.GetString("USER") ?? ""
            });
        }
    }
}