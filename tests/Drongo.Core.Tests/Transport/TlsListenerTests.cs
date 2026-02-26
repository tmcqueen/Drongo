using Drongo.Core.Transport;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Xunit;

namespace Drongo.Core.Tests.Transport;

public class TlsListenerTests
{
    [Fact]
    public async Task TlsListener_Start_ShouldListenOnPort()
    {
        var cert = TestCertificate.Create();
        var listener = new TlsListener(cert, port: 0);
        
        await listener.StartAsync();
        
        var endpoint = (IPEndPoint)listener.LocalEndpoint;
        Assert.True(endpoint.Port > 0);
        
        listener.Dispose();
    }

    [Fact]
    public async Task TlsListener_Stop_ShouldShutdownGracefully()
    {
        var cert = TestCertificate.Create();
        var listener = new TlsListener(cert, port: 0);
        await listener.StartAsync();
        
        await listener.StopAsync();
        
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await listener.AcceptConnectionAsync()
        );
        
        listener.Dispose();
    }
}

internal static class TestCertificate
{
    public static X509Certificate2 Create()
    {
        var rsa = RSA.Create(2048);
        
        var request = new CertificateRequest(
            "CN=localhost",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1
        );
        
        request.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(false, false, 0, false)
        );
        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(
                X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
                false
            )
        );
        
        var sanBuilder = new SubjectAlternativeNameBuilder();
        sanBuilder.AddDnsName("localhost");
        request.CertificateExtensions.Add(sanBuilder.Build());
        
        var certificate = request.CreateSelfSigned(
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddYears(1)
        );
        
        return new X509Certificate2(
            certificate.Export(X509ContentType.Pfx),
            (string?)null,
            X509KeyStorageFlags.Exportable
        );
    }
}
