// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE.txt file in the project root for more information.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using Moq;
using Sign.TestInfrastructure;

namespace Sign.Core.Test
{
    public sealed class ClickOnceSignerTests : IDisposable
    {
        private readonly DirectoryService _directoryService;
        private readonly ClickOnceSigner _signer;

        public ClickOnceSignerTests()
        {
            _directoryService = new(Mock.Of<ILogger<IDirectoryService>>());
            _signer = new ClickOnceSigner(
                Mock.Of<ISignatureAlgorithmProvider>(),
                Mock.Of<ICertificateProvider>(),
                Mock.Of<IServiceProvider>(),
                Mock.Of<IMageCli>(),
                Mock.Of<IManifestSigner>(),
                Mock.Of<ILogger<IDataFormatSigner>>(),
                Mock.Of<IFileMatcher>());
        }

        public void Dispose()
        {
            _directoryService.Dispose();
        }

        [Fact]
        public void Constructor_WhenSignatureAlgorithmProviderIsNull_Throws()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
                () => new ClickOnceSigner(
                    signatureAlgorithmProvider: null!,
                    Mock.Of<ICertificateProvider>(),
                    Mock.Of<IServiceProvider>(),
                    Mock.Of<IMageCli>(),
                    Mock.Of<IManifestSigner>(),
                    Mock.Of<ILogger<IDataFormatSigner>>(),
                    Mock.Of<IFileMatcher>()));

            Assert.Equal("signatureAlgorithmProvider", exception.ParamName);
        }

        [Fact]
        public void Constructor_WhenCertificateProviderIsNull_Throws()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
                () => new ClickOnceSigner(
                    Mock.Of<ISignatureAlgorithmProvider>(),
                    certificateProvider: null!,
                    Mock.Of<IServiceProvider>(),
                    Mock.Of<IMageCli>(),
                    Mock.Of<IManifestSigner>(),
                    Mock.Of<ILogger<IDataFormatSigner>>(),
                    Mock.Of<IFileMatcher>()));

            Assert.Equal("certificateProvider", exception.ParamName);
        }

        [Fact]
        public void Constructor_WhenServiceProviderIsNull_Throws()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
                () => new ClickOnceSigner(
                    Mock.Of<ISignatureAlgorithmProvider>(),
                    Mock.Of<ICertificateProvider>(),
                    serviceProvider: null!,
                    Mock.Of<IMageCli>(),
                    Mock.Of<IManifestSigner>(),
                    Mock.Of<ILogger<IDataFormatSigner>>(),
                    Mock.Of<IFileMatcher>()));

            Assert.Equal("serviceProvider", exception.ParamName);
        }

        [Fact]
        public void Constructor_WhenMageCliIsNull_Throws()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
                () => new ClickOnceSigner(
                    Mock.Of<ISignatureAlgorithmProvider>(),
                    Mock.Of<ICertificateProvider>(),
                    Mock.Of<IServiceProvider>(),
                    mageCli: null!,
                    Mock.Of<IManifestSigner>(),
                    Mock.Of<ILogger<IDataFormatSigner>>(),
                    Mock.Of<IFileMatcher>()));

            Assert.Equal("mageCli", exception.ParamName);
        }

        [Fact]
        public void Constructor_WhenManifestSignerIsNull_Throws()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
                () => new ClickOnceSigner(
                    Mock.Of<ISignatureAlgorithmProvider>(),
                    Mock.Of<ICertificateProvider>(),
                    Mock.Of<IServiceProvider>(),
                    Mock.Of<IMageCli>(),
                    manifestSigner: null!,
                    Mock.Of<ILogger<IDataFormatSigner>>(),
                    Mock.Of<IFileMatcher>()));

            Assert.Equal("manifestSigner", exception.ParamName);
        }

        [Fact]
        public void Constructor_WhenLoggerIsNull_Throws()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
                () => new ClickOnceSigner(
                    Mock.Of<ISignatureAlgorithmProvider>(),
                    Mock.Of<ICertificateProvider>(),
                    Mock.Of<IServiceProvider>(),
                    Mock.Of<IMageCli>(),
                    Mock.Of<IManifestSigner>(),
                    logger: null!,
                    Mock.Of<IFileMatcher>()));

            Assert.Equal("logger", exception.ParamName);
        }

        [Fact]
        public void Constructor_WhenFileMatcherIsNull_Throws()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
                () => new ClickOnceSigner(
                    Mock.Of<ISignatureAlgorithmProvider>(),
                    Mock.Of<ICertificateProvider>(),
                    Mock.Of<IServiceProvider>(),
                    Mock.Of<IMageCli>(),
                    Mock.Of<IManifestSigner>(),
                    Mock.Of<ILogger<IDataFormatSigner>>(),
                    fileMatcher: null!));

            Assert.Equal("fileMatcher", exception.ParamName);
        }

        [Fact]
        public void CanSign_WhenFileIsNull_Throws()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
                () => _signer.CanSign(file: null!));

            Assert.Equal("file", exception.ParamName);
        }

        [Theory]
        [InlineData(".application")]
        [InlineData(".APPLICATION")] // test case insensitivity
        [InlineData(".vsto")]
        public void CanSign_WhenFileExtensionMatches_ReturnsTrue(string extension)
        {
            FileInfo file = new($"file{extension}");

            Assert.True(_signer.CanSign(file));
        }

        [Theory]
        [InlineData(".txt")]
        [InlineData(".applİcation")] // Turkish İ (U+0130)
        [InlineData(".applıcation")] // Turkish ı (U+0131)
        public void CanSign_WhenFileExtensionDoesNotMatch_ReturnsFalse(string extension)
        {
            FileInfo file = new($"file{extension}");

            Assert.False(_signer.CanSign(file));
        }

        [Fact]
        public async Task SignAsync_WhenFilesIsNull_Throws()
        {
            ArgumentNullException exception = await Assert.ThrowsAsync<ArgumentNullException>(
                () => _signer.SignAsync(
                    files: null!,
                    new SignOptions(HashAlgorithmName.SHA256, new Uri("http://timestamp.test"))));

            Assert.Equal("files", exception.ParamName);
        }

        [Fact]
        public async Task SignAsync_WhenOptionsIsNull_Throws()
        {
            ArgumentNullException exception = await Assert.ThrowsAsync<ArgumentNullException>(
                () => _signer.SignAsync(
                    Enumerable.Empty<FileInfo>(),
                    options: null!));

            Assert.Equal("options", exception.ParamName);
        }

        [Fact]
        public async Task SignAsync_WhenSigningFails_Throws()
        {
            using (TemporaryDirectory temporaryDirectory = new(_directoryService))
            {
                FileInfo clickOnceFile = new(
                    Path.Combine(
                        temporaryDirectory.Directory.FullName,
                        $"{Path.GetRandomFileName()}.clickonce"));

                ContainerSpy containerSpy = new(clickOnceFile);

                FileInfo applicationFile = AddFile(
                    containerSpy,
                    temporaryDirectory.Directory,
                    string.Empty,
                    "MyApp.application");

                SignOptions options = new(
                    "ApplicationName",
                    "PublisherName",
                    "Description",
                    new Uri("https://description.test"),
                    HashAlgorithmName.SHA256,
                    HashAlgorithmName.SHA256,
                    new Uri("http://timestamp.test"),
                    matcher: null,
                    antiMatcher: null,
                    recurseContainers: true);

                using (X509Certificate2 certificate = SelfIssuedCertificateCreator.CreateCertificate())
                using (RSA privateKey = certificate.GetRSAPrivateKey()!)
                {
                    Mock<ISignatureAlgorithmProvider> signatureAlgorithmProvider = new();
                    Mock<ICertificateProvider> certificateProvider = new();

                    certificateProvider.Setup(x => x.GetCertificateAsync(It.IsAny<CancellationToken>()))
                        .ReturnsAsync(certificate);

                    signatureAlgorithmProvider.Setup(x => x.GetRsaAsync(It.IsAny<CancellationToken>()))
                        .ReturnsAsync(privateKey);

                    Mock<IServiceProvider> serviceProvider = new();
                    AggregatingSignerSpy aggregatingSignerSpy = new();

                    serviceProvider.Setup(x => x.GetService(It.IsAny<Type>()))
                        .Returns(aggregatingSignerSpy);

                    Mock<IMageCli> mageCli = new();

                    mageCli.Setup(x => x.RunAsync(
                            It.IsAny<string>()))
                        .ReturnsAsync(1);

                    Mock<IManifestSigner> manifestSigner = new();
                    Mock<IFileMatcher> fileMatcher = new();
                    Mock<ILogger<IDataFormatSigner>> logger = new();

                    manifestSigner.Setup(
                        x => x.Sign(
                            It.Is<FileInfo>(fi => fi.Name == applicationFile.Name),
                            It.Is<X509Certificate2>(c => ReferenceEquals(certificate, c)),
                            It.Is<RSA>(rsa => ReferenceEquals(privateKey, rsa)),
                            It.Is<SignOptions>(o => ReferenceEquals(options, o))));

                    ClickOnceSigner signer = new(
                        signatureAlgorithmProvider.Object,
                        certificateProvider.Object,
                        serviceProvider.Object,
                        mageCli.Object,
                        manifestSigner.Object,
                        logger.Object,
                        fileMatcher.Object);

                    signer.Retry = TimeSpan.FromMicroseconds(1);

                    await Assert.ThrowsAsync<SigningException>(() => signer.SignAsync(new[] { applicationFile }, options));
                }
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("PublisherName")]
        public async Task SignAsync_WhenFilesIsClickOnceFile_Signs(string? publisherName)
        {
            const string commonName = "Test certificate (DO NOT TRUST)";

            using (TemporaryDirectory temporaryDirectory = new(_directoryService))
            {
                FileInfo clickOnceFile = new(
                    Path.Combine(
                        temporaryDirectory.Directory.FullName,
                        $"{Path.GetRandomFileName()}.clickonce"));

                ContainerSpy containerSpy = new(clickOnceFile);

                FileInfo applicationFile = AddFile(
                    containerSpy,
                    temporaryDirectory.Directory,
                    string.Empty,
                    "MyApp.application");
                FileInfo dllDeployFile = AddFile(
                    containerSpy,
                    temporaryDirectory.Directory,
                    string.Empty,
                    "MyApp_1_0_0_0", "MyApp.dll.deploy");
                // This is an incomplete manifest --- just enough to satisfy SignAsync(...)'s requirements.
                FileInfo manifestFile = AddFile(
                    containerSpy,
                    temporaryDirectory.Directory,
                    @$"<?xml version=""1.0"" encoding=""utf-8""?>
<asmv1:assembly xsi:schemaLocation=""urn:schemas-microsoft-com:asm.v1 assembly.adaptive.xsd"" manifestVersion=""1.0"" xmlns:asmv1=""urn:schemas-microsoft-com:asm.v1"" xmlns=""urn:schemas-microsoft-com:asm.v2"" xmlns:asmv2=""urn:schemas-microsoft-com:asm.v2"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:co.v1=""urn:schemas-microsoft-com:clickonce.v1"" xmlns:asmv3=""urn:schemas-microsoft-com:asm.v3"" xmlns:dsig=""http://www.w3.org/2000/09/xmldsig#"" xmlns:co.v2=""urn:schemas-microsoft-com:clickonce.v2"">
  <publisherIdentity name=""CN={commonName}, O=unit.test"" />
</asmv1:assembly>",
                    "MyApp_1_0_0_0", "MyApp.dll.manifest");
                FileInfo exeDeployFile = AddFile(
                    containerSpy,
                    temporaryDirectory.Directory,
                    string.Empty,
                    "MyApp_1_0_0_0", "MyApp.exe.deploy");
                FileInfo jsonDeployFile = AddFile(
                    containerSpy,
                    temporaryDirectory.Directory,
                    string.Empty,
                    "MyApp_1_0_0_0", "MyApp.json.deploy");

                SignOptions options = new(
                    "ApplicationName",
                    publisherName,
                    "Description",
                    new Uri("https://description.test"),
                    HashAlgorithmName.SHA256,
                    HashAlgorithmName.SHA256,
                    new Uri("http://timestamp.test"),
                    matcher: null,
                    antiMatcher: null,
                    recurseContainers: true);

                using (X509Certificate2 certificate = SelfIssuedCertificateCreator.CreateCertificate())
                using (RSA privateKey = certificate.GetRSAPrivateKey()!)
                {
                    Mock<ISignatureAlgorithmProvider> signatureAlgorithmProvider = new();
                    Mock<ICertificateProvider> certificateProvider = new();

                    certificateProvider.Setup(x => x.GetCertificateAsync(It.IsAny<CancellationToken>()))
                        .ReturnsAsync(certificate);

                    signatureAlgorithmProvider.Setup(x => x.GetRsaAsync(It.IsAny<CancellationToken>()))
                        .ReturnsAsync(privateKey);

                    Mock<IServiceProvider> serviceProvider = new();
                    AggregatingSignerSpy aggregatingSignerSpy = new();

                    serviceProvider.Setup(x => x.GetService(It.IsAny<Type>()))
                        .Returns(aggregatingSignerSpy);

                    Mock<IMageCli> mageCli = new();
                    string expectedArgs = $"-update \"{manifestFile.FullName}\" -a sha256RSA -n \"{options.ApplicationName}\"";
                    mageCli.Setup(x => x.RunAsync(
                            It.Is<string>(args => string.Equals(expectedArgs, args, StringComparison.Ordinal))))
                        .ReturnsAsync(0);

                    string publisher;

                    if (string.IsNullOrEmpty(options.PublisherName))
                    {
                        publisher = certificate.SubjectName.Name;
                    }
                    else
                    {
                        publisher = options.PublisherName;
                    }

                    expectedArgs = $"-update \"{applicationFile.FullName}\" -a sha256RSA -n \"{options.ApplicationName}\" -pub \"{publisher}\" -appm \"{manifestFile.FullName}\" -SupportURL https://description.test/";
                    mageCli.Setup(x => x.RunAsync(
                            It.Is<string>(args => string.Equals(expectedArgs, args, StringComparison.Ordinal))))
                        .ReturnsAsync(0);

                    Mock<IManifestSigner> manifestSigner = new();
                    Mock<IFileMatcher> fileMatcher = new();

                    manifestSigner.Setup(
                        x => x.Sign(
                            It.Is<FileInfo>(fi => fi.Name == manifestFile.Name),
                            It.Is<X509Certificate2>(c => ReferenceEquals(certificate, c)),
                            It.Is<RSA>(rsa => ReferenceEquals(privateKey, rsa)),
                            It.Is<SignOptions>(o => ReferenceEquals(options, o))));

                    manifestSigner.Setup(
                        x => x.Sign(
                            It.Is<FileInfo>(fi => fi.Name == applicationFile.Name),
                            It.Is<X509Certificate2>(c => ReferenceEquals(certificate, c)),
                            It.Is<RSA>(rsa => ReferenceEquals(privateKey, rsa)),
                            It.Is<SignOptions>(o => ReferenceEquals(options, o))));

                    ILogger<IDataFormatSigner> logger = Mock.Of<ILogger<IDataFormatSigner>>();
                    ClickOnceSigner signer = new(
                        signatureAlgorithmProvider.Object,
                        certificateProvider.Object,
                        serviceProvider.Object,
                        mageCli.Object,
                        manifestSigner.Object,
                        logger,
                        fileMatcher.Object);

                    await signer.SignAsync(new[] { applicationFile }, options);

                    // Verify that files have been renamed back.
                    foreach (FileInfo file in containerSpy.Files)
                    {
                        file.Refresh();

                        Assert.True(file.Exists);
                    }

                    Assert.Equal(3, aggregatingSignerSpy.FilesSubmittedForSigning.Count);
                    Assert.Collection(
                        aggregatingSignerSpy.FilesSubmittedForSigning,
                        file => Assert.Equal(
                            Path.Combine(dllDeployFile.DirectoryName!, Path.GetFileNameWithoutExtension(dllDeployFile.Name)),
                            file.FullName),
                        file => Assert.Equal(
                            Path.Combine(exeDeployFile.DirectoryName!, Path.GetFileNameWithoutExtension(exeDeployFile.Name)),
                            file.FullName),
                        file => Assert.Equal(
                            Path.Combine(jsonDeployFile.DirectoryName!, Path.GetFileNameWithoutExtension(jsonDeployFile.Name)),
                            file.FullName));

                    mageCli.VerifyAll();
                    manifestSigner.VerifyAll();
                }
            }
        }

        [Fact]
        public async Task SignAsync_WhenFilesIsClickOnceFileWithoutContent_Signs()
        {
            using (TemporaryDirectory temporaryDirectory = new(_directoryService))
            {
                FileInfo clickOnceFile = new(
                    Path.Combine(
                        temporaryDirectory.Directory.FullName,
                        $"{Path.GetRandomFileName()}.clickonce"));

                ContainerSpy containerSpy = new(clickOnceFile);

                FileInfo applicationFile = AddFile(
                    containerSpy,
                    temporaryDirectory.Directory,
                    string.Empty,
                    "MyApp.application");

                SignOptions options = new(
                    "ApplicationName",
                    "PublisherName",
                    "Description",
                    new Uri("https://description.test"),
                    HashAlgorithmName.SHA256,
                    HashAlgorithmName.SHA256,
                    new Uri("http://timestamp.test"),
                    matcher: null,
                    antiMatcher: null,
                    recurseContainers: true);

                using (X509Certificate2 certificate = SelfIssuedCertificateCreator.CreateCertificate())
                using (RSA privateKey = certificate.GetRSAPrivateKey()!)
                {
                    Mock<ISignatureAlgorithmProvider> signatureAlgorithmProvider = new();
                    Mock<ICertificateProvider> certificateProvider = new();

                    certificateProvider.Setup(x => x.GetCertificateAsync(It.IsAny<CancellationToken>()))
                        .ReturnsAsync(certificate);

                    signatureAlgorithmProvider.Setup(x => x.GetRsaAsync(It.IsAny<CancellationToken>()))
                        .ReturnsAsync(privateKey);

                    Mock<IServiceProvider> serviceProvider = new();
                    AggregatingSignerSpy aggregatingSignerSpy = new();

                    serviceProvider.Setup(x => x.GetService(It.IsAny<Type>()))
                        .Returns(aggregatingSignerSpy);

                    Mock<IMageCli> mageCli = new();

                    string publisher;

                    if (string.IsNullOrEmpty(options.PublisherName))
                    {
                        publisher = certificate.SubjectName.Name;
                    }
                    else
                    {
                        publisher = options.PublisherName;
                    }

                    string expectedArgs = $"-update \"{applicationFile.FullName}\" -a sha256RSA -n \"{options.ApplicationName}\" -pub \"{publisher}\" -SupportURL https://description.test/";
                    mageCli.Setup(x => x.RunAsync(
                            It.Is<string>(args => string.Equals(expectedArgs, args, StringComparison.Ordinal))))
                        .ReturnsAsync(0);

                    Mock<IManifestSigner> manifestSigner = new();
                    Mock<IFileMatcher> fileMatcher = new();

                    manifestSigner.Setup(
                        x => x.Sign(
                            It.Is<FileInfo>(fi => fi.Name == applicationFile.Name),
                            It.Is<X509Certificate2>(c => ReferenceEquals(certificate, c)),
                            It.Is<RSA>(rsa => ReferenceEquals(privateKey, rsa)),
                            It.Is<SignOptions>(o => ReferenceEquals(options, o))));

                    ILogger<IDataFormatSigner> logger = Mock.Of<ILogger<IDataFormatSigner>>();
                    ClickOnceSigner signer = new(
                        signatureAlgorithmProvider.Object,
                        certificateProvider.Object,
                        serviceProvider.Object,
                        mageCli.Object,
                        manifestSigner.Object,
                        logger,
                        fileMatcher.Object);

                    await signer.SignAsync(new[] { applicationFile }, options);

                    // Verify that files have been renamed back.
                    foreach (FileInfo file in containerSpy.Files)
                    {
                        file.Refresh();

                        Assert.True(file.Exists);
                    }

                    Assert.Empty(aggregatingSignerSpy.FilesSubmittedForSigning);

                    mageCli.VerifyAll();
                    manifestSigner.VerifyAll();
                }
            }
        }

        [Fact]
        public void CopySigningDependencies_CopiesCorrectFiles()
        {
            using (TemporaryDirectory temporaryDirectory = new(_directoryService))
            {
                FileInfo clickOnceFile = new(
                    Path.Combine(
                        temporaryDirectory.Directory.FullName,
                        $"{Path.GetRandomFileName()}.clickonce"));

                ContainerSpy containerSpy = new(clickOnceFile);

                FileInfo applicationFile = AddFile(
                    containerSpy,
                    temporaryDirectory.Directory,
                    string.Empty,
                    "MyApp.application");
                FileInfo dllDeployFile = AddFile(
                    containerSpy,
                    temporaryDirectory.Directory,
                    string.Empty,
                    "MyApp_1_0_0_0", "MyApp.dll.deploy");

                using (X509Certificate2 certificate = SelfIssuedCertificateCreator.CreateCertificate())
                using (RSA privateKey = certificate.GetRSAPrivateKey()!)
                {
                    Mock<ISignatureAlgorithmProvider> signatureAlgorithmProvider = new();
                    Mock<ICertificateProvider> certificateProvider = new();

                    certificateProvider.Setup(x => x.GetCertificateAsync(It.IsAny<CancellationToken>()))
                        .ReturnsAsync(certificate);

                    signatureAlgorithmProvider.Setup(x => x.GetRsaAsync(It.IsAny<CancellationToken>()))
                        .ReturnsAsync(privateKey);

                    Mock<IServiceProvider> serviceProvider = new();
                    AggregatingSignerSpy aggregatingSignerSpy = new();

                    serviceProvider.Setup(x => x.GetService(It.IsAny<Type>()))
                        .Returns(aggregatingSignerSpy);

                    Mock<IMageCli> mageCli = new();
                    string publisher = certificate.SubjectName.Name;

                    Mock<IManifestSigner> manifestSigner = new();
                    Mock<IFileMatcher> fileMatcher = new();

                    SignOptions options = new(
                        "ApplicationName",
                        "PublisherName",
                        "Description",
                        new Uri("https://description.test"),
                        HashAlgorithmName.SHA256,
                        HashAlgorithmName.SHA256,
                        new Uri("http://timestamp.test"),
                        matcher: null,
                        antiMatcher: null,
                        recurseContainers: true
                    );

                    manifestSigner.Setup(
                        x => x.Sign(
                            It.Is<FileInfo>(fi => fi.Name == applicationFile.Name),
                            It.Is<X509Certificate2>(c => ReferenceEquals(certificate, c)),
                            It.Is<RSA>(rsa => ReferenceEquals(privateKey, rsa)),
                            It.Is<SignOptions>(o => ReferenceEquals(options, o))));

                    ILogger<IDataFormatSigner> logger = Mock.Of<ILogger<IDataFormatSigner>>();
                    ClickOnceSigner signer = new(
                        signatureAlgorithmProvider.Object,
                        certificateProvider.Object,
                        serviceProvider.Object,
                        mageCli.Object,
                        manifestSigner.Object,
                        logger,
                        fileMatcher.Object);

                    using (TemporaryDirectory signingDirectory = new(_directoryService))
                    {
                        // ensure that we start with nothing
                        Assert.Empty(signingDirectory.Directory.EnumerateFiles());
                        Assert.Empty(signingDirectory.Directory.EnumerateDirectories());
                        // tell the provider to copy what it needs into the signing directory
                        signer.CopySigningDependencies(applicationFile, signingDirectory.Directory, options);
                        // and make sure we got it. We expect only the DLL to be copied, and NOT the .application file itself.
                        IEnumerable<FileInfo> copiedFiles = signingDirectory.Directory.EnumerateFiles("*", SearchOption.AllDirectories);
                        IEnumerable<DirectoryInfo> copiedDirectories = signingDirectory.Directory.EnumerateDirectories();
                        Assert.Single(copiedFiles);
                        Assert.Single(copiedDirectories);
                        Assert.Contains(copiedDirectories, d => d.Name == "MyApp_1_0_0_0");
                        Assert.Contains(copiedFiles, f => f.Name == "MyApp.dll.deploy");
                    }
                }
            }
        }

        private static FileInfo AddFile(
            ContainerSpy containerSpy,
            DirectoryInfo directory,
            string fileContent,
            params string[] fileParts)
        {
            string[] parts = new[] { directory.FullName }.Concat(fileParts).ToArray();
            FileInfo file = new(Path.Combine(parts));

            // The file needs to exist because it will be renamed.
            file.Directory!.Create();
            File.WriteAllText(file.FullName, fileContent);

            containerSpy.Files.Add(file);

            return file;
        }
    }
}
