// HS Stride Mass Importer (c) 2025 Happenstance Games LLC - Apache License 2.0

using HS.Stride.Mass.Importer.Utilities;

namespace HS.Stride.Mass.Importer.Core
{
    
    /// <summary>
    /// Currently these are just copied from assets i've seen in stride, if this changes later it will need to be updated by hand.
    /// If you want to do more assets open one yourself and add here also. ex. animations so on
    /// </summary>
    public class AssetGenerator
    {
        public string GenerateTextureAsset(string imagePath, string packageName, string assetName)
        {
            var guid = Guid.NewGuid().ToString();
            var resourcePath = $"../../Resources/{packageName}/{Path.GetFileName(imagePath)}";

            
            return $@"!Texture
Id: {guid}
SerializedVersion: {{Stride: 2.0.0.0}}
Tags: []
Source: !file {resourcePath}
Type: !ColorTextureType
    ColorKeyColor: {{R: 255, G: 0, B: 255, A: 255}}
    PremultiplyAlpha: false
";
        }

        public string GenerateMaterialAsset(string materialName, string? textureGuid = null, string? textureName = null)
        {
            var materialGuid = Guid.NewGuid().ToString();
            
            var textureReference = string.IsNullOrEmpty(textureGuid) || string.IsNullOrEmpty(textureName) 
                ? "ab259ecb-f266-44b1-b1b7-80df1407bc3d:BG_Lane_01" 
                : $"{textureGuid}:{textureName}";

            
            return $@"!MaterialAsset
Id: {materialGuid}
SerializedVersion: {{Stride: 2.0.0.0}}
Tags: []
Attributes:
    Diffuse: !MaterialDiffuseMapFeature
        DiffuseMap: !ComputeTextureColor
            Texture: {textureReference}
            FallbackValue:
                Value: {{R: 1.0, G: 1.0, B: 1.0, A: 1.0}}
            Scale: {{X: 1.0, Y: 1.0}}
            Offset: {{X: 0.0, Y: 0.0}}
            Swizzle: null
    DiffuseModel: !MaterialDiffuseLambertModelFeature {{}}
    Overrides:
        UVScale: {{X: 1.0, Y: 1.0}}
Layers: {{}}
";
        }

        public string GenerateModelAsset(string fbxPath, string packageName, string modelName, Dictionary<string, string>? materialReferences = null, string? skeletonReference = null)
        {
            var modelGuid = Guid.NewGuid().ToString();
            var resourcePath = $"../../Resources/{packageName}/{Path.GetFileName(fbxPath)}";

            var materialsSection = "";
            if (materialReferences?.Any() == true)
            {
                var materialEntries = materialReferences.Select(m => 
                    $"    {GenerateMaterialHash(m.Key)}:\n        Name: {m.Key}\n        MaterialInstance:\n            Material: {m.Value}");
                materialsSection = string.Join("\n", materialEntries);
            }

            var skeletonRef = string.IsNullOrEmpty(skeletonReference) ? "null" : skeletonReference;
            var sourceHash = FileHelper.GetFileHash(fbxPath);
            var hashKey = GenerateHashKey(resourcePath);

            
            return $@"!Model
Id: {modelGuid}
SerializedVersion: {{Stride: 2.0.0.0}}
Tags: []
Source: !file {resourcePath}
Skeleton: {skeletonRef}
PivotPosition: {{X: 0.0, Y: 0.0, Z: 0.0}}
Materials:
{materialsSection}
Modifiers: {{}}
~SourceHashes:
    {hashKey}~{resourcePath}: {sourceHash}
";
        }

        private string GenerateMaterialHash(string materialName)
        {
            // Generate a consistent hash for material mapping (simplified)
            using var md5 = System.Security.Cryptography.MD5.Create();
            var inputBytes = System.Text.Encoding.UTF8.GetBytes(materialName);
            var hashBytes = md5.ComputeHash(inputBytes);
            return Convert.ToHexString(hashBytes).ToLower();
        }

        private string GenerateHashKey(string resourcePath)
        {
            // Generate a consistent hash key for source tracking
            using var md5 = System.Security.Cryptography.MD5.Create();
            var inputBytes = System.Text.Encoding.UTF8.GetBytes(resourcePath);
            var hashBytes = md5.ComputeHash(inputBytes);
            return Convert.ToHexString(hashBytes).ToLower();
        }

        public string GenerateRawAsset(string filePath, string packageName, string assetName)
        {
            var guid = Guid.NewGuid().ToString();
            var resourcePath = $"../../Resources/{packageName}/{Path.GetFileName(filePath)}";

            
            return $@"!RawAsset
Id: {guid}
Tags: []
Source: !file {resourcePath}
";
        }

        public string GenerateSoundAsset(string audioPath, string packageName, string assetName)
        {
            var guid = Guid.NewGuid().ToString();
            var resourcePath = $"../../Resources/{packageName}/{Path.GetFileName(audioPath)}";

            return $@"!Sound
Id: {guid}
SerializedVersion: {{Stride: 2.0.0.0}}
Tags: []
Source: !file {resourcePath}
StreamFromDisk: true
Spatialized: false
";
        }

        public string GenerateFontAsset(string fontPath, string packageName, string assetName)
        {
            var guid = Guid.NewGuid().ToString();
            var resourcePath = $"../../Resources/{packageName}/{Path.GetFileName(fontPath)}";

            return $@"!SpriteFont
Id: {guid}
SerializedVersion: {{Stride: 2.0.0.0}}
Tags: []
FontSource: !FileFontProvider
    Source: !file {resourcePath}
FontType: !RuntimeRasterizedSpriteFontType
    Size: 24.0
";
        }

    }
}