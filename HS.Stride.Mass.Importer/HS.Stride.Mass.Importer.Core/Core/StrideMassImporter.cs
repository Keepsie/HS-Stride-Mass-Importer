// HS Stride Mass Importer (c) 2025 Happenstance Games LLC - Apache License 2.0

using HS.Stride.Mass.Importer.Core;
using HS.Stride.Mass.Importer.Utilities;

namespace HS.Stride.Mass.Importer.Core
{
    /// <summary>
    /// Main entry point for the Stride Mass Importer.
    /// Automates the creation of Stride assets from bulk raw resources.
    /// </summary>
    public class StrideMassImporter
    {
        private readonly Importer _importer;

        public StrideMassImporter()
        {
            _importer = new Importer();
        }
        
        public Task<ImportResult> ImportAssetsAsync(string packageName, string sourceFolder, string strideProjectPath)
        {
            return Task.FromResult(_importer.ImportPackage(packageName, sourceFolder, strideProjectPath));
        }
        
        public ValidationResult ValidateInputs(string packageName, string sourceFolder, string strideProjectPath)
        {
            var result = new ValidationResult();

            // Validate package name
            if (string.IsNullOrWhiteSpace(packageName))
            {
                result.Errors.Add("Package name cannot be empty");
            }
            else
            {
                var invalidChars = Path.GetInvalidFileNameChars();
                if (packageName.Any(c => invalidChars.Contains(c)))
                {
                    result.Errors.Add($"Package name contains invalid characters: {packageName}");
                }
            }

            // Validate source folder
            if (!Directory.Exists(sourceFolder))
            {
                result.Errors.Add($"Source folder not found: {sourceFolder}");
            }
            else
            {
                // Check if folder contains importable assets
                var scanner = new AssetScanner();
                try
                {
                    var scanResult = scanner.ScanPackageFolder(sourceFolder, "Preview");
                    if (scanResult.GetTotalFileCount() == 0)
                    {
                        result.Warnings.Add("No importable assets found in source folder");
                    }
                }
                catch (Exception ex)
                {
                    result.Warnings.Add($"Could not scan source folder: {ex.Message}");
                }
            }

            // Validate Stride project
            var projectValidation = PathHelper.ValidateStrideProject(strideProjectPath);
            if (!projectValidation.IsValid)
            {
                result.Errors.Add(projectValidation.ErrorMessage);
                result.Errors.AddRange(projectValidation.Suggestions);
            }

            return result;
        }
        
        public ImportScanResult PreviewImport(string sourceFolder)
        {
            var scanner = new AssetScanner();
            return scanner.ScanPackageFolder(sourceFolder, "Preview");
        }
        
        public bool PackageExists(string packageName, string strideProjectPath)
        {
            try
            {
                var projectStructure = ProjectStructureDetector.DetectTargetProjectStructure(strideProjectPath);
                var packagePath = Path.Combine(strideProjectPath, projectStructure.AssetsPath, packageName);
                
                return Directory.Exists(packagePath) && 
                       Directory.GetFiles(packagePath, "*.sd*", SearchOption.AllDirectories).Any();
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Simple validation result for input checking.
    /// </summary>
    public class ValidationResult
    {
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public bool IsValid => !Errors.Any();
    }
}