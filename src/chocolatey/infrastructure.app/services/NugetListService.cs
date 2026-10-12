

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
using System.IO;

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

        // Original dry-run impl. The refactor includes support for dry-running
        // what List actually does, but we're not changing behaviour as part of
        // a refactor. Should be an easy switch-over if the decision to change
        // behaviour is made. 
        public virtual void ListDryRun(ChocolateyConfiguration config)
        {
            this.Log().Info("{0} would have searched for '{1}' against the following source(s) :\"{2}\"".FormatWith(
                ApplicationParameters.Name,
                config.Input,
                config.Sources
            ));
        }
        
        public virtual IEnumerable<PackageResult> List(ChocolateyConfiguration config)
        {
            RunList(config, false);
            throw new System.NotImplementedException();
        }

        private IEnumerable<PackageResult> RunList(ChocolateyConfiguration config, bool isDry)
        {
            uint count = 0;
            bool isConfigChanged = false;
            if(!TryPrepareConfig(config, ref isConfigChanged))
            {
                // TODO: do we want to print why no packages are listed?
                yield break;
            }

            // We are now in a state where PrepareConfig has run successfully,
            // which means we need to undo that when we exit.
            try
            {
                
            }
            finally
            {
                RestoreConfig(config, isConfigChanged);
            }
            
        }


        private bool TryPrepareConfig(ChocolateyConfiguration config, ref bool isConfigChanged)
        {
            if (config.ListCommand.LocalOnly)
            {
                if (!_common.FileSystem.DirectoryExists(ApplicationParameters.PackagesLocation))
                {
                    return false;
                }

                config.CreateBackup();
                isConfigChanged = true;

                config.Sources = ApplicationParameters.PackagesLocation;
                config.Prerelease = true;
                config.ListCommand.IncludeVersionOverrides = true;
            }
            return true;
        }

        private void RestoreConfig(ChocolateyConfiguration config, bool isConfigChanged)
        {
            // config was only backed up if changed
            if(isConfigChanged)
            {
                config.RevertChanges(true);
            }
        }

    }
}