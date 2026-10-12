using System;
using System.Collections.Generic;
using System.Linq;
using chocolatey.infrastructure.app.configuration;
using chocolatey.infrastructure.app.domain;
using chocolatey.infrastructure.app.nuget;
using chocolatey.infrastructure.app.utility;
using chocolatey.infrastructure.logging;
using chocolatey.infrastructure.results;
using NuGet.Protocol.Core.Types;

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
            this.Log().Info(
                "{0} would have searched for '{1}' against the following source(s) :\"{2}\""
                .FormatWith(ApplicationParameters.Name, config.Input, config.Sources)
            );
        }
        
        public virtual IEnumerable<PackageResult> List(ChocolateyConfiguration config)
        {
            RunList(config, false);
            throw new System.NotImplementedException();
        }

        private IEnumerable<PackageResult> RunList(ChocolateyConfiguration config, bool isDry)
        {
            uint count = 0;
            var decryptionFailures = new List<ChocolateyPackageInformation>();

            bool isConfigChanged = false;
            if(!TryPrepareConfig(config, ref isConfigChanged))
            {
                yield break;
            }
            try // yield, finally: RestoreConfig
            {            
                if (config.RegularOutput)
                {
                    ShowPreamble(config);
                }

                // Don't print yet: wait till we know we're going to show at least one package.
                // Set to false once header is printed.
                bool shouldPrintHeader = (!config.QuietOutput && !config.RegularOutput && config.IncludeHeaders);
                if (isDry)
                {
                    this.Log().Info (
                        "Dry run: {0} would have searched for '{1}' against the following source(s) :\"{2}\""
                        .FormatWith (ApplicationParameters.Name, config.Input, config.Sources)
                    );
                }
                else 
                {
                    foreach (var pkg in NugetList.GetPackages(config, _common.NugetLogger, _common.FileSystem))
                    {
                        ChocolateyPackageMetadata packageLocalMetadata;
                        var listingData = new PackageListingExtraData();
                                
                        if (pkg.PackagePath != null && !string.IsNullOrWhiteSpace(pkg.PackagePath))
                        {
                            packageLocalMetadata = new ChocolateyPackageMetadata(pkg.PackagePath, _common.FileSystem);
                            if (config.ListCommand.LocalOnly)
                            {
                                listingData.GatherLocalOnlyData(_common, config, pkg, packageLocalMetadata, decryptionFailures);
                            }
                        }
                        else
                        {
                            packageLocalMetadata = null;
                        }
                        // TODO: show header
                        // TODO: show package
                        // TODO: yield return
                    }
                }
                // TODO: Postamble 
            }
            finally
            {
                RestoreConfig(config, isConfigChanged);
            }
            // TODO: show threshold warnings
            // TODO: show decryption failure warnings
        }

        

        private bool TryPrepareConfig(ChocolateyConfiguration config, ref bool isConfigChanged)
        {
            if (config.ListCommand.LocalOnly)
            {
                if (!_common.FileSystem.DirectoryExists(ApplicationParameters.PackagesLocation))
                {
                    // TODO: do we want to print why no packages are listed?
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

        private void ShowPreamble(ChocolateyConfiguration config)
        {
            if (config.ListCommand.ApprovedOnly || config.ListCommand.DownloadCacheAvailable || config.ListCommand.NotBroken)
            {
                // This warning has been added here, to provide context to the user, and a follow up issue
                // has been added here: https://github.com/chocolatey/choco/issues/3139 to address the actual
                // issue that causes this warning to be required.
                this.Log().Warn(@"
Starting with Chocolatey CLI v2.0.0, changes have been made to the
`choco search` command which means that filtering of packages using the
`--approved-only`, `--download-cache`, and `--not-broken` options are
now performed within Chocolatey CLI. Previously, this filtering would
have been performed on the Chocolatey Community Repository. As a result,
it is possible that incomplete package lists are returned from a command
that uses these options.");
            }

            this.Log().Debug(() => "Running list with the following filter = '{0}'".FormatWith(config.Input));
            this.Log().Debug(ChocolateyLoggers.Verbose, () => "--- Start of List ---");
        }

        private class PackageListingExtraData
        {
            public bool ShouldSkip { get; private set; } = false;
            public string PackageInstallLocation { get; private set; } = null;
            public string Deploymentlocation { get; private set; } = null;
            public string SourceInstalledFrom { get; private set; } = null;
            public string PackageArgumentsUnencrypted { get; private set; } = null;

            public void GatherLocalOnlyData(
                NugetServiceCommon common,
                ChocolateyConfiguration config,
                IPackageSearchMetadata package,
                ChocolateyPackageMetadata packageLocalMetadata,
                List<ChocolateyPackageInformation> decryptionFailures
            )
            {
                // We need to grab some more information about the package, so that we can
                // determine if it is pinned or not, as well as to grab information about
                // arguments, etc.
                var packageInfo = common.PackageInfoService.Get(packageLocalMetadata);
                PackageInstallLocation = common.FileSystem.GetDirectoryName(package.PackagePath);
                ShouldSkip = config.ListCommand.IgnorePinned && packageInfo.IsPinned;
                if (ShouldSkip)
                {
                    // Other fields are only used in log statements that are behind a skip guard
                    return;
                }
                Deploymentlocation = packageInfo.DeploymentLocation;
                SourceInstalledFrom = packageInfo.SourceInstalledFrom;

                if (config.ListCommand.IncludeVersionOverrides && packageInfo.VersionOverride != null)
                {
                    packageLocalMetadata.OverrideOriginalVersion(packageInfo.VersionOverride);
                }

                if (ShouldDecryptArguments(config,packageInfo))
                {
                    DecryptArguments(common, packageInfo, decryptionFailures);
                }
            }

            private void DecryptArguments(
                NugetServiceCommon common,
                ChocolateyPackageInformation packageInfo, 
                List<ChocolateyPackageInformation> decryptionFailures
            )
            {
                var decryptedArguments = ArgumentsUtility
                    .DecryptPackageArgumentsFile(
                        common.FileSystem, 
                        packageInfo.Package.Id, 
                        packageInfo.Package.Version.ToNormalizedStringChecked()
                    )
                    .ToList();

                if (decryptedArguments.Count <= 0)
                {
                    decryptionFailures.Add(packageInfo);
                }

                PackageArgumentsUnencrypted = 
                    "\n Remembered Package Arguments: \n  {0}"
                    .FormatWith(string.Join(Environment.NewLine + "  ", decryptedArguments));
            }

            // The main scenario where we desire the decrypted arguments during a list sequence is to display them when
            // verbose output is selected. This is done by default on the `info` command, and by request on the `list` command. 
            // As such, we are going to validate it's that scenarioto avoid needlessly decrypting the arguments file.
            private bool ShouldDecryptArguments(
                ChocolateyConfiguration config,
                ChocolateyPackageInformation packageInfo
            ) =>
                !string.IsNullOrWhiteSpace(config.CommandName)
                && config.Verbose
                && !string.IsNullOrWhiteSpace(packageInfo.Arguments)
                && (
                    config.CommandName.Equals("info", StringComparison.OrdinalIgnoreCase)
                    || config.CommandName.Equals("list", StringComparison.OrdinalIgnoreCase)
                );
        }


    }
}