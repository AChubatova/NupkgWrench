﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;
using NuGet.Common;
using NuGet.Packaging;

namespace NupkgWrench
{
    internal static class RemoveFilesCommand
    {
        public static void Register(CommandLineApplication cmdApp, ILogger log)
        {
            cmdApp.Command("remove", (cmd) => Run(cmd, log), throwOnUnexpectedArg: true);
        }

        private static void Run(CommandLineApplication cmd, ILogger log)
        {
            cmd.Description = "Remove files from a nupkg.";
            cmd.HelpOption(Constants.HelpOption);
            var pathOption = cmd.Option("-p|--path", "Paths to remove. These may include wildcards.", CommandOptionType.MultipleValue);
            var idFilter = cmd.Option("-i|--id", "Filter to only packages matching the id or wildcard.", CommandOptionType.SingleValue);
            var versionFilter = cmd.Option("-v|--version", "Filter to only packages matching the version or wildcard.", CommandOptionType.SingleValue);

            var argRoot = cmd.Argument(
                "[root]",
                "Paths to individual packages or directories containing packages.",
                multipleValues: true);

            var required = new List<CommandOption>()
            {
                pathOption
            };

            cmd.OnExecute(() =>
            {
                var inputs = argRoot.Values;

                if (inputs.Count < 1)
                {
                    inputs.Add(Directory.GetCurrentDirectory());
                }

                // Gather all package data
                var packages = Util.GetPackagesWithFilter(idFilter, versionFilter, inputs.ToArray());

                // Validate parameters
                foreach (var requiredOption in required)
                {
                    if (!requiredOption.HasValue())
                    {
                        throw new ArgumentException($"Missing required parameter --{requiredOption.LongName}.");
                    }
                }

                var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var path in pathOption.Values)
                {
                    var fixedPath = Util.GetZipPath(path);
                    paths.Add(fixedPath);
                }

                foreach (var nupkgPath in packages)
                {
                    using (var stream = File.Open(nupkgPath, FileMode.Open))
                    using (var zip = new ZipArchive(stream, ZipArchiveMode.Update))
                    {
                        foreach (var path in paths)
                        {
                            // Remove any existing files
                            Util.RemoveFiles(zip, path, log);
                        }
                    }
                }

                return 0;
            });
        }
    }
}
