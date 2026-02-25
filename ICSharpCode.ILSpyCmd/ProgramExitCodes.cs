// ReSharper disable InconsistentNaming

namespace ICSharpCode.ILSpyCmd
{
	/// <summary>
	/// Exit codes returned by <c>ilspycmd</c>.
	/// </summary>
	/// <remarks>
	/// Values follow the conventional <c>sysexits</c> meanings where possible.
	/// </remarks>
	public class ProgramExitCodes
	{
		// https://www.freebsd.org/cgi/man.cgi?query=sysexits
		/// <summary>
		/// Command-line usage error (invalid syntax or arguments).
		/// </summary>
		public const int EX_USAGE = 64;
		/// <summary>
		/// Input data is invalid.
		/// </summary>
		public const int EX_DATAERR = 65;
		/// <summary>
		/// An input file could not be read.
		/// </summary>
		public const int EX_NOINPUT = 66;
		/// <summary>
		/// Internal software error.
		/// </summary>
		public const int EX_SOFTWARE = 70;
	}
}
