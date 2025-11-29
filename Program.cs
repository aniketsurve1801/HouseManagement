using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;
    using System.Net;
using System.Net.Mail;


// If the process was started with --send-once we will attempt a single send and exit.
if (args is not null && args.Contains("--send-once"))
{
    // Allow callers to pass a custom message via the SEND_MESSAGE environment variable
    // falling back to the previous default text if it's not set.
    var envMsg = Environment.GetEnvironmentVariable("SEND_MESSAGE");
    var messageToSend = !string.IsNullOrWhiteSpace(envMsg) ? envMsg : "aniket you are great";

    Console.WriteLine($"RUN-ONCE: invoking sendemail (dryRun=false) with message: {messageToSend} ...");
    var sendResult = EchoTool.sendemail(messageToSend, dryRun: false);
    Console.WriteLine("RUN-ONCE result: " + sendResult);
    return;
}

var builderMCP = Host.CreateApplicationBuilder(args);
builderMCP.Logging.AddConsole(consoleLogOptions =>
{
    // Configure all logs to go to stderr
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});

builderMCP.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builderMCP.Build().RunAsync();



var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();


[McpServerToolType]
public static class EchoTool
{
    [McpServerTool]
    public static string Echo(string message)
    {
        Console.WriteLine($"ECHO TOOL CALLED with: {message}");
        return $"Hello from C#: {message}";
    }

    [McpServerTool]
    public static string StringLength(string message)
    {
     
        Console.WriteLine($"ECHO TOOL CALLED with: {message}");
        return $"Length is : {message.Length}";
    }

    [McpServerTool]
    public static string sendemail(string message, bool dryRun = true)
    {
        // Log the incoming message so calls are visible in the console
        Console.WriteLine($"SENDMAIL TOOL CALLED with: {message} (dryRun={dryRun})");

        // Dry-run is the default to avoid accidental real sends.
        if (dryRun)
        {
            var result = $"DRY RUN: sendemail received message: {message}";
            Console.WriteLine(result);
            return result;
        }

        try
        {
            EmailService emailService = new EmailService();
            var serviceResult = emailService.SendEmailAsync(message).GetAwaiter().GetResult();
            Console.WriteLine(serviceResult);
            return serviceResult;
        }
        catch (Exception ex)
        {
            var err = $"Failed to send email: {ex.Message}";
            Console.WriteLine(err);
            return err;
        }
    }



public class EmailService
{
    /// <summary>
    /// Sends an email using SMTP configuration from environment variables.
    /// Returns a status string describing success or the error encountered.
    /// Environment variables read (defaults shown):
    /// - SMTP_HOST (smtp.gmail.com)
    /// - SMTP_PORT (587)
    /// - SMTP_USER (your_email@gmail.com)
    /// - SMTP_PASS (your-app-password)
    /// - EMAIL_FROM (defaults to SMTP_USER)
    /// - EMAIL_TO (aniket1801@gmail.com)
    /// - EMAIL_SUBJECT (Hello Aniket from .NET)
    /// - EMAIL_ENABLE_SSL (true)
    /// </summary>
    public async Task<string> SendEmailAsync(string message)
    {
        var host = Environment.GetEnvironmentVariable("SMTP_HOST") ?? "smtp.gmail.com";
        var portValue = Environment.GetEnvironmentVariable("SMTP_PORT");
        var user = Environment.GetEnvironmentVariable("SMTP_USER") ?? "aniketsurve.testing@gmail.com";
        var pass = Environment.GetEnvironmentVariable("SMTP_PASS") ?? "7715883708@n!ket";
        var fromAddr = Environment.GetEnvironmentVariable("EMAIL_FROM") ?? user ?? "aniket1801@gmail.com";
        var toAddr = Environment.GetEnvironmentVariable("EMAIL_TO") ?? "aniket1801@gmail.com";
        var subject = Environment.GetEnvironmentVariable("EMAIL_SUBJECT") ?? "Hello Aniket from .NET";
        var enableSslValue = Environment.GetEnvironmentVariable("EMAIL_ENABLE_SSL") ?? "true";

        if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass))
            return "SMTP credentials are missing — set SMTP_USER and SMTP_PASS environment variables";

        if (string.IsNullOrWhiteSpace(toAddr))
            return "Recipient (EMAIL_TO) is not configured";

        int port = 587;
        if (!string.IsNullOrWhiteSpace(portValue) && int.TryParse(portValue, out var p)) port = p;

        bool enableSsl = true;
        if (!string.IsNullOrWhiteSpace(enableSslValue) && bool.TryParse(enableSslValue, out var ssl)) enableSsl = ssl;

        try
        {
            var mail = new MailMessage();
            mail.From = new MailAddress(fromAddr);

            foreach (var recipient in toAddr.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                mail.To.Add(recipient.Trim());
            }

            mail.Subject = subject;
            mail.Body = message;

            using var smtp = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(user, pass),
                EnableSsl = enableSsl
            };

            await smtp.SendMailAsync(mail);
            return "Email sent successfully.";
        }
        catch (Exception ex)
        {
            return $"Failed to send email: {ex.Message}";
        }
    }
}




    [McpServerTool]
    public static string ReverseEcho(string message)
    {
        Console.WriteLine($"REVERSE ECHO TOOL CALLED with: {message}");
        return new string(message.Reverse().ToArray());
    }
}



// [McpServerToolType]
// public static class EchoTool
// {
//     [McpServerTool, Description("Echoes the message back to the Aniket.")]
//     public static string Echo(string message) => $"Hello from C#: {message}";

//     [McpServerTool, Description("Echoes in reverse the message sent by the Aniket.")]
//     public static string ReverseEcho(string message) => new string(message.Reverse().ToArray());
// }
