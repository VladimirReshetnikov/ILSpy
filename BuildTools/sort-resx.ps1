$ErrorActionPreference = "Stop";

[Reflection.Assembly]::LoadWithPartialName("System.Xml.Linq") | Out-Null

Write-Host "Sorting .resx files...";

# Enumerate git-known .resx files rather than recursing the file system: raw recursion
# follows directory symlinks out of the repository (rewriting files that are not ours) and
# chokes on directories whose name happens to match *.resx. --others picks up .resx files
# that exist but are not staged yet, so a freshly added file is sorted on its first build.
# --full-name keeps the paths repo-root-relative even when the script is run from a
# subdirectory, matching the Join-Path below.
$repoRoot = git rev-parse --show-toplevel;
git -C $repoRoot ls-files --full-name --cached --others --exclude-standard -- "*.resx" | foreach ($_) {
	$path = Join-Path $repoRoot $_;
	Write-Host $path;

	$doc = [System.Xml.Linq.XDocument]::Load($path);
	$descendants = [System.Linq.Enumerable]::ToArray($doc.Descendants("data"));

	[System.Xml.Linq.Extensions]::Remove($descendants);
	$ordered = [System.Linq.Enumerable]::OrderBy($descendants, [System.Func[System.Xml.Linq.XElement,string]] { param ($e) $e.Attribute("name").Value }, [System.StringComparer]::Ordinal);
	$doc.Root.Add($ordered);
	$doc.Save($path);
}
