

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
        private readonly NugetServiceCommon _common;

        public NugetListService(NugetServiceCommon common)
        {
            _common = common;
        }

        public string SourceType { get => _common.SourceType; }

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