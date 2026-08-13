using NsInternal;
using NsPublic;

namespace App
{
	public static class Consumer
	{
		public static string Test()
		{
			return NsPublic.TextUtilities.Shorten(SourceText.Describe() + TextDocument.Open());
		}
	}
}
namespace NsPublic
{
	public static class TextUtilities
	{
		public static string Shorten(string text)
		{
			return text;
		}
	}
}
