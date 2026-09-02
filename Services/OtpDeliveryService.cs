using System.Net;
using System.Net.Mail;

namespace AmarShowsBook.Services;

public record OtpDeliveryResult(bool Success, bool IsConfigured, string Message);

public class OtpDeliveryService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<OtpDeliveryService> _logger;

    public OtpDeliveryService(IConfiguration configuration, ILogger<OtpDeliveryService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<OtpDeliveryResult> SendEmailOtpAsync(string email, string otp, string purpose)
    {
        var host = _configuration["Otp:Email:SmtpHost"] ?? "smtp.gmail.com";
        var port = _configuration.GetValue("Otp:Email:SmtpPort", 587);
        var from = _configuration["Otp:Email:From"];
        var password = _configuration["Otp:Email:Password"];
        var fromName = _configuration["Otp:Email:FromName"] ?? "showTime";
        var enableSsl = _configuration.GetValue("Otp:Email:EnableSsl", true);

        if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(password))
        {
            return new OtpDeliveryResult(false, false, "Email OTP is not configured.");
        }

        try
        {
            using var smtp = new SmtpClient(host)
            {
                Port = port,
                Credentials = new NetworkCredential(from, password),
                EnableSsl = enableSsl
            };

            using var mail = new MailMessage
            {
                From = new MailAddress(from, fromName),
                Subject = $"showTime OTP for {purpose}",
                Body = $"Your showTime OTP for {purpose} is {otp}.\n\nIt is valid for 5 minutes. Do not share this code with anyone.",
                IsBodyHtml = false
            };

            mail.To.Add(email);
            await smtp.SendMailAsync(mail);

            return new OtpDeliveryResult(true, true, "OTP sent to email.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Email OTP send failed for {Email}", email);
            return new OtpDeliveryResult(false, true, "Unable to send email OTP.");
        }
    }

    public async Task<OtpDeliveryResult> SendEmailAsync(string email, string subject, string body)
    {
        var host = _configuration["Otp:Email:SmtpHost"] ?? "smtp.gmail.com";
        var port = _configuration.GetValue("Otp:Email:SmtpPort", 587);
        var from = _configuration["Otp:Email:From"];
        var password = _configuration["Otp:Email:Password"];
        var fromName = _configuration["Otp:Email:FromName"] ?? "showTime";
        var enableSsl = _configuration.GetValue("Otp:Email:EnableSsl", true);

        if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(password))
        {
            return new OtpDeliveryResult(false, false, "Email is not configured.");
        }

        try
        {
            using var smtp = new SmtpClient(host)
            {
                Port = port,
                Credentials = new NetworkCredential(from, password),
                EnableSsl = enableSsl
            };

            using var mail = new MailMessage
            {
                From = new MailAddress(from, fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };

            mail.To.Add(email);
            await smtp.SendMailAsync(mail);

            return new OtpDeliveryResult(true, true, "Email sent.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Email send failed for {Email}", email);
            return new OtpDeliveryResult(false, true, "Unable to send email.");
        }
    }

    public async Task<OtpDeliveryResult> SendMobileOtpAsync(string mobile, string otp, string purpose)
    {
        var apiKey = _configuration["Otp:Sms:Fast2SmsApiKey"];
        var route = _configuration["Otp:Sms:Route"] ?? "otp";

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new OtpDeliveryResult(false, false, "Mobile OTP is not configured.");
        }

        var url = route.Equals("message", StringComparison.OrdinalIgnoreCase)
            ? BuildFast2SmsMessageUrl(apiKey, mobile, otp, purpose)
            : BuildFast2SmsOtpUrl(apiKey, mobile, otp);

        try
        {
            using var client = new HttpClient();
            using var response = await client.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                return new OtpDeliveryResult(true, true, "OTP sent to mobile.");
            }

            _logger.LogWarning("Mobile OTP send failed for {Mobile}. Status: {Status}", mobile, response.StatusCode);
            return new OtpDeliveryResult(false, true, "Unable to send mobile OTP.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Mobile OTP send failed for {Mobile}", mobile);
            return new OtpDeliveryResult(false, true, "Unable to send mobile OTP.");
        }
    }

    private static string BuildFast2SmsOtpUrl(string apiKey, string mobile, string otp)
    {
        return "https://www.fast2sms.com/dev/bulkV2" +
            $"?authorization={Uri.EscapeDataString(apiKey)}" +
            "&route=otp" +
            $"&variables_values={Uri.EscapeDataString(otp)}" +
            "&flash=0" +
            $"&numbers={Uri.EscapeDataString(mobile)}";
    }

    private static string BuildFast2SmsMessageUrl(string apiKey, string mobile, string otp, string purpose)
    {
        var message = $"Your showTime OTP for {purpose} is {otp}. Valid for 5 minutes. Do not share it.";

        return "https://www.fast2sms.com/dev/bulkV2" +
            $"?authorization={Uri.EscapeDataString(apiKey)}" +
            "&route=q" +
            $"&message={Uri.EscapeDataString(message)}" +
            "&flash=0" +
            $"&numbers={Uri.EscapeDataString(mobile)}";
    }
}
