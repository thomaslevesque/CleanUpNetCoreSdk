using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using SemVersion;

namespace CleanUpNetCoreSdk
{
    static class Program
    {
        static void Main(string[] args)
        {
            var sdks = GetSdks().OrderBy(sdk => sdk.Version).ToList();

            Console.WriteLine("Found the following .NET Core SDKs:");

            var grouped = sdks.GroupBy(sdk => new { sdk.Bitness, Version = sdk.Version.TruncateToMinor(), IsPrerelease = sdk.Version.IsPrerelease() });

            foreach (var group in grouped)
            {
                group.Last().Keep = true;
                Console.WriteLine();
                Console.WriteLine($"{group.Key.Version} ({group.Key.Bitness}) {(group.Key.IsPrerelease ? "Prerelease" : "Stable")}");
                foreach (var sdk in group)
                {
                    Console.Write($"- {sdk.Version}");
                    if (sdk.Keep)
                        Console.Write(" <== KEEP");
                    else
                        Console.Write("     REMOVE");
                    Console.WriteLine();
                }
            }

            Console.WriteLine();

            if (sdks.All(sdk => sdk.Keep))
            {
                Console.WriteLine("Nothing to remove. Exiting.");
                return;
            }

            Console.Write("Proceed (y/N)? ");
            string choice = Console.ReadLine();
            if (choice.ToLowerInvariant() == "y")
            {
                var commandSeparators = new[] { ' ', '\t' };
                foreach (var sdk in sdks)
                {
                    if (!sdk.Keep)
                    {
                        Console.Write($"Uninstalling {sdk.DisplayName}... ");
                        var parts = sdk.Id.Split(commandSeparators, 2, StringSplitOptions.RemoveEmptyEntries);
                        var process = Process.Start("msiexec.exe", $"/x {sdk.Id} /qb");
                        process.WaitForExit();
                        if (process.ExitCode == 0)
                            Console.WriteLine("Successfully uninstalled");
                        else
                            Console.WriteLine($"Uninstall failed with exit code {process.ExitCode}");
                    }
                }

                Console.WriteLine();
                Console.WriteLine("Complete");
            }
        }

        private static IEnumerable<DotNetCoreSdk> GetSdks()
        {
            using (var uninstallKey = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall"))
            {
                var subKeyNames = uninstallKey.GetSubKeyNames();
                foreach (var name in subKeyNames)
                {
                    using (var subKey = uninstallKey.OpenSubKey(name))
                    {
                        string displayName = (string) subKey.GetValue("DisplayName");
                        if (displayName?.StartsWith("Microsoft .NET Core SDK", StringComparison.OrdinalIgnoreCase) == true)
                        {
                            const string pattern = @"Microsoft .NET Core SDK( -)? (?<version>\d+\.\d+\.\d+)( - (?<prerelease>\w+\d+))? \((?<bitness>x\d+)\)";
                            var match = Regex.Match(displayName, pattern);
                            if (match.Success)
                            {
                                var versionString = match.Groups["version"].Value;
                                var prerelease = match.Groups["prerelease"].Value;
                                var bitness = match.Groups["bitness"].Value;
                                var fullVersionString = versionString;
                                if (!string.IsNullOrEmpty(prerelease))
                                    fullVersionString += "-" + prerelease;
                                var version = SemanticVersion.Parse(fullVersionString);

                                var sdk = new DotNetCoreSdk(displayName, version, bitness, name);
                                yield return sdk;
                            }
                        }
                    }
                }
            }
        }

        class DotNetCoreSdk
        {
            public DotNetCoreSdk(string displayName, SemanticVersion version, string bitness, string id)
            {
                DisplayName = displayName;
                Version = version;
                Bitness = bitness;
                Id = id;
            }

            public string DisplayName { get; }
            public SemanticVersion Version { get; }
            public string Bitness { get; }
            public string Id { get; }

            public bool Keep { get; set; }
        }

        private static SemanticVersion TruncateToMinor(this SemanticVersion version) => new SemanticVersion(version.Major, version.Minor, null);

        private static bool IsPrerelease(this SemanticVersion version) => !string.IsNullOrEmpty(version.Prerelease);
    }
}
