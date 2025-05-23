﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE.txt file in the project root for more information.

using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using Azure.CodeSigning;
using Azure.CodeSigning.Extensions;
using Azure.Core;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sign.Core;
using Sign.SignatureProviders.TrustedSigning;

namespace Sign.Cli
{
    internal sealed class TrustedSigningCommand : Command
    {
        internal Option<Uri> EndpointOption { get; } = new(["--trusted-signing-endpoint", "-tse"], TrustedSigningResources.EndpointOptionDescription);
        internal Option<string> AccountOption { get; } = new(["--trusted-signing-account", "-tsa"], TrustedSigningResources.AccountOptionDescription);
        internal Option<string> CertificateProfileOption { get; } = new(["--trusted-signing-certificate-profile", "-tscp"], TrustedSigningResources.CertificateProfileOptionDescription);
        internal AzureCredentialOptions AzureCredentialOptions { get; } = new();

        internal Argument<List<string>?> FilesArgument { get; } = new("file(s)", Resources.FilesArgumentDescription) { Arity = ArgumentArity.OneOrMore };

        internal TrustedSigningCommand(CodeCommand codeCommand, IServiceProviderFactory serviceProviderFactory)
            : base("trusted-signing", TrustedSigningResources.CommandDescription)
        {
            ArgumentNullException.ThrowIfNull(codeCommand, nameof(codeCommand));
            ArgumentNullException.ThrowIfNull(serviceProviderFactory, nameof(serviceProviderFactory));

            EndpointOption.IsRequired = true;
            AccountOption.IsRequired = true;
            CertificateProfileOption.IsRequired = true;

            AddOption(EndpointOption);
            AddOption(AccountOption);
            AddOption(CertificateProfileOption);
            AzureCredentialOptions.AddOptionsToCommand(this);

            AddArgument(FilesArgument);

            this.SetHandler(async (InvocationContext context) =>
            {
                List<string>? filesArgument = context.ParseResult.GetValueForArgument(FilesArgument);

                if (filesArgument is not { Count: > 0 })
                {
                    context.Console.Error.WriteLine(Resources.MissingFileValue);
                    context.ExitCode = ExitCode.InvalidOptions;
                    return;
                }

                TokenCredential? credential = AzureCredentialOptions.CreateTokenCredential(context);

                if (credential is null)
                {
                    return;
                }

                // Some of the options are required and that is why we can safely use
                // the null-forgiving operator (!) to simplify the code.
                Uri endpointUrl = context.ParseResult.GetValueForOption(EndpointOption)!;
                string accountName = context.ParseResult.GetValueForOption(AccountOption)!;
                string certificateProfileName = context.ParseResult.GetValueForOption(CertificateProfileOption)!;

                serviceProviderFactory.AddServices(services =>
                {
                    services.AddAzureClients(builder =>
                    {
                        builder.AddCertificateProfileClient(endpointUrl);
                        builder.UseCredential(credential);
                        builder.ConfigureDefaults(options => options.Retry.Mode = RetryMode.Exponential);
                    });

                    services.AddSingleton<TrustedSigningService>(serviceProvider =>
                    {
                        return new TrustedSigningService(
                            serviceProvider.GetRequiredService<CertificateProfileClient>(),
                            accountName,
                            certificateProfileName,
                            serviceProvider.GetRequiredService<ILogger<TrustedSigningService>>());
                    });
                });

                TrustedSigningServiceProvider trustedSigningServiceProvider = new();

                await codeCommand.HandleAsync(context, serviceProviderFactory, trustedSigningServiceProvider, filesArgument);
            });
        }
    }
}
