// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE.txt file in the project root for more information.

using Microsoft.Extensions.FileSystemGlobbing;

namespace Sign.Core
{
    internal sealed class AggregatingSigner : IAggregatingDataFormatSigner
    {
        private readonly IContainerProvider _containerProvider;
        private readonly IDefaultDataFormatSigner _defaultSigner;
        private readonly IFileMetadataService _fileMetadataService;
        private readonly IMatcherFactory _matcherFactory;
        private readonly IEnumerable<IDataFormatSigner> _signers;

        // Dependency injection requires a public constructor.
        public AggregatingSigner(
            IEnumerable<IDataFormatSigner> signers,
            IDefaultDataFormatSigner defaultSigner,
            IContainerProvider containerProvider,
            IFileMetadataService fileMetadataService,
            IMatcherFactory matcherFactory)
        {
            ArgumentNullException.ThrowIfNull(signers, nameof(signers));
            ArgumentNullException.ThrowIfNull(defaultSigner, nameof(defaultSigner));
            ArgumentNullException.ThrowIfNull(containerProvider, nameof(containerProvider));
            ArgumentNullException.ThrowIfNull(fileMetadataService, nameof(fileMetadataService));
            ArgumentNullException.ThrowIfNull(matcherFactory, nameof(matcherFactory));

            _signers = signers;
            _defaultSigner = defaultSigner;
            _containerProvider = containerProvider;
            _fileMetadataService = fileMetadataService;
            _matcherFactory = matcherFactory;
        }

        public bool CanSign(FileInfo file)
        {
            ArgumentNullException.ThrowIfNull(file, nameof(file));

            foreach (IDataFormatSigner signer in _signers)
            {
                if (signer.CanSign(file))
                {
                    return true;
                }
            }

            string extension = file.Extension.ToLowerInvariant();

            return extension switch
            {
                // archives
                ".zip" or ".appxupload" or ".msixupload" => true,
                _ => false
            };
        }

        public async Task SignAsync(IEnumerable<FileInfo> files, SignOptions options)
        {
            ArgumentNullException.ThrowIfNull(files, nameof(files));
            ArgumentNullException.ThrowIfNull(options, nameof(options));

            if (options.RecurseContainers)
            {
                await SignContainerContentsAsync(files, options);
            }

            // split by code sign service and fallback to default

            var grouped = (from signer in _signers
                           from file in files
                           where signer.CanSign(file)
                           group file by signer into groups
                           select groups).ToList();

            // get all files and exclude existing; 

            // This is to catch PE files that don't have the correct extension set
            var defaultFiles = files.Except(grouped.SelectMany(g => g))
                                    .Where(_fileMetadataService.IsPortableExecutable)
                                    .Select(f => new { _defaultSigner.Signer, f })
                                    .GroupBy(a => a.Signer, k => k.f)
                                    .SingleOrDefault(); // one group here

            if (defaultFiles != null)
            {
                grouped.Add(defaultFiles);
            }

            await Task.WhenAll(grouped.Select(g => g.Key.SignAsync(g.ToList(), options)));
        }

        private async Task SignContainerContentsAsync(IEnumerable<FileInfo> files, SignOptions options)
        {
            // See if any of them are archives
            List<FileInfo> archives = (from file in files
                                       where _containerProvider.IsZipContainer(file) || _containerProvider.IsNuGetContainer(file)
                                       select file).ToList();

            // expand the archives and sign recursively first
            List<IContainer> containers = new();

            try
            {
                foreach (FileInfo archive in archives)
                {
                    IContainer container = _containerProvider.GetContainer(archive)!;

                    await container.OpenAsync();

                    containers.Add(container);
                }

                // See if there's any files in the expanded zip that we need to sign
                List<FileInfo> allFiles = containers
                    .SelectMany(container => GetFiles(container, options))
                    .ToList();

                if (allFiles.Count > 0)
                {
                    // Send the files from the archives through the aggregator to sign
                    await SignAsync(allFiles, options);

                    // After signing the contents, save the zip
                    // For NuPkg, this step removes the signature too, but that's ok as it'll get signed below
                    await Parallel.ForEachAsync(containers, (container, cancellationToken) => container.SaveAsync());
                }
            }
            finally
            {
                containers.ForEach(tz => tz.Dispose());
                containers.Clear();
            }

            // See if there's any appx's in here, process them recursively first to sign the inner files
            List<FileInfo> appxs = (from file in files
                                    where _containerProvider.IsAppxContainer(file)
                                    select file).ToList();

            // See if there's any appxbundles here, process them recursively first
            // expand the archives and sign recursively first
            // This will also update the publisher information to get it ready for signing
            try
            {
                foreach (FileInfo appx in appxs)
                {
                    IContainer container = _containerProvider.GetContainer(appx)!;

                    await container.OpenAsync();

                    containers.Add(container);
                }

                // See if there's any files in the expanded zip that we need to sign
                List<FileInfo> allFiles = containers
                    .SelectMany(container => GetFiles(container, options))
                    .ToList();

                if (allFiles.Count > 0)
                {
                    // Send the files from the archives through the aggregator to sign
                    await SignAsync(allFiles, options);
                }

                // Save the appx with the updated publisher info
                await Parallel.ForEachAsync(containers, (container, cancellationToken) => container.SaveAsync());
            }
            finally
            {
                containers.ForEach(tz => tz.Dispose());
                containers.Clear();
            }

            List<FileInfo> bundles = (from file in files
                                      where _containerProvider.IsAppxBundleContainer(file)
                                      select file).ToList();

            try
            {
                foreach (FileInfo bundle in bundles)
                {
                    IContainer container = _containerProvider.GetContainer(bundle)!;

                    await container.OpenAsync();

                    containers.Add(container);
                }

                Matcher appxBundleFileMatcher = _matcherFactory.Create();

                appxBundleFileMatcher.AddInclude("**/*.appx");
                appxBundleFileMatcher.AddInclude("**/*.msix");

                // See if there's any files in the expanded zip that we need to sign
                List<FileInfo> allFiles = containers.SelectMany(tz => tz.GetFiles(appxBundleFileMatcher)).ToList();

                if (allFiles.Count > 0)
                {
                    // Send the files from the archives through the aggregator to sign
                    await SignAsync(allFiles, options);

                    // After signing the contents, save the zip
                    await Parallel.ForEachAsync(containers, (container, cancellationToken) => container.SaveAsync());
                }
            }
            finally
            {
                containers.ForEach(tz => tz.Dispose());
                containers.Clear();
            }
        }


        public void CopySigningDependencies(FileInfo file, DirectoryInfo destination, SignOptions options)
        {
            // pass the handling for this down to the actual implementations
            foreach (IDataFormatSigner signer in _signers)
            {
                if (signer.CanSign(file))
                {
                    signer.CopySigningDependencies(file, destination, options);
                }
            }
        }

        private static IEnumerable<FileInfo> GetFiles(IContainer container, SignOptions options)
        {
            IEnumerable<FileInfo> files;

            if (options.Matcher is null)
            {
                // If not filtered, default to all
                files = container.GetFiles();
            }
            else
            {
                files = container.GetFiles(options.Matcher);
            }

            if (options.AntiMatcher is not null)
            {
                IEnumerable<FileInfo> antiFiles = container.GetFiles(options.AntiMatcher);

                files = files.Except(antiFiles, FileInfoComparer.Instance).ToList();
            }

            return files;
        }
    }
}
