

using System;
using System.Collections.Generic;
using chocolatey.infrastructure.adapters;
using DateTime = chocolatey.infrastructure.adapters.DateTime;
using chocolatey.infrastructure.app.configuration;
using chocolatey.infrastructure.app.domain;
using chocolatey.infrastructure.filesystem;
using chocolatey.infrastructure.results;
using chocolatey.infrastructure.services;
using NuGet.Common;

namespace chocolatey.infrastructure.app.services
{
    internal class NugetListService : IListSourceRunner
    {
        private readonly IFileSystem _fileSystem;
        private readonly ILogger _nugetLogger;
        private readonly IChocolateyPackageInformationService _packageInfoService;
        private readonly IFilesService _filesService;
        private readonly IRuleService _ruleService;
        //private readonly PackageDownloader _packageDownloader;
        private readonly Lazy<IDateTime> _datetime = new Lazy<IDateTime>(() => new DateTime());

        private IDateTime DateTime
        {
            get { return _datetime.Value; }
        }

        public NugetListService(
            IFileSystem fileSystem,
            ILogger nugetLogger,
            IChocolateyPackageInformationService packageInfoService,
            IFilesService filesService,
            IRuleService ruleService)
        {
            _fileSystem = fileSystem;
            _nugetLogger = nugetLogger;
            _packageInfoService = packageInfoService;
            _filesService = filesService;
            _ruleService = ruleService;
        }

        public string SourceType { get => SourceTypes.Normal; }

        public virtual void ListDryRun(ChocolateyConfiguration config)
        {
            throw new System.NotImplementedException();
        }
        
        public IEnumerable<PackageResult> List(ChocolateyConfiguration config)
        {
            throw new System.NotImplementedException();
        }
    }
}