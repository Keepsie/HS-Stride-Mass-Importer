// HS Stride Mass Importer (c) 2025 Happenstance Games LLC - Apache License 2.0

using HS.Stride.Mass.Importer.Utilities;

namespace HS.Stride.Mass.Importer.Core
{
    public class AssetScanner
    {
        public ImportScanResult ScanPackageFolder(string packageFolder, string packageName)
        {
            var result = new ImportScanResult();

            if (!Directory.Exists(packageFolder))
            {
                throw new DirectoryNotFoundException($"Package folder not found: {packageFolder}");
            }

            // Discover all importable files
            var allFiles = Directory.GetFiles(packageFolder, "*", SearchOption.AllDirectories)
                .Where(f => !PathHelper.ShouldIgnoreFile(f)) // Filter out ignored files
                .ToList();

            // Create unified list of import items
            foreach (var filePath in allFiles)
            {
                try
                {
                    var importItem = new AssetImportItem(filePath, packageName);
                    if (importItem.AssetType != "Unknown")
                    {
                        result.Items.Add(importItem);
                    }
                }
                catch (Exception ex)
                {
                    // Skip files that can't be processed
                    Console.WriteLine($"Warning: Could not process file {filePath}: {ex.Message}");
                }
            }

            return result;
        }
    }
}