// HS Stride Mass Importer (c) 2025 Happenstance Games LLC - Apache License 2.0
using HS.Stride.Mass.Importer.Utilities;

namespace HS.Stride.Mass.Importer.Core
{
    public class ImportResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string TargetAssetsPath { get; set; } = string.Empty;
        public string TargetResourcesPath { get; set; } = string.Empty;
        public TargetProjectStructure? ProjectStructure { get; set; }
        
        // Statistics
        public int AssetsCreated { get; set; }
        public int ResourcesCopied { get; set; }
        public int CodeFilesCopied { get; set; }
        public int ProcessedTextures { get; set; }
        public int ProcessedMaterials { get; set; }
        public int ProcessedModels { get; set; }
        
        // Error tracking
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        
        public bool HasErrors => Errors.Any();
        public bool HasWarnings => Warnings.Any();
        
        public string GetReport()
        {
            var report = new List<string>();
            
            if (Success)
            {
                report.Add("Mass import completed successfully!");
                report.Add($"Assets created: {AssetsCreated}");
                report.Add($"Resources copied: {ResourcesCopied}");
                if (CodeFilesCopied > 0)
                    report.Add($"Code files copied: {CodeFilesCopied}");
                report.Add($"Textures: {ProcessedTextures}");
                report.Add($"Materials: {ProcessedMaterials}");
                report.Add($"Models: {ProcessedModels}");
                report.Add($"Target location: {TargetAssetsPath}");
            }
            else
            {
                report.Add("Mass import failed!");
                if (!string.IsNullOrEmpty(ErrorMessage))
                    report.Add($"Error: {ErrorMessage}");
            }
            
            if (HasWarnings)
            {
                report.Add("\nWarnings:");
                report.AddRange(Warnings.Select(w => $"  {w}"));
            }
            
            if (HasErrors)
            {
                report.Add("\nErrors:");
                report.AddRange(Errors.Select(e => $"  {e}"));
            }
            
            return string.Join('\n', report);
        }
    }
}