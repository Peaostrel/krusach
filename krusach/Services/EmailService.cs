using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using System.IO;

namespace krusach.Services
{
    public class EmailService
    {
        private static string _smtpServer = ConfigHelper.GetEmailSettingString("SmtpServer", "smtp.gmail.com");
        private static int _smtpPort = ConfigHelper.GetEmailSettingInt("SmtpPort", 587);
        private static string _senderEmail = ConfigHelper.GetEmailSettingString("SenderEmail", "your-email@gmail.com"); 
        private static string _appPassword = ConfigHelper.GetEmailSettingString("AppPassword", ""); 

        // Метод для вызова из MainWindow (с полной кастомизацией)
        public static void SendInvoice(string recipientEmail, string subject, string body, string attachmentPath = null)
        {
            if (string.IsNullOrEmpty(_appPassword))
                throw new Exception("Пароль приложения для SMTP не настроен в appsettings.json");

            try
            {
                using (var message = new MailMessage(_senderEmail, recipientEmail))
                {
                    message.Subject = subject;
                    message.Body = body;

                    if (!string.IsNullOrEmpty(attachmentPath) && File.Exists(attachmentPath))
                    {
                        message.Attachments.Add(new Attachment(attachmentPath));
                    }

                    using (var client = new SmtpClient(_smtpServer, _smtpPort))
                    {
                        client.EnableSsl = true;
                        client.Credentials = new NetworkCredential(_senderEmail, _appPassword);
                        client.Send(message);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка почтового сервиса: {ex.Message}");
            }
        }

        // Асинхронный метод для триггеров
        public async Task SendInvoiceAsync(string recipientEmail, int orderId)
        {
            await Task.Run(() => 
            {
                SendInvoice(
                    recipientEmail, 
                    $"Счет по заказу №{orderId} - Опт Торг", 
                    $"Здравствуйте!\nВаш заказ №{orderId} успешно обработан.\n\nС уважением, Администрация."
                );
            });
        }
    }
}
