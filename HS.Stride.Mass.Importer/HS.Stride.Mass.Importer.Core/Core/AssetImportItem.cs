// HS Stride Mass Importer (c) 2025 Happenstance Games LLC - Apache License 2.0

using HS.Stride.Mass.Importer.Utilities;

namespace HS.Stride.Mass.Importer.Core
{
    public class AssetImportItem
    {
        public string AssetType { get; set; } = string.Empty;        // "Texture", "Model", "RawAsset", "Audio", "Code"
        public string SourcePath { get; set; } = string.Empty;       // Original file path
        public string ResourcePath { get; set; } = string.Empty;     // ../../Resources/PackageName/file.ext
        public string AssetPath { get; set; } = string.Empty;        // Assets/PackageName/Textures/file.sdtex
        public string AssetName { get; set; } = string.Empty;        // Clean name for the asset
        public string FileName { get; set; } = string.Empty;         // Original filename
        public string Extension { get; set; } = string.Empty;        // File extension
        public long FileSize { get; set; }                          // File size in bytes
        public string AssetExtension { get; set; } = string.Empty;   // Target Stride asset extension (.sdtex, .sdm3d, etc.)
        public string SubFolder { get; set; } = string.Empty;        // Subfolder in Assets/ (Textures, Models, Data, etc.)

        public AssetImportItem(string filePath, string packageName)
        {
            SourcePath = filePath;
            FileName = Path.GetFileName(filePath);
            AssetName = PathHelper.MakeValidAssetName(Path.GetFileNameWithoutExtension(filePath));
            Extension = Path.GetExtension(filePath).ToLower();
            FileSize = new FileInfo(filePath).Length;
            
            // Determine asset type and target paths
            DetermineAssetType();
            SetupPaths(packageName);
        }

        private void DetermineAssetType()
        {
            if (PathHelper.IsImageFile(SourcePath))
            {
                AssetType = "Texture";
                AssetExtension = ".sdtex";
                SubFolder = "Textures";
            }
            else if (PathHelper.IsModelFile(SourcePath))
            {
                AssetType = "Model";
                AssetExtension = ".sdm3d";
                SubFolder = "Models";
            }
            else if (PathHelper.IsAudioFile(SourcePath))
            {
                AssetType = "Audio";
                AssetExtension = ".sdsnd";
                SubFolder = "Audio";
            }
            else if (Extension == ".cs")
            {
                AssetType = "Code";
                AssetExtension = ".cs";
                SubFolder = "Code";
            }
            else if (PathHelper.IsRawAssetFile(SourcePath))
            {
                AssetType = "RawAsset";
                AssetExtension = ".sdraw";
                SubFolder = "Data";
            }
            else //Can add more types later doesn't matter for my current use case
            {
                AssetType = "Unknown";
                AssetExtension = Extension;
                SubFolder = "Other";
            }
        }

        private void SetupPaths(string packageName)
        {
            ResourcePath = $"../../Resources/{packageName}/{FileName}";
            
            if (AssetType == "Code")
            {
                // Code files go directly to Code folder, not as Stride assets
                AssetPath = $"Code/{packageName}/{SubFolder}/{FileName}";
            }
            else if (AssetType == "RawAsset")
            {
                // Raw assets keep their original extension
                AssetPath = $"Assets/{packageName}/{SubFolder}/{AssetName}{AssetExtension}";
            }
            else //Even if adding more later this should be the same unless stride changes something.
            {
                // Standard Stride assets get .sd* extensions
                AssetPath = $"Assets/{packageName}/{SubFolder}/{AssetName}{AssetExtension}";
            }
        }

        public bool ShouldCreateStrideAsset()
        {
            // Code files are just copied
            return AssetType != "Code" && AssetType != "Unknown";
        }
    }

    public class ImportScanResult
    {
        public List<AssetImportItem> Items { get; set; } = new();

        public int GetTotalFileCount() => Items.Count;
        
        public int GetTextureCount() => Items.Count(i => i.AssetType == "Texture");
        public int GetModelCount() => Items.Count(i => i.AssetType == "Model");
        public int GetAudioCount() => Items.Count(i => i.AssetType == "Audio");
        public int GetRawAssetCount() => Items.Count(i => i.AssetType == "RawAsset");
        public int GetCodeFileCount() => Items.Count(i => i.AssetType == "Code");
    }
}