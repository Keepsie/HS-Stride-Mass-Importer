// HS Stride Mass Importer (c) 2025 Happenstance Games LLC - Apache License 2.0

using HS.Stride.Mass.Importer.Utilities;

namespace HS.Stride.Mass.Importer.Core
{
    public class Importer
    {
        private readonly AssetScanner _assetScanner;
        private readonly AssetGenerator _assetGenerator;

        public Importer()
        {
            _assetScanner = new AssetScanner();
            _assetGenerator = new AssetGenerator();
        }

        public ImportResult ImportPackage(string packageName, string packageFolder, string strideProject)
        {
            // 1. Validate inputs
            ValidateInputs(packageName, packageFolder, strideProject);

            // 2. Detect project structure
            var projectStructure = ProjectStructureDetector.DetectTargetProjectStructure(strideProject);

            // 3. Check for conflicts
            CheckForConflicts(strideProject, projectStructure, packageName);

            // 4. Scan source assets
            var scanResult = _assetScanner.ScanPackageFolder(packageFolder, packageName);

            // 5. Create target directories
            var targetAssets = Path.Combine(strideProject, projectStructure.AssetsPath, packageName);
            var targetResources = Path.Combine(strideProject, projectStructure.ResourcesPath, packageName);
            var targetCode = string.IsNullOrEmpty(projectStructure.CodePath) 
                ? strideProject  // Template structure with no .Game folder - use root
                : Path.Combine(strideProject, projectStructure.CodePath);  // Fresh or Template with .Game folder

            FileHelper.EnsureDirectoryExists(targetAssets);
            FileHelper.EnsureDirectoryExists(targetResources);

            var result = new ImportResult
            {
                TargetAssetsPath = targetAssets,
                TargetResourcesPath = targetResources,
                ProjectStructure = projectStructure
            };

            try
            {
                // Clean single loop: foreach asset, copy resource and create .sd* asset
                foreach (var item in scanResult.Items)
                {
                    ProcessSingleAsset(item, targetAssets, targetResources, targetCode, packageFolder, packageName, result);
                }

                result.Success = true;
                result.AssetsCreated = scanResult.Items.Count(i => i.ShouldCreateStrideAsset());
                result.ResourcesCopied = scanResult.Items.Count(i => i.AssetType != "Code"); // Exclude code files from resource count
                result.CodeFilesCopied = scanResult.Items.Count(i => i.AssetType == "Code");

                // Count processed assets by type
                result.ProcessedTextures = scanResult.Items.Count(i => i.AssetType == "Texture");
                result.ProcessedModels = scanResult.Items.Count(i => i.AssetType == "Model");
                result.ProcessedMaterials = scanResult.Items.Count(i => i.AssetType == "Texture"); // Materials are created for textures
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        private void ValidateInputs(string packageName, string packageFolder, string strideProject)
        {
            if (string.IsNullOrWhiteSpace(packageName))
                throw new ArgumentException("Package name cannot be empty");

            if (!Directory.Exists(packageFolder))
                throw new DirectoryNotFoundException($"Package folder not found: {packageFolder}");

            if (!PathHelper.IsStrideProject(strideProject))
                throw new InvalidOperationException($"Not a valid Stride project: {strideProject}");

            // Validate package name for file system compatibility
            var invalidChars = Path.GetInvalidFileNameChars();
            if (packageName.Any(c => invalidChars.Contains(c)))
                throw new ArgumentException($"Package name contains invalid characters: {packageName}");
        }

        private void CheckForConflicts(string strideProject, TargetProjectStructure projectStructure, string packageName)
        {
            var existingAssetFolder = Path.Combine(strideProject, projectStructure.AssetsPath, packageName);
            if (Directory.Exists(existingAssetFolder))
            {
                var existingFiles = Directory.GetFiles(existingAssetFolder, "*.sd*", SearchOption.AllDirectories);
                if (existingFiles.Any())
                {
                    throw new InvalidOperationException( //Not overwriting for now at least users can change this later if they want.
                        $"Package '{packageName}' already exists with {existingFiles.Length} asset files. " +
                        "Please choose a different name or remove the existing package.");
                }
            }
        }

        private void ProcessSingleAsset(AssetImportItem item, string targetAssets, string targetResources, string targetCode, string sourceFolder, string packageName, ImportResult result)
        {
            try
            {
                // Handle special cases (Code files) first - they don't go to Resources
                if (item.AssetType == "Code")
                {
                    var codeRelativePath = Path.GetRelativePath(sourceFolder, item.SourcePath);
                    var targetCodePath = Path.Combine(targetCode, packageName, codeRelativePath);
                    
                    // Create subdirectory structure as needed
                    var targetCodeDir = Path.GetDirectoryName(targetCodePath);
                    if (!string.IsNullOrEmpty(targetCodeDir))
                    {
                        Directory.CreateDirectory(targetCodeDir);
                    }
                    
                    FileHelper.CopyFile(item.SourcePath, targetCodePath);
                    return;
                }

                // 1. Copy resource file to Resources/PackageName/ preserving folder structure (for non-code files)
                var relativePath = Path.GetRelativePath(sourceFolder, item.SourcePath);
                var targetResourcePath = Path.Combine(targetResources, relativePath);

                // Create subdirectory structure as needed
                var targetResourceDir = Path.GetDirectoryName(targetResourcePath);
                if (!string.IsNullOrEmpty(targetResourceDir))
                {
                    Directory.CreateDirectory(targetResourceDir);
                }

                FileHelper.CopyFile(item.SourcePath, targetResourcePath);

                // 3. Create folder structure matching source structure (not by asset type)
                var relativeDir = Path.GetDirectoryName(relativePath) ?? "";
                var assetTargetFolder = Path.Combine(targetAssets, relativeDir);
                FileHelper.EnsureDirectoryExists(assetTargetFolder);

                // 4. Generate appropriate Stride asset based on type
                string? textureAssetGuid = null;
                string? textureAssetName = null;

                if (item.ShouldCreateStrideAsset())
                {
                    string assetContent = item.AssetType switch
                    {
                        "Texture" => _assetGenerator.GenerateTextureAsset(item.SourcePath, GetPackageNameFromPath(targetResources), item.AssetName),
                        "Model" => _assetGenerator.GenerateModelAsset(item.SourcePath, GetPackageNameFromPath(targetResources), item.AssetName),
                        "Audio" => _assetGenerator.GenerateSoundAsset(item.SourcePath, GetPackageNameFromPath(targetResources), item.AssetName),
                        "Font" => _assetGenerator.GenerateFontAsset(item.SourcePath, GetPackageNameFromPath(targetResources), item.AssetName),
                        "RawAsset" => _assetGenerator.GenerateRawAsset(item.SourcePath, GetPackageNameFromPath(targetResources), item.AssetName),
                        _ => throw new NotSupportedException($"Asset type {item.AssetType} not supported")
                    };

                    // 5. Update resource paths in asset content to match actual structure
                    var updatedAssetContent = UpdateAssetResourcePaths(assetContent, assetTargetFolder, targetResourcePath);

                    // 6. Save asset file to Assets/PackageName/[relative-path]/
                    var finalAssetPath = Path.Combine(assetTargetFolder, $"{item.AssetName}{item.AssetExtension}");
                    FileHelper.SaveFile(updatedAssetContent, finalAssetPath);

                    // 7. Extract GUID and name for material creation (if texture)
                    if (item.AssetType == "Texture")
                    {
                        textureAssetGuid = ExtractGuidFromAsset(updatedAssetContent);
                        textureAssetName = item.AssetName;
                    }
                }

                // 8. Create basic material for each texture with proper reference
                if (item.AssetType == "Texture" && !string.IsNullOrEmpty(textureAssetGuid))
                {
                    CreateBasicMaterialForTexture(item, assetTargetFolder, targetAssets, targetResourcePath, textureAssetGuid, textureAssetName);
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Failed to process {item.AssetType.ToLower()} {item.FileName}: {ex.Message}");
            }
        }

        private void CreateBasicMaterialForTexture(AssetImportItem textureItem, string textureFolder, string targetAssets, string targetResourcePath, string textureAssetGuid, string? textureAssetName)
        {
            try
            {
                // Create Materials folder at the package level (same level as texture folders)
                var materialsFolder = Path.Combine(targetAssets, "Materials");
                FileHelper.EnsureDirectoryExists(materialsFolder);

                // Generate basic material pointing to this texture asset (not the resource file)
                var materialName = textureItem.AssetName + "_Mat";
                var materialContent = _assetGenerator.GenerateMaterialAsset(materialName, textureAssetGuid, textureAssetName);

                // Update any resource paths in the material content as well (though materials shouldn't have resource refs)
                var updatedMaterialContent = UpdateAssetResourcePaths(materialContent, materialsFolder, Path.GetDirectoryName(targetResourcePath) ?? "");

                var materialPath = Path.Combine(materialsFolder, $"{materialName}.sdmat");
                FileHelper.SaveFile(updatedMaterialContent, materialPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not create material for texture {textureItem.AssetName}: {ex.Message}");
            }
        }

        private string GetPackageNameFromPath(string resourcesPath)
        {
            return Path.GetFileName(resourcesPath);
        }

        private string UpdateAssetResourcePaths(string assetContent, string currentAssetFolder, string actualResourcePath)
        {
            // Calculate the relative path from the current asset location to the actual resource file
            var relativePath = Path.GetRelativePath(currentAssetFolder, actualResourcePath);
            
            // Normalize path separators for cross-platform compatibility
            relativePath = relativePath.Replace('\\', '/');
            
            // Get package name and filename to create the proper old pattern
            var resourceFileName = Path.GetFileName(actualResourcePath);
            var packageName = GetPackageNameFromActualResourcePath(actualResourcePath);
            var oldPattern = $"../../Resources/{packageName}/{resourceFileName}";
            var newPattern = relativePath;
            
            // Replace old hardcoded paths with actual relative paths
            var updatedContent = assetContent.Replace($"Source: !file {oldPattern}", $"Source: !file {newPattern}");
            
            // Also handle any other !file references that might exist
            updatedContent = updatedContent.Replace($"!file {oldPattern}", $"!file {newPattern}");
            
            return updatedContent;
        }

        private string GetPackageNameFromActualResourcePath(string actualResourcePath)
        {
            var pathParts = actualResourcePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var resourcesIndex = Array.FindLastIndex(pathParts, part => part.Equals("Resources", StringComparison.OrdinalIgnoreCase));
            
            if (resourcesIndex >= 0 && resourcesIndex + 1 < pathParts.Length)
            {
                return pathParts[resourcesIndex + 1]; // Return the folder name after "Resources"
            }
            
            return "Unknown";
        }

        private string ExtractGuidFromAsset(string assetContent)
        {
            // Extract GUID from "Id: {guid}" line
            var lines = assetContent.Split('\n');
            var idLine = lines.FirstOrDefault(l => l.StartsWith("Id: "));
            return idLine?.Substring(4).Trim() ?? Guid.NewGuid().ToString();
        }
    }
}