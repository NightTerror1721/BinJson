#nullable enable

namespace Krampus.BinJson.SourceGenerators.Models
{
    /// <summary>
    /// Holds the version range metadata parsed from [BJsonVersion] and [BJsonVersionContext] attributes.
    /// </summary>
    internal sealed class VersionInfo
    {
        public VersionInfo(
            string versionTypeName,
            string? introducedIn,
            string? removedIn,
            string? renamedFrom)
        {
            VersionTypeName = versionTypeName;
            IntroducedIn = introducedIn;
            RemovedIn = removedIn;
            RenamedFrom = renamedFrom;
        }

        /// <summary>Fully qualified name of the version type (e.g. "System.Version").</summary>
        public string VersionTypeName { get; }

        /// <summary>Raw string of the version at which the member was introduced. Null means always present.</summary>
        public string? IntroducedIn { get; }

        /// <summary>Raw string of the version at which the member was removed (exclusive). Null means never removed.</summary>
        public string? RemovedIn { get; }

        /// <summary>Legacy JSON key name for the member, used when migrating renamed members across versions.</summary>
        public string? RenamedFrom { get; }
    }
}
