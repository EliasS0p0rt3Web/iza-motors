using System.Net;
using System.Net.Mail;

namespace Negocio.Web.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // =========================================
        // 🛠️ MÉTODO GENÉRICO (El que quita el error)
        // =========================================
        public async Task EnviarCorreoGenerico(string destinatario, string asunto, string mensajeHtml)
        {
            var settings = _configuration.GetSection("EmailSettings");

            using var clienteSmtp = new SmtpClient(settings["SmtpHost"], int.Parse(settings["SmtpPort"]))
            {
                Credentials = new NetworkCredential(settings["Remitente"], settings["Password"]),
                EnableSsl = true
            };

            using var mail = new MailMessage
            {
                From = new MailAddress(settings["Remitente"], "Aluminios & Vidrios"),
                Subject = asunto,
                Body = mensajeHtml,
                IsBodyHtml = true
            };

            mail.To.Add(destinatario);

            await clienteSmtp.SendMailAsync(mail);
        }

        // =========================================
        // 📩 MÉTODO ESPECÍFICO (Para reservas nuevas)
        // =========================================
        public async Task EnviarNotificacionReserva(string codigo, string cliente, decimal total, string destinatario)
        {
            var settings = _configuration.GetSection("EmailSettings");

            using var clienteSmtp = new SmtpClient(settings["SmtpHost"], int.Parse(settings["SmtpPort"]))
            {
                Credentials = new NetworkCredential(settings["Remitente"], settings["Password"]),
                EnableSsl = true
            };

            var mensajeHtml = $@"
                <div style='font-family: sans-serif; border: 1px solid #ffc107; padding: 20px; border-radius: 10px;'>
                    <h2 style='color: #ff9800;'>✨ ¡Confirmación de Reserva!</h2>
                    <p>Hola <strong>{cliente}</strong>, gracias por confiar en nuestra vidriería.</p>
                    <p>Tu solicitud ha sido registrada con éxito.</p>
                    <hr style='border: 0; border-top: 1px solid #eee;'/>
                    <p><strong>Código:</strong> {codigo}</p>
                    <p><strong>Monto Total:</strong> S/ {total:N2}</p>
                    <hr style='border: 0; border-top: 1px solid #eee;'/>
                    <p style='font-size: 0.9em;'>Te avisaremos cuando el pedido esté listo.</p>
                </div>";

            using var mail = new MailMessage
            {
                From = new MailAddress(settings["Remitente"], "Aluminios & Vidrios"),
                Subject = $"Confirmación de tu Reserva {codigo}",
                Body = mensajeHtml,
                IsBodyHtml = true
            };

            mail.To.Add(destinatario);

            await clienteSmtp.SendMailAsync(mail);
        }
    }
}