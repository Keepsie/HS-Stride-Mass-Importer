// HS Stride Mass Importer (c) 2025 Happenstance Games LLC - Apache License 2.0

using HS.Stride.Mass.Importer.Core;

namespace HS.Stride.Mass.Importer.Console
{
    internal class Program
    {
        private const string VERSION = "0.8.0";
        
        static async Task Main(string[] args)
        {
            ShowBanner();
            System.Console.WriteLine();

            string packageName, sourceFolder, strideProject;

            if (args.Length < 3)
            {
                // Launch wizard mode
                ShowInfo("Welcome to the Mass Importer Wizard!");
                ShowInfo("This will guide you through importing raw assets into your Stride project.");
                System.Console.WriteLine();

                if (!RunWizard(out packageName, out sourceFolder, out strideProject))
                {
                    ShowWarning("Import cancelled.");
                    return;
                }
            }
            else
            {
                // Command line mode
                packageName = args[0];
                sourceFolder = args[1];
                strideProject = args[2];
            }

            try
            {
                ShowInfo("=== Import Configuration ===");
                System.Console.WriteLine();
                System.Console.WriteLine($"Package Name: {packageName}");
                System.Console.WriteLine($"Source Folder: {sourceFolder}");
                System.Console.WriteLine($"Stride Project: {strideProject}");
                System.Console.WriteLine();

                var importer = new StrideMassImporter();

                // Validate inputs first
                ShowProgress("Validating inputs...");
                var validation = importer.ValidateInputs(packageName, sourceFolder, strideProject);
                
                if (!validation.IsValid)
                {
                    ShowError("Validation failed:");
                    foreach (var error in validation.Errors)
                        System.Console.WriteLine($"  • {error}");
                    return;
                }

                if (validation.Warnings.Any())
                {
                    ShowWarning("Warnings:");
                    foreach (var warning in validation.Warnings)
                        System.Console.WriteLine($"  • {warning}");
                    System.Console.WriteLine();
                }

                // Check if package already exists
                if (importer.PackageExists(packageName, strideProject))
                {
                    ShowWarning($"Package '{packageName}' already exists!");
                    System.Console.Write("Overwrite? (y/N): ");
                    var response = System.Console.ReadLine();
                    if (!string.Equals(response, "y", StringComparison.OrdinalIgnoreCase))
                    {
                        ShowWarning("Import cancelled.");
                        return;
                    }
                    System.Console.WriteLine();
                }

                // Preview what will be imported
                ShowProgress("Scanning source folder...");
                var preview = importer.PreviewImport(sourceFolder);
                ShowInfo($"Found: {preview.GetTextureCount()} textures, {preview.GetModelCount()} models, {preview.GetAudioCount()} audio files, {preview.GetRawAssetCount()} raw assets, {preview.GetCodeFileCount()} code files");
                System.Console.WriteLine();

                // Perform the import
                ShowProgress("Starting import...");
                var result = await importer.ImportAssetsAsync(packageName, sourceFolder, strideProject);

                // Show results
                System.Console.WriteLine();
                System.Console.WriteLine(result.GetReport());

                if (result.Success)
                {
                    System.Console.WriteLine();
                    ShowSuccess("Import completed! You can now:");
                    System.Console.WriteLine("  1. Open Stride GameStudio");
                    System.Console.WriteLine("  2. Refresh the Asset View to see imported assets");
                    System.Console.WriteLine($"  3. Assets are located in: Assets/{packageName}/");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Fatal error: {ex.Message}");
                if (args.Contains("--debug"))
                {
                    System.Console.WriteLine();
                    System.Console.WriteLine("Stack trace:");
                    System.Console.WriteLine(ex.StackTrace);
                }
            }

            System.Console.WriteLine();
            System.Console.WriteLine("Press any key to exit...");
            System.Console.ReadKey();
        }

        private static bool RunWizard(out string packageName, out string sourceFolder, out string strideProject)
        {
            packageName = string.Empty;
            sourceFolder = string.Empty;
            strideProject = string.Empty;

            try
            {
                // Step 1: Package Name
                ShowInfo("=== Step 1/3: Package Name ===");
                System.Console.WriteLine("Enter a name for your asset package (e.g., 'SyntyPack', 'Characters', 'DialogSystem'):");
                System.Console.Write("> ");
                packageName = System.Console.ReadLine()?.Trim() ?? string.Empty;

                if (string.IsNullOrEmpty(packageName))
                {
                    ShowError("Package name cannot be empty.");
                    return false;
                }

                System.Console.WriteLine();

                // Step 2: Source Folder
                ShowInfo("=== Step 2/3: Source Folder ===");
                System.Console.WriteLine("Enter the path to your source assets folder:");
                System.Console.WriteLine("(This folder contains your .fbx, .png, .json files, etc.)");
                System.Console.Write("> ");
                sourceFolder = System.Console.ReadLine()?.Trim() ?? string.Empty;

                if (string.IsNullOrEmpty(sourceFolder))
                {
                    ShowError("Source folder path cannot be empty.");
                    return false;
                }

                if (!Directory.Exists(sourceFolder))
                {
                    ShowError($"Source folder does not exist: {sourceFolder}");
                    return false;
                }

                ShowSuccess($"Found source folder: {sourceFolder}");
                System.Console.WriteLine();

                // Step 3: Stride Project
                ShowInfo("=== Step 3/3: Stride Project ===");
                System.Console.WriteLine("Enter the path to your Stride project folder:");
                System.Console.WriteLine("(This should contain your .sln file)");
                System.Console.Write("> ");
                strideProject = System.Console.ReadLine()?.Trim() ?? string.Empty;

                if (string.IsNullOrEmpty(strideProject))
                {
                    ShowError("Stride project path cannot be empty.");
                    return false;
                }

                if (!Directory.Exists(strideProject))
                {
                    ShowError($"Stride project folder does not exist: {strideProject}");
                    return false;
                }

                // Quick validation
                var importer = new StrideMassImporter();
                var validation = importer.ValidateInputs(packageName, sourceFolder, strideProject);
                
                if (!validation.IsValid)
                {
                    ShowError("Validation failed:");
                    foreach (var error in validation.Errors)
                        System.Console.WriteLine($"  • {error}");
                    return false;
                }

                ShowSuccess($"Valid Stride project: {strideProject}");
                System.Console.WriteLine();

                // Final confirmation
                ShowInfo("Summary:");
                System.Console.WriteLine($"  Package: {packageName}");
                System.Console.WriteLine($"  Source:  {sourceFolder}");
                System.Console.WriteLine($"  Target:  {strideProject}");
                System.Console.WriteLine();
                System.Console.Write("Proceed with import? (y/N): ");
                var confirm = System.Console.ReadLine()?.Trim().ToLower();
                
                return confirm == "y" || confirm == "yes";
            }
            catch (Exception ex)
            {
                ShowError($"Wizard error: {ex.Message}");
                return false;
            }
        }

        private static void ShowBanner()
        {
            System.Console.WriteLine(
@"
╔═══════════════════════════════════════════════════════════════════════════╗
║                                                                           ║
║  ██╗  ██╗███████╗    ███╗   ███╗ █████╗ ███████╗███████╗                  ║
║  ██║  ██║██╔════╝    ████╗ ████║██╔══██╗██╔════╝██╔════╝                  ║
║  ███████║███████╗    ██╔████╔██║███████║███████╗███████╗                  ║
║  ██╔══██║╚════██║    ██║╚██╔╝██║██╔══██║╚════██║╚════██║                  ║
║  ██║  ██║███████║    ██║ ╚═╝ ██║██║  ██║███████║███████║                  ║
║  ╚═╝  ╚═╝╚══════╝    ╚═╝     ╚═╝╚═╝  ╚═╝╚══════╝╚══════╝                  ║
║                                                                           ║
║                        Asset Importer v" + VERSION + @"                     ║
║           © 2025 Happenstance Games LLC - All Rights Reserved             ║
║                                                                           ║
╚═══════════════════════════════════════════════════════════════════════════╝");
        }

        private static void ShowSuccess(string message)
        {
            var originalColor = System.Console.ForegroundColor;
            System.Console.ForegroundColor = ConsoleColor.Green;
            System.Console.WriteLine(message);
            System.Console.ForegroundColor = originalColor;
        }

        private static void ShowError(string message)
        {
            var originalColor = System.Console.ForegroundColor;
            System.Console.ForegroundColor = ConsoleColor.Red;
            System.Console.WriteLine(message);
            System.Console.ForegroundColor = originalColor;
        }

        private static void ShowWarning(string message)
        {
            var originalColor = System.Console.ForegroundColor;
            System.Console.ForegroundColor = ConsoleColor.Yellow;
            System.Console.WriteLine(message);
            System.Console.ForegroundColor = originalColor;
        }

        private static void ShowInfo(string message)
        {
            var originalColor = System.Console.ForegroundColor;
            System.Console.ForegroundColor = ConsoleColor.Cyan;
            System.Console.WriteLine(message);
            System.Console.ForegroundColor = originalColor;
        }

        private static void ShowProgress(string message)
        {
            var originalColor = System.Console.ForegroundColor;
            System.Console.ForegroundColor = ConsoleColor.White;
            System.Console.WriteLine(message);
            System.Console.ForegroundColor = originalColor;
        }
    }
}
