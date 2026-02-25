using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace ICSharpCode.ILSpyCmd
{
	/// <summary>
	/// Result of checking NuGet for a newer tool package.
	/// </summary>
	/// <param name="RunningVersion">Version currently running in the process.</param>
	/// <param name="LatestVersion">Latest stable version available on the feed.</param>
	/// <param name="UpdateRecommendation"><see langword="true"/> when <paramref name="LatestVersion"/> is newer than <paramref name="RunningVersion"/>.</param>
	internal record PackageCheckResult(NuGetVersion RunningVersion, NuGetVersion LatestVersion, bool UpdateRecommendation);

	// Idea from https://github.com/ErikEJ/EFCorePowerTools/blob/master/src/GUI/efcpt/Services/PackageService.cs
	internal static class DotNetToolUpdateChecker
	{
		static NuGetVersion CurrentPackageVersion()
		{
			return new NuGetVersion(Assembly.GetEntryAssembly()!.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!
				.InformationalVersion);
		}

		/// <summary>
		/// Checks NuGet.org for the latest stable version of a package and compares it with the running tool version.
		/// </summary>
		/// <param name="packageId">The NuGet package identifier to query (for example, <c>ilspycmd</c>).</param>
		/// <returns>
		/// A <see cref="PackageCheckResult"/> when the lookup succeeds; otherwise <see langword="null"/> if the check fails
		/// (for example due to network issues).
		/// </returns>
		public static async Task<PackageCheckResult> CheckForPackageUpdateAsync(string packageId)
		{
			try
			{
				using var cache = new SourceCacheContext();
				var repository = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
				var resource = await repository.GetResourceAsync<FindPackageByIdResource>().ConfigureAwait(false);

				var versions = await resource.GetAllVersionsAsync(
					packageId,
					cache,
					new NullLogger(),
					CancellationToken.None).ConfigureAwait(false);

				var latestVersion = versions.Where(v => v.Release == "").MaxBy(v => v);
				var runningVersion = CurrentPackageVersion();
				int comparisonResult = latestVersion.CompareTo(runningVersion, VersionComparison.Version);

				return new PackageCheckResult(runningVersion, latestVersion, comparisonResult > 0);
			}
#pragma warning disable RCS1075 // Avoid empty catch clause that catches System.Exception.
			catch (Exception)
			{
			}
#pragma warning restore RCS1075 // Avoid empty catch clause that catches System.Exception.

			return null;
		}
	}
}
