using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CrimsonStainedLands;
using CrimsonStainedLands.Connections;
using Org.BouncyCastle.Tls;

public class ConnectionManager
{
    private class CertSettings
    {
        public string PrivateKeyPassword { get; set; } = "localhost";
        public string CertificatePath { get; set; } = @"I:\certs";
        public string DomainName { get; set; } = "kbs-cloud.com";

        public string CertChainName { get; set; } = "-chain.pem";
        public string CertKeyName { get; set; } = "-key.pem";

    }
    public delegate void ConnectionConnected(BaseConnection connection, string username, string password);

    public ConcurrentList<BaseConnection> Connections {get;set;}

    private CancellationTokenSource cancellationTokenSource;

    public void OnConnectionConnected(BaseConnection connection, string username, string password)
    {
        Connections.Add(connection);
        connection.Player = new Player(Game.Instance, connection, username, password);
    }

    public ConnectionManager()
    {
        Connections = new ConcurrentList<BaseConnection>();
        cancellationTokenSource = new CancellationTokenSource();
    }

    private X509Certificate2 LoadCertificate()
    {
        X509Certificate2 certificate = null;
        try
        {
            //certificate = new X509Certificate2(Settings.X509CertificatePath, Settings.X509CertificatePassword);
            var settings_path = Settings.CertificateSettingsJsonPath;
            var settings = new CertSettings();
            if (File.Exists(settings_path))
            {
                JsonDocument jsonDocument = JsonDocument.Parse(File.ReadAllText(settings_path));
                if (jsonDocument.Deserialize(typeof(CertSettings)) is CertSettings loadsettings)
                {
                    settings = loadsettings;
                }

            }
            else
            {
                var json = JsonSerializer.Serialize<CertSettings>(settings, new JsonSerializerOptions(JsonSerializerDefaults.General));
                File.WriteAllText(settings_path, json);
            }

            string chainpath = Path.Join(settings.CertificatePath, settings.DomainName + settings.CertChainName);

            string keypath = Path.Join(settings.CertificatePath, settings.DomainName + settings.CertKeyName);
            string password = settings.PrivateKeyPassword;

            try
            {
                X509Certificate2 cert;
                cert = CrimsonStainedLands.CertificateChainLoader.LoadCertificateWithChain(chainpath, keypath, password);
                certificate = cert;
            }
            catch(Exception ex)
            {
                Game.log(ex.Message);
            }
            if (certificate != null)
            {
                Console.WriteLine($"Certificate subject: {certificate.Subject}");
                Console.WriteLine($"Has private key: {certificate.HasPrivateKey}");
            }
        }
        catch (Exception ex)
        {
            Game.bug(ex.Message);
        }
        return certificate;
    }

    public async Task RunAsync()
    {
        var cert = LoadCertificate();
        var tcpServer = new TCPServer(this, "0.0.0.0", Settings.Port, cancellationTokenSource);
        var sslServer = new SslServer(this, "0.0.0.0", Settings.SSLPort, cert, cancellationTokenSource);
        var webServer = new WebServer(this, "0.0.0.0", Settings.SSLPort + 2, cert, cancellationTokenSource);
        var sshServer = new SSHServer(this, "0.0.0.0", Settings.SSLPort + 1, cancellationTokenSource);

        var services = new List<Task>() {
            tcpServer.Start(OnConnectionConnected),
            sslServer.Start(OnConnectionConnected),
            webServer.Start(OnConnectionConnected),
            sshServer.Start(OnConnectionConnected),
        };

        try
        {
            await Task.WhenAll(services);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"One of the services threw an exception: {ex.Message}");
        }

        Console.WriteLine("All services have completed.");
    }

}