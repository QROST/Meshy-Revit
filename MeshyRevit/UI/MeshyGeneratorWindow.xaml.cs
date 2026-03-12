// <author>QROST</author>
// <created>20260312</created>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using MeshyRevit.Handlers;
using MeshyRevit.Models;
using MeshyRevit.Services;

namespace MeshyRevit.UI
{
    public partial class MeshyGeneratorWindow : Window
    {
        private readonly UIApplication _uiApp;
        private CancellationTokenSource _cts;
        private bool _isGenerating;
        private readonly List<string> _multiImagePaths = new List<string>();
        private readonly Action<ElementId> _onPlacementSuccess;
        private readonly Action<string> _onPlacementError;

        public MeshyGeneratorWindow(UIApplication uiApp)
        {
            _uiApp = uiApp;
            InitializeComponent();

            _onPlacementSuccess = id =>
            {
                Dispatcher.Invoke(() =>
                {
                    SetStatus($"[Meshy Revit] Placed element {id} successfully.", 100);
                    SetGenerating(false);
                });
            };

            _onPlacementError = error =>
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(error, "Meshy Revit - Placement Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    SetStatus("Placement failed.", 0);
                    SetGenerating(false);
                });
            };

            App.PlacementHandler.OnSuccess += _onPlacementSuccess;
            App.PlacementHandler.OnError += _onPlacementError;
        }

        #region Shared Helpers

        private string GetSelectedAiModel()
        {
            return CbAiModel.SelectedIndex == 0 ? "latest" : "meshy-5";
        }

        private string GetSelectedTopology()
        {
            return CbTopology.SelectedIndex == 0 ? "triangle" : "quad";
        }

        private int GetTargetPolycount()
        {
            if (int.TryParse(TbPolycount.Text, out int val) && val >= 100 && val <= 300000)
                return val;
            return 30000;
        }

        private PlacementMode GetPlacementMode()
        {
            return RbFamily.IsChecked == true ? PlacementMode.Family : PlacementMode.DirectShape;
        }

        private void SetGenerating(bool generating)
        {
            _isGenerating = generating;
            BtnTextGenerate.IsEnabled = !generating;
            BtnImageGenerate.IsEnabled = !generating;
            BtnMultiGenerate.IsEnabled = !generating;
        }

        private void SetStatus(string text, int progress)
        {
            StatusText.Text = text;
            ProgressBar.Value = progress;
        }

        private string ImageFileToDataUri(string filePath)
        {
            byte[] bytes = File.ReadAllBytes(filePath);
            string base64 = Convert.ToBase64String(bytes);
            string ext = Path.GetExtension(filePath).ToLowerInvariant();
            string mime = ext == ".png" ? "image/png" : "image/jpeg";
            return $"data:{mime};base64,{base64}";
        }

        private void PlaceMesh(ParsedMesh mesh)
        {
            if (App.PlacementHandler.Mesh != null)
            {
                MessageBox.Show("A placement is already pending. Please wait.",
                    "Meshy Revit", MessageBoxButton.OK, MessageBoxImage.Information);
                SetGenerating(false);
                return;
            }

            App.PlacementHandler.Mesh = mesh;
            App.PlacementHandler.Mode = GetPlacementMode();
            App.PlacementEvent.Raise();
        }

        #endregion

        #region Text-to-3D

        private async void BtnTextGenerate_Click(object sender, RoutedEventArgs e)
        {
            string prompt = TbTextPrompt.Text?.Trim();
            if (string.IsNullOrWhiteSpace(prompt))
            {
                MessageBox.Show("Please enter a text prompt.", "Meshy Revit",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SetGenerating(true);
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            var settings = MeshySettingsService.Load();

            try
            {
                using (var api = new MeshyApiService(settings.ApiKey))
                {
                    SetStatus("[Meshy Revit] Creating preview task...", 0);
                    var previewRequest = new TextTo3DPreviewRequest
                    {
                        Prompt = prompt,
                        AiModel = GetSelectedAiModel(),
                        Topology = GetSelectedTopology(),
                        TargetPolycount = GetTargetPolycount()
                    };

                    string previewId = await api.CreateTextTo3DPreviewAsync(previewRequest);
                    SetStatus("[Meshy Revit] Generating preview...", 5);

                    var previewResult = await api.PollUntilCompleteAsync(
                        previewId, GenerationMode.TextTo3D,
                        new Progress<int>(p => Dispatcher.Invoke(() => SetStatus($"Preview: {p}%", p / 2))),
                        _cts.Token, settings.PollIntervalMs);

                    SetStatus("[Meshy Revit] Creating refine task...", 50);
                    var refineRequest = new TextTo3DRefineRequest
                    {
                        PreviewTaskId = previewId,
                        EnablePbr = CbPbr.IsChecked == true,
                        AiModel = GetSelectedAiModel(),
                        TexturePrompt = TbTexturePrompt.Text?.Trim()
                    };

                    string refineId = await api.CreateTextTo3DRefineAsync(refineRequest);
                    SetStatus("[Meshy Revit] Refining with texture...", 55);

                    var refineResult = await api.PollUntilCompleteAsync(
                        refineId, GenerationMode.TextTo3D,
                        new Progress<int>(p => Dispatcher.Invoke(() => SetStatus($"Refine: {p}%", 50 + p / 2))),
                        _cts.Token, settings.PollIntervalMs);

                    await DownloadAndPlace(api, refineResult, prompt);
                }
            }
            catch (OperationCanceledException)
            {
                SetStatus("Cancelled.", 0);
                SetGenerating(false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Meshy Revit - Error", MessageBoxButton.OK, MessageBoxImage.Error);
                SetStatus("Failed.", 0);
                SetGenerating(false);
            }
        }

        #endregion

        #region Image-to-3D

        private async void BtnImageGenerate_Click(object sender, RoutedEventArgs e)
        {
            string imageUrl = TbImageUrl.Text?.Trim();
            string imagePath = TbImagePath.Text?.Trim();

            if (string.IsNullOrWhiteSpace(imageUrl) && string.IsNullOrWhiteSpace(imagePath))
            {
                MessageBox.Show("Please select an image or enter a URL.", "Meshy Revit",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(imageUrl) && !string.IsNullOrWhiteSpace(imagePath))
                imageUrl = ImageFileToDataUri(imagePath);

            SetGenerating(true);
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            var settings = MeshySettingsService.Load();

            try
            {
                using (var api = new MeshyApiService(settings.ApiKey))
                {
                    SetStatus("[Meshy Revit] Creating image-to-3D task...", 0);
                    var request = new ImageTo3DRequest
                    {
                        ImageUrl = imageUrl,
                        AiModel = GetSelectedAiModel(),
                        Topology = GetSelectedTopology(),
                        TargetPolycount = GetTargetPolycount(),
                        ShouldTexture = CbShouldTexture.IsChecked == true,
                        EnablePbr = CbPbr.IsChecked == true
                    };

                    string taskId = await api.CreateImageTo3DAsync(request);
                    SetStatus("[Meshy Revit] Generating...", 5);

                    var result = await api.PollUntilCompleteAsync(
                        taskId, GenerationMode.ImageTo3D,
                        new Progress<int>(p => Dispatcher.Invoke(() => SetStatus($"Progress: {p}%", p))),
                        _cts.Token, settings.PollIntervalMs);

                    await DownloadAndPlace(api, result, "Meshy_ImageTo3D");
                }
            }
            catch (OperationCanceledException)
            {
                SetStatus("Cancelled.", 0);
                SetGenerating(false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Meshy Revit - Error", MessageBoxButton.OK, MessageBoxImage.Error);
                SetStatus("Failed.", 0);
                SetGenerating(false);
            }
        }

        private void BtnBrowseImage_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Images|*.jpg;*.jpeg;*.png|All Files|*.*",
                Title = "Select Image for 3D Generation"
            };
            if (dlg.ShowDialog() == true)
            {
                TbImagePath.Text = dlg.FileName;
                TbImageUrl.Text = string.Empty;
            }
        }

        #endregion

        #region Multi-Image-to-3D

        private async void BtnMultiGenerate_Click(object sender, RoutedEventArgs e)
        {
            if (_multiImagePaths.Count == 0)
            {
                MessageBox.Show("Please add at least one image.", "Meshy Revit",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SetGenerating(true);
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            var settings = MeshySettingsService.Load();

            try
            {
                var imageUrls = _multiImagePaths.Select(p => ImageFileToDataUri(p)).ToList();

                using (var api = new MeshyApiService(settings.ApiKey))
                {
                    SetStatus("[Meshy Revit] Creating multi-image-to-3D task...", 0);
                    var request = new MultiImageTo3DRequest
                    {
                        ImageUrls = imageUrls,
                        AiModel = GetSelectedAiModel(),
                        Topology = GetSelectedTopology(),
                        TargetPolycount = GetTargetPolycount(),
                        ShouldTexture = CbMultiShouldTexture.IsChecked == true,
                        EnablePbr = CbPbr.IsChecked == true
                    };

                    string taskId = await api.CreateMultiImageTo3DAsync(request);
                    SetStatus("[Meshy Revit] Generating...", 5);

                    var result = await api.PollUntilCompleteAsync(
                        taskId, GenerationMode.MultiImageTo3D,
                        new Progress<int>(p => Dispatcher.Invoke(() => SetStatus($"Progress: {p}%", p))),
                        _cts.Token, settings.PollIntervalMs);

                    await DownloadAndPlace(api, result, "Meshy_MultiImageTo3D");
                }
            }
            catch (OperationCanceledException)
            {
                SetStatus("Cancelled.", 0);
                SetGenerating(false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Meshy Revit - Error", MessageBoxButton.OK, MessageBoxImage.Error);
                SetStatus("Failed.", 0);
                SetGenerating(false);
            }
        }

        private void BtnAddMultiImage_Click(object sender, RoutedEventArgs e)
        {
            if (_multiImagePaths.Count >= 4)
            {
                MessageBox.Show("Maximum 4 images allowed.", "Meshy Revit",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Images|*.jpg;*.jpeg;*.png|All Files|*.*",
                Title = "Select Image"
            };
            if (dlg.ShowDialog() == true)
            {
                _multiImagePaths.Add(dlg.FileName);
                LbMultiImages.Items.Add(Path.GetFileName(dlg.FileName));
            }
        }

        private void BtnRemoveMultiImage_Click(object sender, RoutedEventArgs e)
        {
            int idx = LbMultiImages.SelectedIndex;
            if (idx >= 0)
            {
                _multiImagePaths.RemoveAt(idx);
                LbMultiImages.Items.RemoveAt(idx);
            }
        }

        #endregion

        #region Download & Place

        private async Task DownloadAndPlace(MeshyApiService api, MeshyTaskStatus result, string name)
        {
            string objUrl = result.ModelUrls?.Obj;
            if (string.IsNullOrWhiteSpace(objUrl))
            {
                MessageBox.Show("No OBJ model URL in the result. The task may not have produced geometry.",
                    "Meshy Revit - No Model", MessageBoxButton.OK, MessageBoxImage.Warning);
                SetStatus("No OBJ available.", 0);
                SetGenerating(false);
                return;
            }

            SetStatus("[Meshy Revit] Downloading OBJ...", 95);
            string objContent = await api.DownloadObjAsync(objUrl);

            SetStatus("[Meshy Revit] Parsing mesh...", 97);
            var mesh = MeshyObjParser.Parse(objContent, name);

            if (mesh.FaceCount == 0)
            {
                MessageBox.Show("Downloaded model contains no faces.", "Meshy Revit - Empty Model",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                SetStatus("Empty model.", 0);
                SetGenerating(false);
                return;
            }

            SetStatus($"[Meshy Revit] Placing mesh ({mesh.VertexCount} vertices, {mesh.FaceCount} faces)...", 98);
            PlaceMesh(mesh);
        }

        #endregion

        #region Settings

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new MeshySettingsWindow();
            settingsWindow.Owner = this;
            settingsWindow.ShowDialog();
        }

        #endregion

        protected override void OnClosed(EventArgs e)
        {
            App.PlacementHandler.OnSuccess -= _onPlacementSuccess;
            App.PlacementHandler.OnError -= _onPlacementError;

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            base.OnClosed(e);
        }
    }
}
