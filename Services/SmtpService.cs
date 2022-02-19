using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using System.Threading.Channels;

namespace ActiveDirectoryManager.Services;

public class SmtpOptions
{
    public string Server { get; set; } = default!;
    public ushort Port { get; set; } = 25;
    public bool Ssl { get; set; } = false;
    public string Username { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string Sender { get; set; } = default!;
    public string Host { get; set; } = default!;
}

public sealed class SmtpService : IDisposable
{
    private readonly SmtpClient _smtp;
    private readonly Channel<(string Sender, string Title, string Content, string[] To)> _queue;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly string _domain;

    public SmtpService(IOptions<SmtpOptions> options)
    {
        _smtp = new SmtpClient
        {
            Host = options.Value.Server,
            Port = options.Value.Port,
            EnableSsl = options.Value.Ssl,
            Credentials = new NetworkCredential(options.Value.Username, options.Value.Password)
        };
        _queue = Channel.CreateUnbounded<(string Sender, string Title, string Content, string[] To)>();
        _cancellationTokenSource = new CancellationTokenSource();
        _domain = options.Value.Host;
        _ = SendEmailQueueAsync(_cancellationTokenSource.Token);
    }

    public async Task SendEmailAsync(string sender, string title, string content, params string[] to)
    {
        await _queue.Writer.WriteAsync((sender, title, content, to));
    }

    private async Task SendEmailQueueAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var email = await _queue.Reader.ReadAsync(cancellationToken);
            using var message = new MailMessage
            {
                Subject = email.Title,
                Body = email.Content.Replace("[|host|]", _domain),
                SubjectEncoding = System.Text.Encoding.UTF8,
                BodyEncoding = System.Text.Encoding.UTF8,
                IsBodyHtml = true,
                From = new MailAddress(email.Sender)
            };
            foreach (var i in email.To) message.To.Add(i);
            await _smtp.SendMailAsync(message);
        }
    }

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        _queue.Writer.Complete();
        _cancellationTokenSource.Dispose();
        _smtp.Dispose();
    }
}
