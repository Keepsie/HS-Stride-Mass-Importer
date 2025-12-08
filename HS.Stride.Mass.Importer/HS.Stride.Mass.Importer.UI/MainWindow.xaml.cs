using System.Reflection;
using System.Windows;
using HS.Stride.Mass.Importer.Core;

namespace HS.Stride.Mass.Importer.UI
{
    public partial class MainWindow : Window
    {
        private readonly StrideMassImporter _importer;

        public MainWindow()
        {
            InitializeComponent();
            _importer = new StrideMassImporter();

            var version = Assembly.GetExecutingAssembly().GetName().Version;
            Title = $"HS Stride Mass Importer v{version?.Major}.{version?.Minor}.{version?.Build} - © 2025 Happenstance Games";
        }

        private void BrowseSourceButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select Source Assets Folder"
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                SourceFolderBox.Text = dialog.SelectedPath;
            }
        }

        private void BrowseProjectButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select Stride Project Folder"
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                StrideProjectBox.Text = dialog.SelectedPath;
            }
        }

        private async void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            var packageName = PackageNameBox.Text.Trim();
            var sourceFolder = SourceFolderBox.Text.Trim();
            var strideProject = StrideProjectBox.Text.Trim();

            if (string.IsNullOrEmpty(packageName) || string.IsNullOrEmpty(sourceFolder) || string.IsNullOrEmpty(strideProject))
            {
                System.Windows.MessageBox.Show("Please fill in all fields.", "Missing Information", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validate inputs
            var validation = _importer.ValidateInputs(packageName, sourceFolder, strideProject);
            if (!validation.IsValid)
            {
                System.Windows.MessageBox.Show($"Validation failed:\n{string.Join("\n", validation.Errors)}", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Check for existing package
            if (_importer.PackageExists(packageName, strideProject))
            {
                var result = System.Windows.MessageBox.Show($"Package '{packageName}' already exists.\nSame-named files will be overwritten. Continue?", "Package Exists", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes)
                    return;
            }

            // Start import
            ImportButton.IsEnabled = false;
            ProgressBar.Visibility = Visibility.Visible;
            StatusText.Text = "Importing...";

            try
            {
                var importResult = await _importer.ImportAssetsAsync(packageName, sourceFolder, strideProject, CreateMaterialsCheckBox.IsChecked == true);
                
                if (importResult.Success)
                {
                    StatusText.Text = "Import completed successfully!";
                    System.Windows.MessageBox.Show($"Import completed!\n\nAssets created: {importResult.AssetsCreated}\nResources copied: {importResult.ResourcesCopied}", "Import Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    StatusText.Text = "Import failed!";
                    System.Windows.MessageBox.Show($"Import failed:\n{importResult.ErrorMessage}", "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = "Import failed!";
                System.Windows.MessageBox.Show($"Import failed: {ex.Message}", "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ImportButton.IsEnabled = true;
                ProgressBar.Visibility = Visibility.Collapsed;
            }
        }
    }
}