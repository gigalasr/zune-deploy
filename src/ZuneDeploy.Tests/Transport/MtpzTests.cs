using System.Security.Cryptography;
using ZuneDeploy.Transport.Mtpz;

namespace ZuneDeploy.Tests.Transport;

public class MtpzTests {
    [Fact]
    public void CertificateMessage() {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (!File.Exists(Path.Combine(dir.FullName, "docs/.mtpz-data"))) {
            dir = dir.Parent!;
        }
        var keys = MtpzKeys.Load(Path.Combine(dir.FullName, "docs/.mtpz-data"));
        var challenge = Enumerable.Range(1, 16).Select(i => (byte)i).ToArray();

        var message = MtpzCrypto.GenerateCertificateMessage(keys, challenge).Message;
        var hash = Convert.ToHexString(SHA256.HashData(message));

        // SHA-256 of the certificate message
        const string expectedHash = "EF02EEFF25B5B910981564CA09DEDF9590FA047CB94554D7EFD6EA90E7942C63";

        Assert.Equal(expectedHash, hash);
    }
}
