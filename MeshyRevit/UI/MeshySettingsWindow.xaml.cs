// <author>QROST</author>
// <created>20260312</created>

using System.Windows;
using MeshyRevit.Services;

namespace MeshyRevit.UI
{
    public partial class MeshySettingsWindow : Window
    {
        public MeshySettingsWindow()
        {
            InitializeComponent();
            var settings = MeshySettingsService.Load();
            ApiKeyTextBox.Text = settings.ApiKey;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string key = ApiKeyTextBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(key) || !key.StartsWith("msy_"))
            {
                MessageBox.Show(
                    "Please enter a valid Meshy API key (starts with msy_).",
                    "Invalid API Key",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var settings = MeshySettingsService.Load();
            settings.ApiKey = key;
            MeshySettingsService.Save(settings);

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
