using System.Security.Cryptography.X509Certificates;

namespace Drongo.Core.Transport;

public class TlsOptions
{
    public X509Certificate2? Certificate { get; set; }
    public string? CertificatePath { get; set; }
    public string? CertificatePassword { get; set; }
    public bool ClientCertificateRequired { get; set; }
    public Dictionary<string, X509Certificate2> SniCertificates { get; set; } = new();
}

public class TlsListenerFactory
{
    private readonly TlsOptions _options;

    public TlsListenerFactory(TlsOptions options)
    {
        _options = options;
    }

    public ITlsListenerService Create(int port)
    {
        var certificate = _options.Certificate;
        
        if (certificate is null && !string.IsNullOrEmpty(_options.CertificatePath))
        {
            certificate = new X509Certificate2(
                _options.CertificatePath,
                _options.CertificatePassword
            );
        }

        if (certificate is null)
        {
            throw new InvalidOperationException("TLS certificate not configured");
        }

        return new TlsListener(certificate, port, _options.ClientCertificateRequired, _options.SniCertificates);
    }
}
