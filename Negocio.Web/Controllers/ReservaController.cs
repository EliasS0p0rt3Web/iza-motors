using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Negocio.Web.Data;
using Negocio.Web.Models.Entities;
using System.Text.Json;
using Negocio.Web.Models.Dtos;
using Negocio.Web.Services;
namespace Negocio.Web.Controllers
{
    public class ReservaController : Controller
    {
        private readonly NegocioDbContext _context;
        private readonly EmailService _emailService;

        public ReservaController(NegocioDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;   
        }

        // =========================================
        // 🔹 CREAR RESERVA (desde cotización)
        // =========================================
        // =========================================
        // 🔹 CREAR RESERVA (desde cotización) - ACTUALIZADO
        // =========================================
        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] CrearReservaDto dto)
        {
            if (dto == null) return BadRequest();

            // Validaciones básicas de negocio
            if (string.IsNullOrWhiteSpace(dto.NombreCliente))
                return BadRequest("Nombre requerido");

            if ((dto.Total ?? 0) <= 0)
                return BadRequest("El total debe ser mayor a cero");

            // 🔥 DETECTAR SESIÓN: Intentamos obtener el ID del usuario si está logueado
            int? idUsuarioSesion = HttpContext.Session.GetInt32("USER_ID");

            var reserva = new Reserva
            {
                CodigoReserva = GenerarCodigo(),
                NombreCliente = dto.NombreCliente,
                Telefono = dto.Telefono,
                Correo = dto.Correo,
                DetalleJson = dto.DetalleJson,
                Total = dto.Total ?? 0,
                Adelanto = dto.Adelanto ?? 0,
                TipoEntrega = dto.TipoEntrega,
                DireccionEntrega = dto.DireccionEntrega,
                FechaSolicitada = dto.FechaSolicitada,
                FechaRegistro = DateTime.Now,
                Estado = "PENDIENTE",

                // 🔥 ASIGNACIÓN INTELIGENTE: 
                // Si el DTO trae un IdUsuario lo usa, si no, usa el de la sesión. 
                // Si ninguno existe, se guarda como NULL (Incógnito).
                IdUsuario = dto.IdUsuario ?? idUsuarioSesion
            };

            _context.Reservas.Add(reserva);
            await _context.SaveChangesAsync();

            // =================================================================
            // 🔄 AUTOMATIZACIÓN: GUARDADO DE DIRECCIÓN EN PERFIL (ASOCIADO A DIRECCIONCLIENTE)
            // =================================================================
            if (reserva.IdUsuario.HasValue && !string.IsNullOrEmpty(reserva.DireccionEntrega))
            {
                try
                {
                    string entregaUpper = reserva.TipoEntrega?.ToUpper() ?? "";

                    // 1️⃣ Solo procesamos si es un tipo de entrega logística válida
                    if (entregaUpper == "DELIVERY" || entregaUpper == "SHALOM")
                    {
                        // Determinamos el TipoUbicación según tu regla de negocio de la entidad
                        string tipoUbicacionFinal = entregaUpper == "SHALOM" ? "SHALOM" : "DOMICILIO";

                        // 2️⃣ Buscamos en tu DbSet si el cliente ya tiene guardada esa misma dirección exacta
                        // Cambia "_context.DireccionesClientes" por el nombre exacto de tu DbSet en el NegocioDbContext si varía
                        bool existeDireccion = await _context.DireccionesClientes
                            .AnyAsync(d => d.IdUsuario == reserva.IdUsuario.Value &&
                                           d.TipoUbicacion == tipoUbicacionFinal &&
                                           reserva.DireccionEntrega.Contains(d.DireccionTexto));

                        if (!existeDireccion)
                        {
                            var nuevaDireccion = new DireccionCliente
                            {
                                IdUsuario = reserva.IdUsuario.Value,
                                TipoUbicacion = tipoUbicacionFinal,
                                Activo = true
                            };

                            if (entregaUpper == "DELIVERY")
                            {
                                // Formato del formulario: "DOMICILIO: Distrito - Calle (Ref: Referencia)"
                                string limpia = reserva.DireccionEntrega.Replace("DOMICILIO: ", "");
                                var partes = limpia.Split(" - ");

                                nuevaDireccion.Distrito = partes.Length > 0 ? partes[0].Trim() : "SJL";

                                if (partes.Length > 1)
                                {
                                    var calleYRef = partes[1].Split(" (Ref: ");
                                    nuevaDireccion.DireccionTexto = calleYRef[0].Trim();

                                    if (calleYRef.Length > 1)
                                    {
                                        nuevaDireccion.Referencia = calleYRef[1].Replace(")", "").Trim();
                                    }
                                }
                                else
                                {
                                    nuevaDireccion.DireccionTexto = limpia;
                                }
                            }
                            else if (entregaUpper == "SHALOM")
                            {
                                // Formato del formulario: "AGENCIA SHALOM: Provincia | RECOGE: Nombre"
                                string limpiaShalom = reserva.DireccionEntrega.Replace("AGENCIA SHALOM: ", "");
                                var partesShalom = limpiaShalom.Split(" | RECOGE: ");

                                nuevaDireccion.Distrito = partesShalom.Length > 0 ? partesShalom[0].Trim() : "Agencia";
                                nuevaDireccion.DireccionTexto = partesShalom.Length > 0 ? partesShalom[0].Trim() : limpiaShalom;
                                nuevaDireccion.DatosReceptorAgencia = partesShalom.Length > 1 ? partesShalom[1].Trim() : dto.NombreCliente;
                                nuevaDireccion.Referencia = "Recojo en Agencia";
                            }

                            // 3️⃣ Agregamos al DbSet correspondiente de tu contexto
                            _context.DireccionesClientes.Add(nuevaDireccion);
                            await _context.SaveChangesAsync();
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Aviso en el Auto-guardado de dirección: " + ex.Message);
                }
            }

            // =========================================
            // 📩 SISTEMA DE NOTIFICACIONES DIFERENCIADO
            // =========================================
            try
            {
                // 1️⃣ NOTIFICACIÓN PARA TI (ADMIN)
                string asuntoAdmin = $"⚠️ NUEVA VENTA: {reserva.CodigoReserva}";
                string mensajeAdminHtml = $@"
<div style='font-family: sans-serif; background-color: #f8f9fa; padding: 20px; border-left: 5px solid #007bff;'>
    <h2 style='color: #007bff;'>¡Hola Elias!</h2>
    <p>Tienes una <strong>nueva reserva</strong> que debes revisar en el panel.</p>
    <hr/>
    <p><strong>Cliente:</strong> {reserva.NombreCliente}</p>
    <p><strong>Total:</strong> S/ {reserva.Total:N2}</p>
    <p><strong>Teléfono:</strong> {reserva.Telefono}</p>
    <br/>
    <a href='https://negocio-aluminio-bugyewagdtgne6dy.canadacentral-01.azurewebsites.net/Login' 
       style='background-color: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>
       Ir al Panel de Control
    </a>
</div>";

                await _emailService.EnviarCorreoGenerico("canchanyasullcaelias@gmail.com", asuntoAdmin, mensajeAdminHtml);

                // 2️⃣ NOTIFICACIÓN PARA EL CLIENTE (Solo si dejó correo)
                if (!string.IsNullOrEmpty(reserva.Correo) && reserva.Correo != "canchanyasullcaelias@gmail.com")
                {
                    string asuntoCliente = $"✨ Confirmación de tu Reserva {reserva.CodigoReserva}";
                    string mensajeClienteHtml = $@"
    <div style='font-family: sans-serif; border: 1px solid #ffc107; padding: 20px; border-radius: 10px;'>
        <h2 style='color: #ff9800;'>¡Gracias por tu reserva!</h2>
        <p>Hola <strong>{reserva.NombreCliente}</strong>, hemos recibido tu reserva con éxito.</p>
        <p>Estaremos en contacto contigo para cualquier consulta o actualización sobre tu reserva.</p>
        <hr/>
        <p><strong>Código de Seguimiento:</strong> {reserva.CodigoReserva}</p>
        <p><strong>Monto Total:</strong> S/ {reserva.Total:N2}</p>
    </div>";

                    await _emailService.EnviarCorreoGenerico(reserva.Correo, asuntoCliente, mensajeClienteHtml);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error en Notificaciones: " + ex.Message);
            }

            return RedirectToAction("Confirmacion", new { codigo = reserva.CodigoReserva });
        }

        // =========================================
        // 🔹 CONFIRMACIÓN
        // =========================================
        public IActionResult Confirmacion(string codigo)
        {
            var reserva = _context.Reservas
                .FirstOrDefault(r => r.CodigoReserva == codigo);

            if (reserva == null)
                return NotFound();

            return View(reserva);
        }

        // =========================================
        // 🔹 FORM CONSULTA
        // =========================================
        public IActionResult Consultar()
        {
            return View();
        }

        // =========================================
        // 🔹 BUSCAR POR CÓDIGO
        // =========================================
        [HttpPost]
        public IActionResult Buscar(string codigo)
        {
            var reserva = _context.Reservas
                .FirstOrDefault(r => r.CodigoReserva == codigo);

            if (reserva == null)
            {
                ViewBag.Error = "No se encontró la reserva.";
                return View("Consultar");
            }

            return View("Detalle", reserva);
        }

        // =========================================
        // 🔹 CANCELAR RESERVA
        // =========================================
        [HttpPost]
        public async Task<IActionResult> Cancelar(string codigo)
        {
            var reserva = await _context.Reservas
                .FirstOrDefaultAsync(r => r.CodigoReserva == codigo);

            if (reserva == null) return NotFound();

            // Regla de negocio: No cancelar si ya está avanzado
            if (reserva.Estado == "EN_PRODUCCION" || reserva.Estado == "LISTO" || reserva.Estado == "ENTREGADO")
            {
                TempData["Error"] = "Esta reserva ya no puede cancelarse.";
                return RedirectToAction("Confirmacion", new { codigo });
            }

            reserva.Estado = "CANCELADO";
            await _context.SaveChangesAsync();

            // 📩 NOTIFICACIÓN DE CANCELACIÓN DIFERENCIADA
            try
            {
                string asunto = $"Cancelación de Reserva {reserva.CodigoReserva}";

                // 🔥 1. LO QUE TE LLEGA A TI (ADMIN)
                string mensajeAdmin = $"<p>Tu reserva <strong>{reserva.CodigoReserva}</strong> FUE CANCELADA EXITOSAMENTE.</p>";
                await _emailService.EnviarCorreoGenerico("canchanyasullcaelias@gmail.com", asunto, mensajeAdmin);

                // 📩 2. LO QUE LE LLEGA AL CLIENTE (Si dejó correo)
                if (!string.IsNullOrEmpty(reserva.Correo))
                {
                    string mensajeCliente = $"<p>Hola <strong>{reserva.NombreCliente}</strong>, tu reserva <strong>{reserva.CodigoReserva}</strong> ha sido cancelada correctamente.</p>";
                    await _emailService.EnviarCorreoGenerico(reserva.Correo, "Tu pedido ha sido cancelado", mensajeCliente);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error email: " + ex.Message);
            }

            TempData["Success"] = "Reserva cancelada correctamente.";

            // 🔥 Detectar si está logueado para no sacarlo de su panel
            if (HttpContext.Session.GetInt32("USER_ID") != null)
            {
                return RedirectToAction("VerPedido", "Cliente", new { id = reserva.ReservaId });
            }
            return RedirectToAction("Confirmacion", new { codigo });
        }

        // =========================================
        // 🔹 PANEL ADMIN RESERVAS
        // =========================================
        public IActionResult Admin(string estado, string tipoEntrega)
        {
            var rol = HttpContext.Session.GetString("ROL");

            if (rol != "ADMINISTRADOR" && rol != "JEFE")
                return RedirectToAction("Index", "Login");

            var query = _context.Reservas.AsQueryable();

            if (!string.IsNullOrEmpty(estado))
                query = query.Where(r => r.Estado == estado);

            if (!string.IsNullOrEmpty(tipoEntrega))
                query = query.Where(r => r.TipoEntrega == tipoEntrega);

            var reservas = query
                .OrderByDescending(r => r.FechaRegistro)
                .ToList();

            return View(reservas);
        }
        // =========================================
        // 🔹 GENERAR CÓDIGO PROFESIONAL
        // =========================================
        private string GenerarCodigo()
        {
            var ultimo = _context.Reservas
                .OrderByDescending(r => r.ReservaId)
                .FirstOrDefault();

            int numero = ultimo == null ? 1 : ultimo.ReservaId + 1;

            return $"RES-{DateTime.Now.Year}-{numero:D4}";
        }

        // =========================================
        // 🔹 CAMBIAR ESTADO (ULTRA BLINDADO)
        // =========================================
        [HttpPost]
        public async Task<IActionResult> CambiarEstado(string codigo, string nuevoEstado)
        {
            var reserva = await _context.Reservas
                .FirstOrDefaultAsync(r => r.CodigoReserva == codigo);

            if (reserva == null) return NotFound();

            // 🔒 ESCUDO 1: Si el pedido ya está CANCELADO o ENTREGADO, nadie lo vuelve a tocar
            if (reserva.Estado == "CANCELADO" || reserva.Estado == "ENTREGADO")
            {
                TempData["Error"] = $"No se puede modificar una reserva que ya está en estado {reserva.Estado}.";
                return RedirectToAction("AdminDetalle", new { id = reserva.ReservaId });
            }

            // Definimos la jerarquía estricta del flujo de negocio
            var estadosFlujo = new List<string> { "PENDIENTE", "CONFIRMADO", "EN_PRODUCCION", "LISTO", "ENTREGADO" };

            int indiceActual = estadosFlujo.IndexOf(reserva.Estado);
            int indiceNuevo = estadosFlujo.IndexOf(nuevoEstado);

            // 🔒 ESCUDO 2: Candado Unidireccional (Evita retroceder estados por manipulación)
            if (nuevoEstado != "CANCELADO" && indiceNuevo < indiceActual)
            {
                TempData["Error"] = "Operación inválida: No se puede retroceder a un estado ya superado.";
                return RedirectToAction("AdminDetalle", new { id = reserva.ReservaId });
            }

            // Si todo está correcto, actualizamos el estado en la base de datos
            reserva.Estado = nuevoEstado;
            await _context.SaveChangesAsync();

            // 📩 SISTEMA DE NOTIFICACIONES POR CORREO
            try
            {
                // 1. ✅ SI EL ADMIN LO MARCA COMO "LISTO"
                if (nuevoEstado == "LISTO" && !string.IsNullOrEmpty(reserva.Correo))
                {
                    string asunto = "🚀 ¡Tu pedido de vidriería está listo!";
                    string mensajeHtml = $@"
                <div style='font-family: sans-serif; border: 2px solid #28a745; padding: 20px; border-radius: 10px;'>
                    <h2 style='color: #28a745;'>✅ ¡Todo listo, {reserva.NombreCliente}!</h2>
                    <p>Te informamos que tu pedido <strong>{codigo}</strong> ya ha sido terminado.</p>
                    <p>Puedes pasar por nuestra tienda para recogerlo en el horario habitual o estar atento al delivery programado.</p>
                    <hr style='border: 0; border-top: 1px solid #eee;'/>
                    <p style='color: gray; font-size: 0.8em;'>Gracias por confiar en E&E Aluminios.</p>
                </div>";
                    await _emailService.EnviarCorreoGenerico(reserva.Correo, asunto, mensajeHtml);
                }

                // 2. ❌ SI EL ADMIN LO MARCA COMO "CANCELADO"
                else if (nuevoEstado == "CANCELADO" && !string.IsNullOrEmpty(reserva.Correo))
                {
                    string asunto = $"Actualización de Reserva {reserva.CodigoReserva}";
                    string mensajeCliente = $@"
                <div style='font-family: sans-serif; border: 1px solid #dc3545; padding: 20px; border-radius: 10px;'>
                    <h2 style='color: #dc3545;'>Reserva Cancelada</h2>
                    <p>Hola <strong>{reserva.NombreCliente}</strong>,</p>
                    <p>Te informamos que tu reserva <strong>{reserva.CodigoReserva}</strong> ha sido cancelada por el administrador.</p>
                    <p>Si consideras que esto es un error o requieres la devolución de un depósito bajo nuestras políticas comerciales, contáctanos de inmediato por WhatsApp.</p>
                </div>";

                    await _emailService.EnviarCorreoGenerico(reserva.Correo, asunto, mensajeCliente);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error al avisar cambio de estado: " + ex.Message);
            }

            TempData["Success"] = $"Estado actualizado correctamente a {nuevoEstado}.";
            return RedirectToAction("AdminDetalle", new { id = reserva.ReservaId });
        }

        public IActionResult AdminDetalle(int id)
        {
            var rol = HttpContext.Session.GetString("ROL");

            if (rol != "ADMINISTRADOR" && rol != "JEFE")
                return RedirectToAction("Index", "Login");

            var reserva = _context.Reservas
                .FirstOrDefault(r => r.ReservaId == id);

            if (reserva == null)
                return NotFound();

            return View(reserva);
        }

        // =========================================
        // 💰 REGISTRAR ADELANTO (CORREGIDO - NO PONE ENTREGADO AUTOMÁTICO)
        // =========================================
        [HttpPost]
        public async Task<IActionResult> RegistrarAdelanto(string codigo, decimal monto)
        {
            var reserva = await _context.Reservas
                .FirstOrDefaultAsync(r => r.CodigoReserva == codigo);

            if (reserva == null) return NotFound();

            // 🔒 ESCUDO 1: Si ya está cancelado o entregado, no se le mete plata
            if (reserva.Estado == "CANCELADO" || reserva.Estado == "ENTREGADO")
            {
                TempData["Error"] = "No se pueden registrar pagos en órdenes canceladas o entregadas.";
                return RedirectToAction("AdminDetalle", new { id = reserva.ReservaId });
            }

            if (monto <= 0) return BadRequest("Monto inválido.");

            var saldoActual = reserva.Total - reserva.Adelanto;
            if (monto > saldoActual) return BadRequest("El monto excede el saldo pendiente.");

            // Sumamos el dinero ingresado a la columna Adelanto
            reserva.Adelanto += monto;

            // 🔥 DISPARADOR INTELIGENTE CORREGIDO:
            // Si el pedido estaba en "PENDIENTE", sin importar si pagó la mitad o el 100% completo, 
            // el estado evoluciona estrictamente a "CONFIRMADO". El estado "ENTREGADO" solo se maneja manualmente.
            if (reserva.Estado == "PENDIENTE")
            {
                reserva.Estado = "CONFIRMADO";
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Pago de S/ {monto:N2} registrado con éxito. Estado actual: {reserva.Estado}.";
            return RedirectToAction("AdminDetalle", new { id = reserva.ReservaId });
        }
    }
}
