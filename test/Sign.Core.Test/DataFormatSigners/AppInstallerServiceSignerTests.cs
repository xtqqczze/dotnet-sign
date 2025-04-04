// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE.txt file in the project root for more information.

using System.Text;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Moq;

namespace Sign.Core.Test
{
    public class AppInstallerServiceSignerTests
    {
        private readonly AppInstallerServiceSigner _signer;

        public AppInstallerServiceSignerTests()
        {
            _signer = new AppInstallerServiceSigner(
                Mock.Of<ICertificateProvider>(),
                Mock.Of<ILogger<IDataFormatSigner>>());
        }

        [Fact]
        public void Constructor_WhenCertificateProviderIsNull_Throws()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
                () => new AppInstallerServiceSigner(
                    certificateProvider: null!,
                    Mock.Of<ILogger<IDataFormatSigner>>()));

            Assert.Equal("certificateProvider", exception.ParamName);
        }

        [Fact]
        public void Constructor_WhenLoggerIsNull_Throws()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
                () => new AppInstallerServiceSigner(
                    Mock.Of<ICertificateProvider>(),
                    logger: null!));

            Assert.Equal("logger", exception.ParamName);
        }

        [Fact]
        public void CanSign_WhenFileIsNull_Throws()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
                () => _signer.CanSign(file: null!));

            Assert.Equal("file", exception.ParamName);
        }

        [Theory]
        [InlineData(".appInstaller")] // Turkish I (U+0049)
        [InlineData(".appinstaller")] // Turkish i (U+0069)
        public void CanSign_WhenFileExtensionMatches_ReturnsTrue(string extension)
        {
            FileInfo file = new($"file{extension}");

            Assert.True(_signer.CanSign(file));
        }

        [Theory]
        [InlineData(".txt")]
        [InlineData(".appİnstaller")] // Turkish İ (U+0130)
        [InlineData(".appınstaller")] // Turkish ı (U+0131)
        public void CanSign_WhenFileExtensionDoesNotMatch_ReturnsFalse(string extension)
        {
            FileInfo file = new($"file{extension}");

            Assert.False(_signer.CanSign(file));
        }

        [Theory]
        [InlineData("http://schemas.microsoft.com/appx/appinstaller/2017", "MainBundle")]
        [InlineData("http://schemas.microsoft.com/appx/appinstaller/2017", "MainPackage")]
        [InlineData("http://schemas.microsoft.com/appx/appinstaller/2017/2", "MainBundle")]
        [InlineData("http://schemas.microsoft.com/appx/appinstaller/2017/2", "MainPackage")]
        [InlineData("http://schemas.microsoft.com/appx/appinstaller/2018", "MainBundle")]
        [InlineData("http://schemas.microsoft.com/appx/appinstaller/2018", "MainPackage")]
        [InlineData("http://schemas.microsoft.com/appx/appinstaller/2021", "MainBundle")]
        [InlineData("http://schemas.microsoft.com/appx/appinstaller/2021", "MainPackage")]
        public void TryGetMainElement_WhenNamespaceAndElementAreKnown_ReturnsElement(string xmlNamespace, string elementName)
        {
            CreateAppInstallerManifest(
                xmlNamespace,
                elementName,
                out XDocument appInstallerManifest,
                out string expectedPublisher);

            Assert.True(AppInstallerServiceSigner.TryGetMainElement(appInstallerManifest, out XElement? mainElement));
            Assert.NotNull(mainElement);

            XAttribute? publisherAttribute = mainElement.Attribute("Publisher");
            Assert.NotNull(publisherAttribute);

            Assert.Equal(expectedPublisher, publisherAttribute.Value);
        }

        [Theory]
        [InlineData("http://schemas.microsoft.com/appx/appinstaller/2024", "MainBundle")]
        [InlineData("http://schemas.microsoft.com/appx/appinstaller/2024", "MainPackage")]
        public void TryGetMainElement_WhenNamespaceAndElementAreUnknown_ReturnsNull(string xmlNamespace, string elementName)
        {
            CreateAppInstallerManifest(
                xmlNamespace,
                elementName,
                out XDocument appInstallerManifest,
                out string _);

            Assert.False(AppInstallerServiceSigner.TryGetMainElement(appInstallerManifest, out XElement? mainElement));
            Assert.Null(mainElement);
        }

        private static void CreateAppInstallerManifest(
            string xmlNamespace,
            string mainElementName,
            out XDocument appInstallerManifest,
            out string expectedPublisher)
        {
            expectedPublisher = "CN=Microsoft Corporation, O=Microsoft Corporation, L=Redmond, S=Washington, C=US";

            string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" + Environment.NewLine +
                $"<AppInstaller xmlns=\"{xmlNamespace}\" Version=\"1.0.0.0\" Uri=\"http://sign.test/app.appinstaller\">" + Environment.NewLine +
                $"  <{mainElementName} Name=\"sign.test\" Publisher=\"{expectedPublisher}\" />" + Environment.NewLine +
                "</AppInstaller>";

            using (MemoryStream stream = new(Encoding.UTF8.GetBytes(xml), writable: false))
            {
                appInstallerManifest = XDocument.Load(stream, LoadOptions.PreserveWhitespace);
            }
        }
    }
}
