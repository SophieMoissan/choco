using System;
using chocolatey.infrastructure.adapters;
using DateTime = chocolatey.infrastructure.adapters.DateTime;
using chocolatey.infrastructure.filesystem;
using chocolatey.infrastructure.services;
using NuGet.Common;
using chocolatey.infrastructure.app.domain;


namespace chocolatey.infrastructure.app.services
{
    // Contains shared elements between various Nuget SourceRunner services
    internal class NugetServiceCommon
    {
        public IFileSystem FileSystem { get; }
        internal ILogger NugetLogger { get; }
        internal IChocolateyPackageInformationService PackageInfoService { get; }
        internal IFilesService FilesService { get; }
        internal IRuleService RuleService { get; }

        private readonly Lazy<IDateTime> _datetime = new Lazy<IDateTime>(() => new DateTime());
        internal IDateTime DateTime { get => _datetime.Value; }

        public NugetServiceCommon(
            IFileSystem fileSystem,
            ILogger nugetLogger,
            IChocolateyPackageInformationService packageInfoService,
            IFilesService filesService,
            IRuleService ruleService)
        {
            FileSystem = fileSystem;
            NugetLogger = nugetLogger;
            PackageInfoService = packageInfoService;
            FilesService = filesService;
            RuleService = ruleService;
        }

        public string SourceType { get => SourceTypes.Normal; }
    }
}