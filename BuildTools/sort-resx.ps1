$ErrorActionPreference = "Stop";

[Reflection.Assembly]::LoadWithPartialName("System.Xml.Linq") | Out-Null

Write-Host "Sorting .resx files...";

# Enumerate git-tracked .resx files rather than recursing the file system: raw recursion
# follows directory symlinks out of the repository (rewriting files that are not ours) and
# chokes on directories whose name happens to match *.resx.
$repoRoot = git rev-parse --show-toplevel;
git ls-files -- "*.resx" | foreach ($_) {
	$path = Join-Path $repoRoot $_;
	Write-Host $path;

	$doc = [System.Xml.Linq.XDocument]::Load($path);
	$descendants = [System.Linq.Enumerable]::ToArray($doc.Descendants("data"));

	[System.Xml.Linq.Extensions]::Remove($descendants);
	$ordered = [System.Linq.Enumerable]::OrderBy($descendants, [System.Func[System.Xml.Linq.XElement,string]] { param ($e) $e.Attribute("name").Value }, [System.StringComparer]::Ordinal);
	$doc.Root.Add($ordered);
	$doc.Save($path);
}
