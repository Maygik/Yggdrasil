using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI;
using Yggdrasil.Application;
using Yggdrasil.Application.UseCases;
using Yggdrasil.Domain.Scene;
using Yggdrasil.Presentation.Models;
using Yggdrasil.Presentation.Services;
using Yggdrasil.Types;

namespace Yggdrasil.Presentation.Pages
{
    public sealed partial class MaterialsPage : Page
    {
        public AppHost Host { get; }
        public ObservableCollection<MaterialListItem> MaterialItems { get; } = new();
        private bool _isRefreshingEditor;
        private bool _isExportingMaterials;

        public MaterialsPage()
        {
            Host = App.Instance.Host;
            InitializeComponent();
            InitializeEditorEventHandlers();

            Host.Shell.PropertyChanged += Shell_PropertyChanged;
            Unloaded += MaterialsPage_Unloaded;

            RefreshFromShell();
        }

        private void Shell_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Host.Shell.CurrentSession)
                || e.PropertyName == nameof(Host.Shell.HasOpenProject)
                || e.PropertyName == nameof(Host.Shell.CanExportModel)
                || e.PropertyName == nameof(Host.Shell.SelectedMaterialName))
            {
                RefreshFromShell();
                return;
            }
        }

        private void RefreshFromShell()
        {
            NoProjectPanel.Visibility = Host.Shell.HasOpenProject ? Visibility.Collapsed : Visibility.Visible;
            MaterialsPanel.Visibility = Host.Shell.HasOpenProject ? Visibility.Visible : Visibility.Collapsed;

            MaterialItems.Clear();

            var materialNames = Host.Shell.CurrentSession?.Project?.Scene?.MaterialSettings.Keys
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase);

            if (materialNames is not null)
            {
                foreach (var materialName in materialNames)
                {
                    var materialItem = new MaterialListItem
                    {
                        Name = materialName
                    };

                    materialItem.UpdateSelection(string.Equals(
                        Host.Shell.SelectedMaterialName,
                        materialName,
                        StringComparison.Ordinal));

                    MaterialItems.Add(materialItem);
                }
            }

            NoMaterialsPanel.Visibility = MaterialItems.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
            MaterialListItemsControl.Visibility = MaterialItems.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

            RefreshSelectedMaterialEditor();
            RefreshMaterialExportState();
        }

        private void RefreshSelectedMaterialEditor()
        {
            var selectedMaterial = GetSelectedMaterial();

            SelectedMaterialNameTextBlock.Text = selectedMaterial is null
                ? "Selected material: None"
                : $"Selected material: {selectedMaterial.Name}";
            SelectedMaterialShaderTextBlock.Text = selectedMaterial is null
                ? "Shader: -"
                : $"Shader: {selectedMaterial.Shader}";

            NoSelectedMaterialPanel.Visibility = selectedMaterial is null ? Visibility.Visible : Visibility.Collapsed;
            MaterialEditorPanel.Visibility = selectedMaterial is null ? Visibility.Collapsed : Visibility.Visible;

            _isRefreshingEditor = true;

            try
            {
                ShaderComboBox.SelectedItem = selectedMaterial?.Shader ?? SourceMaterialDefaults.Shader;
                BaseTextureTextBox.Text = selectedMaterial?.BaseTexture ?? string.Empty;
                BumpMapTextBox.Text = selectedMaterial?.BumpMap ?? string.Empty;
                ApplyColorPicker(TintColorPicker, TintValueTextBlock, TintValuePreviewBorder, selectedMaterial?.Tint, SourceMaterialDefaults.Color2);
                NoTintToggleSwitch.IsOn = selectedMaterial?.NoTint ?? SourceMaterialDefaults.Toggle;

                AlphaTestToggleSwitch.IsOn = selectedMaterial?.AlphaTest ?? SourceMaterialDefaults.Toggle;
                ApplyNullableFloatNumberBox(AlphaTestReferenceNumberBox, selectedMaterial?.AlphaTestReference ?? SourceMaterialDefaults.AlphaTestReference);
                AllowAlphaToCoverageToggleSwitch.IsOn = selectedMaterial?.AllowAlphaToCoverage ?? SourceMaterialDefaults.Toggle;
                NoCullToggleSwitch.IsOn = selectedMaterial?.NoCull ?? SourceMaterialDefaults.Toggle;
                TranslucentToggleSwitch.IsOn = selectedMaterial?.Translucent ?? SourceMaterialDefaults.Toggle;
                AdditiveToggleSwitch.IsOn = selectedMaterial?.Additive ?? SourceMaterialDefaults.Toggle;
                SelfIllumToggleSwitch.IsOn = selectedMaterial?.SelfIllum ?? SourceMaterialDefaults.Toggle;
                EmissiveTextureTextBox.Text = selectedMaterial?.EmissiveTexture ?? string.Empty;
                ApplyNullableFloatNumberBox(EmissiveBlendStrengthNumberBox, selectedMaterial?.EmissiveBlendStrength ?? SourceMaterialDefaults.EmissiveBlendStrength);

                HalfLambertToggleSwitch.IsOn = selectedMaterial?.HalfLambert ?? SourceMaterialDefaults.Toggle;
                LightWarpTextureTextBox.Text = selectedMaterial?.LightWarpTexture ?? string.Empty;
                UseEnvMapProbesToggleSwitch.IsOn = selectedMaterial?.UseEnvMapProbes ?? SourceMaterialDefaults.Toggle;
                EnvMapTextBox.Text = selectedMaterial?.EnvMap ?? string.Empty;
                EnvMapMaskTextBox.Text = selectedMaterial?.EnvMapMask ?? string.Empty;
                ApplyColorPicker(EnvMapTintColorPicker, EnvMapTintValueTextBlock, EnvMapTintValuePreviewBorder, selectedMaterial?.EnvMapTint, SourceMaterialDefaults.EnvMapTint);
                ApplyNullableFloatNumberBox(EnvMapContrastNumberBox, selectedMaterial?.EnvMapContrast ?? SourceMaterialDefaults.EnvMapContrast);

                PhongToggleSwitch.IsOn = selectedMaterial?.Phong ?? SourceMaterialDefaults.Toggle;
                ApplyNullableFloatNumberBox(PhongBoostNumberBox, selectedMaterial?.PhongBoost ?? SourceMaterialDefaults.PhongBoost);
                ApplyNullableIntNumberBox(PhongExponentNumberBox, selectedMaterial?.PhongExponent ?? SourceMaterialDefaults.PhongExponent);
                PhongExponentTextureTextBox.Text = selectedMaterial?.PhongExponentTexture ?? string.Empty;
                ApplyTripletEditors(selectedMaterial?.PhongFresnelRanges, SourceMaterialDefaults.PhongFresnelRanges);
                ApplyColorPicker(PhongTintColorPicker, PhongTintValueTextBlock, PhongTintValuePreviewBorder, selectedMaterial?.PhongTint, SourceMaterialDefaults.PhongTint);
                RimLightToggleSwitch.IsOn = selectedMaterial?.RimLight ?? SourceMaterialDefaults.Toggle;
                ApplyNullableIntNumberBox(RimLightExponentNumberBox, selectedMaterial?.RimLightExponent ?? SourceMaterialDefaults.RimLightExponent);
                ApplyNullableFloatNumberBox(RimLightBoostNumberBox, selectedMaterial?.RimLightBoost ?? SourceMaterialDefaults.RimLightBoost);
                RefreshTexturePreviews();
            }
            finally
            {
                _isRefreshingEditor = false;
            }
        }

        private SourceMaterialSettings? GetSelectedMaterial()
        {
            var selectedMaterialName = Host.Shell.SelectedMaterialName;
            if (string.IsNullOrWhiteSpace(selectedMaterialName))
            {
                return null;
            }

            var materials = Host.Shell.CurrentSession?.Project?.Scene?.MaterialSettings;
            if (materials is null)
            {
                return null;
            }

            return materials.TryGetValue(selectedMaterialName, out var material)
                ? material
                : null;
        }

        private void MaterialRow_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is MaterialListItem materialItem)
            {
                Host.Shell.SelectedMaterialName = materialItem.Name;
            }
        }

        private void MaterialRow_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is MaterialListItem materialItem)
            {
                Host.Shell.HoveredMaterialName = materialItem.Name;
            }
        }

        private void MaterialRow_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (sender is FrameworkElement element
                && element.Tag is MaterialListItem materialItem
                && string.Equals(Host.Shell.HoveredMaterialName, materialItem.Name, StringComparison.Ordinal))
            {
                Host.Shell.HoveredMaterialName = null;
            }
        }

        private void InitializeEditorEventHandlers()
        {
            ShaderComboBox.SelectionChanged += ShaderComboBox_SelectionChanged;

            NoTintToggleSwitch.Toggled += MaterialToggleSwitch_Toggled;
            AlphaTestToggleSwitch.Toggled += MaterialToggleSwitch_Toggled;
            AllowAlphaToCoverageToggleSwitch.Toggled += MaterialToggleSwitch_Toggled;
            NoCullToggleSwitch.Toggled += MaterialToggleSwitch_Toggled;
            TranslucentToggleSwitch.Toggled += MaterialToggleSwitch_Toggled;
            AdditiveToggleSwitch.Toggled += MaterialToggleSwitch_Toggled;
            SelfIllumToggleSwitch.Toggled += MaterialToggleSwitch_Toggled;
            HalfLambertToggleSwitch.Toggled += MaterialToggleSwitch_Toggled;
            UseEnvMapProbesToggleSwitch.Toggled += MaterialToggleSwitch_Toggled;
            PhongToggleSwitch.Toggled += MaterialToggleSwitch_Toggled;
            RimLightToggleSwitch.Toggled += MaterialToggleSwitch_Toggled;

            RegisterNumberBoxHandlers(
                AlphaTestReferenceNumberBox,
                EmissiveBlendStrengthNumberBox,
                EnvMapContrastNumberBox,
                PhongBoostNumberBox,
                PhongExponentNumberBox,
                RimLightExponentNumberBox,
                RimLightBoostNumberBox);

            RegisterNumberBoxCommitHandlers(
                PhongFresnelXNumberBox,
                PhongFresnelYNumberBox,
                PhongFresnelZNumberBox);
        }

        private void ShaderComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isRefreshingEditor || ShaderComboBox.SelectedItem is not string shader)
            {
                return;
            }

            UpdateSelectedMaterial(material => material.Shader = shader);
        }

        private void MaterialToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (_isRefreshingEditor || sender is not ToggleSwitch toggleSwitch)
            {
                return;
            }

            switch (toggleSwitch.Name)
            {
                case nameof(NoTintToggleSwitch):
                    UpdateSelectedMaterial(material => material.NoTint = toggleSwitch.IsOn);
                    break;
                case nameof(AlphaTestToggleSwitch):
                    UpdateSelectedMaterial(material => material.AlphaTest = toggleSwitch.IsOn);
                    break;
                case nameof(AllowAlphaToCoverageToggleSwitch):
                    UpdateSelectedMaterial(material => material.AllowAlphaToCoverage = toggleSwitch.IsOn);
                    break;
                case nameof(NoCullToggleSwitch):
                    UpdateSelectedMaterial(material => material.NoCull = toggleSwitch.IsOn);
                    break;
                case nameof(TranslucentToggleSwitch):
                    UpdateSelectedMaterial(material => material.Translucent = toggleSwitch.IsOn);
                    break;
                case nameof(AdditiveToggleSwitch):
                    UpdateSelectedMaterial(material => material.Additive = toggleSwitch.IsOn);
                    break;
                case nameof(SelfIllumToggleSwitch):
                    UpdateSelectedMaterial(material => material.SelfIllum = toggleSwitch.IsOn);
                    break;
                case nameof(HalfLambertToggleSwitch):
                    UpdateSelectedMaterial(material => material.HalfLambert = toggleSwitch.IsOn);
                    break;
                case nameof(UseEnvMapProbesToggleSwitch):
                    UpdateSelectedMaterial(material => material.UseEnvMapProbes = toggleSwitch.IsOn);
                    break;
                case nameof(PhongToggleSwitch):
                    UpdateSelectedMaterial(material => material.Phong = toggleSwitch.IsOn);
                    break;
                case nameof(RimLightToggleSwitch):
                    UpdateSelectedMaterial(material => material.RimLight = toggleSwitch.IsOn);
                    break;
            }
        }

        private void MaterialNumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            if (_isRefreshingEditor)
            {
                return;
            }

            if (IsPhongFresnelNumberBox(sender))
            {
                CommitPhongFresnelRanges();
                return;
            }

            switch (sender.Name)
            {
                case nameof(AlphaTestReferenceNumberBox):
                    if (TryReadNullableFloat(sender, args.NewValue, out var alphaTestReference))
                    {
                        UpdateSelectedMaterial(material => material.AlphaTestReference = alphaTestReference);
                    }
                    break;
                case nameof(EmissiveBlendStrengthNumberBox):
                    if (TryReadNullableFloat(sender, args.NewValue, out var emissiveBlendStrength))
                    {
                        UpdateSelectedMaterial(material => material.EmissiveBlendStrength = emissiveBlendStrength);
                    }
                    break;
                case nameof(EnvMapContrastNumberBox):
                    if (TryReadNullableFloat(sender, args.NewValue, out var envMapContrast))
                    {
                        UpdateSelectedMaterial(material => material.EnvMapContrast = envMapContrast);
                    }
                    break;
                case nameof(PhongBoostNumberBox):
                    if (TryReadNullableFloat(sender, args.NewValue, out var phongBoost))
                    {
                        UpdateSelectedMaterial(material => material.PhongBoost = phongBoost);
                    }
                    break;
                case nameof(PhongExponentNumberBox):
                    if (TryReadNullableInt(sender, args.NewValue, out var phongExponent))
                    {
                        UpdateSelectedMaterial(material => material.PhongExponent = phongExponent);
                    }
                    break;
                case nameof(RimLightExponentNumberBox):
                    if (TryReadNullableInt(sender, args.NewValue, out var rimLightExponent))
                    {
                        UpdateSelectedMaterial(material => material.RimLightExponent = rimLightExponent);
                    }
                    break;
                case nameof(RimLightBoostNumberBox):
                    if (TryReadNullableFloat(sender, args.NewValue, out var rimLightBoost))
                    {
                        UpdateSelectedMaterial(material => material.RimLightBoost = rimLightBoost);
                    }
                    break;
            }
        }

        private void MaterialNumberBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (_isRefreshingEditor || sender is not NumberBox numberBox)
            {
                return;
            }

            CommitNumberBox(numberBox);
        }

        private void MaterialNumberBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (_isRefreshingEditor || sender is not NumberBox numberBox || e.Key != Windows.System.VirtualKey.Enter)
            {
                return;
            }

            CommitNumberBox(numberBox);
            MaterialEditorPanelHost.Focus(FocusState.Programmatic);
            e.Handled = true;
        }

        private void TintColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
        {
            if (_isRefreshingEditor)
            {
                return;
            }

            var tint = FromUiColor(args.NewColor);
            ApplyColorPreview(TintValueTextBlock, TintValuePreviewBorder, tint);
            UpdateSelectedMaterial(material => material.Tint = tint);
        }

        private void EnvMapTintColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
        {
            if (_isRefreshingEditor)
            {
                return;
            }

            var envMapTint = FromUiColor(args.NewColor);
            ApplyColorPreview(EnvMapTintValueTextBlock, EnvMapTintValuePreviewBorder, envMapTint);
            UpdateSelectedMaterial(material => material.EnvMapTint = envMapTint);
        }

        private void PhongTintColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
        {
            if (_isRefreshingEditor)
            {
                return;
            }

            var phongTint = FromUiColor(args.NewColor);
            ApplyColorPreview(PhongTintValueTextBlock, PhongTintValuePreviewBorder, phongTint);
            UpdateSelectedMaterial(material => material.PhongTint = phongTint);
        }

        private void PhongFresnelNumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            if (_isRefreshingEditor)
            {
                return;
            }

            CommitPhongFresnelRanges();
        }

        private async void TextureBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not string targetName)
            {
                return;
            }

            var textBox = ResolveTexturePathTextBox(targetName);
            if (textBox is null)
            {
                return;
            }

            var selectedPath = await Host.FileDialogs.ShowOpenTextureDialogAsync();
            if (!string.IsNullOrWhiteSpace(selectedPath))
            {
                textBox.Text = selectedPath;
                ApplyTexturePreview(targetName, selectedPath);
                ApplyTexturePathChange(targetName, selectedPath);
            }
        }

        private void TextureClearButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not string targetName)
            {
                return;
            }

            var textBox = ResolveTexturePathTextBox(targetName);
            if (textBox is null)
            {
                return;
            }

            textBox.Text = string.Empty;
            ApplyTexturePreview(targetName, null);
            ApplyTexturePathChange(targetName, null);
        }

        private async void ExportSelectedMaterialButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isExportingMaterials)
            {
                return;
            }

            var selectedMaterialName = Host.Shell.SelectedMaterialName;
            if (string.IsNullOrWhiteSpace(selectedMaterialName))
            {
                Host.Shell.StatusMessage = "Select a material before exporting a single material set.";
                return;
            }

            await ExportMaterialsAsync(new[] { selectedMaterialName });
        }

        private async void ExportAllMaterialsButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isExportingMaterials)
            {
                return;
            }

            await ExportMaterialsAsync(null);
        }

        private void MaterialsPage_Unloaded(object sender, RoutedEventArgs e)
        {
            Host.Shell.HoveredMaterialName = null;
            Host.Shell.PropertyChanged -= Shell_PropertyChanged;
        }

        private void RefreshMaterialExportState()
        {
            var addonPath = Host.Shell.CurrentSession?.Project?.Build?.AddonDirectory;
            var hasAddonPath = !string.IsNullOrWhiteSpace(addonPath);
            var hasVtfCmdPath = PackagedToolPaths.HasVtfCmd();
            var canExportMaterials = hasAddonPath && hasVtfCmdPath;
            var hasSelectedMaterial = GetSelectedMaterial() is not null;

            MaterialExportPanel.IsHitTestVisible = canExportMaterials;
            MaterialExportPanel.Opacity = canExportMaterials ? 1.0 : 0.55;
            ExportSelectedMaterialButton.IsEnabled = canExportMaterials && hasSelectedMaterial && !_isExportingMaterials;
            ExportAllMaterialsButton.IsEnabled = canExportMaterials && !_isExportingMaterials;
            MaterialExportHintTextBlock.Text = !hasAddonPath && !hasVtfCmdPath
                ? "Set an addon root on the Project page and include the bundled VTFCmd tool in this app build to enable material export."
                : !hasAddonPath
                    ? "Set an addon root on the Project page to enable material export."
                    : !hasVtfCmdPath
                        ? "This app build does not include the bundled VTFCmd tool yet."
                : _isExportingMaterials
                    ? "Writing VMT and VTF files..."
                    : hasSelectedMaterial
                        ? "Export the selected material or the full material set to the configured addon."
                        : "Export all materials now, or select one to enable single-material export.";
        }

        private async Task ExportMaterialsAsync(IReadOnlyCollection<string>? materialNames)
        {
            if (Host.Shell.CurrentSession?.Project is null)
            {
                Host.Shell.StatusMessage = "No project is currently open.";
                return;
            }

            var request = new ExportMaterialsRequest
            {
                Project = Host.Shell.CurrentSession.Project,
                MaterialNames = materialNames
            };

            try
            {
                _isExportingMaterials = true;
                RefreshMaterialExportState();

                var result = await Task.Run(() => Host.Backend.ExportMaterials.Execute(request));
                RenderMaterialExportResult(result);
            }
            catch (Exception ex)
            {
                Host.Shell.StatusMessage = $"Material export failed: {ex.Message}";
            }
            finally
            {
                _isExportingMaterials = false;
                RefreshMaterialExportState();
            }
        }

        private void RenderMaterialExportResult(ExportMaterialsResult result)
        {
            if (!result.Success)
            {
                Host.Shell.StatusMessage = $"Material export failed: {result.ErrorMessage ?? "Unknown error."}";
                return;
            }

            if (result.HasWarnings)
            {
                var message = result.Messages.FirstOrDefault();
                var warning = result.Warnings.FirstOrDefault() ?? "Material export completed with warnings.";
                Host.Shell.StatusMessage = string.IsNullOrWhiteSpace(message)
                    ? warning
                    : $"{message} {warning}";
                return;
            }

            Host.Shell.StatusMessage = result.Messages.FirstOrDefault() ?? "Material export completed successfully.";
        }

        private void MaterialEditorPanelHost_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (e.OriginalSource is DependencyObject dependencyObject && IsInteractiveInput(dependencyObject))
            {
                return;
            }

            MaterialEditorPanelHost.Focus(FocusState.Pointer);
        }

        private void ApplyColorPicker(ColorPicker colorPicker, TextBlock valueTextBlock, Border previewBorder, Color3? color, Color3 defaultColor)
        {
            colorPicker.Color = ToUiColor(color ?? defaultColor);
            ApplyColorPreview(valueTextBlock, previewBorder, color, defaultColor);
        }

        private void ApplyColorPreview(TextBlock valueTextBlock, Border previewBorder, Color3? color)
        {
            ApplyColorPreview(valueTextBlock, previewBorder, color, SourceMaterialDefaults.Color2);
        }

        private void ApplyColorPreview(TextBlock valueTextBlock, Border previewBorder, Color3? color, Color3 defaultColor)
        {
            var effectiveColor = color ?? defaultColor;
            valueTextBlock.Text = FormatColor(effectiveColor);
            ToolTipService.SetToolTip(
                previewBorder,
                color is null ? $"Default: {valueTextBlock.Text}" : valueTextBlock.Text);

            var uiColor = ToUiColor(effectiveColor);
            previewBorder.Background = new SolidColorBrush(uiColor);
            valueTextBlock.Foreground = new SolidColorBrush(GetContrastingTextColor(uiColor));
        }

        private void ApplyTripletEditors(Vector3? value, Vector3 defaultValue)
        {
            var effectiveValue = value ?? defaultValue;
            ApplyNullableFloatNumberBox(PhongFresnelXNumberBox, effectiveValue.X);
            ApplyNullableFloatNumberBox(PhongFresnelYNumberBox, effectiveValue.Y);
            ApplyNullableFloatNumberBox(PhongFresnelZNumberBox, effectiveValue.Z);
            PhongFresnelRangesValueTextBlock.Text = FormatVector(effectiveValue);
        }

        private void UpdateSelectedMaterial(Action<SourceMaterialSettings> applyChanges)
        {
            if (_isRefreshingEditor)
            {
                return;
            }

            var project = Host.Shell.CurrentSession?.Project;
            var materialName = Host.Shell.SelectedMaterialName;
            if (project is null || string.IsNullOrWhiteSpace(materialName))
            {
                return;
            }

            var result = Host.Backend.ProjectEditor.UpdateMaterial(project, materialName, applyChanges);
            if (!result.Success)
            {
                Host.Shell.StatusMessage = result.ErrorMessage;
                return;
            }

            if (project.Scene?.MaterialSettings.TryGetValue(materialName, out var updatedMaterial) == true)
            {
                SelectedMaterialShaderTextBlock.Text = $"Shader: {updatedMaterial.Shader}";
                Host.Viewport.UpdateMaterial(materialName, updatedMaterial);
            }
        }

        private void ApplyTexturePathChange(string targetName, string? texturePath)
        {
            var normalizedPath = string.IsNullOrWhiteSpace(texturePath) ? null : texturePath;

            switch (targetName)
            {
                case nameof(BaseTextureTextBox):
                    UpdateSelectedMaterial(material => material.BaseTexture = normalizedPath);
                    break;
                case nameof(BumpMapTextBox):
                    UpdateSelectedMaterial(material => material.BumpMap = normalizedPath);
                    break;
                case nameof(EmissiveTextureTextBox):
                    UpdateSelectedMaterial(material => material.EmissiveTexture = normalizedPath);
                    break;
                case nameof(LightWarpTextureTextBox):
                    UpdateSelectedMaterial(material => material.LightWarpTexture = normalizedPath);
                    break;
                case nameof(EnvMapTextBox):
                    UpdateSelectedMaterial(material => material.EnvMap = normalizedPath);
                    break;
                case nameof(EnvMapMaskTextBox):
                    UpdateSelectedMaterial(material => material.EnvMapMask = normalizedPath);
                    break;
                case nameof(PhongExponentTextureTextBox):
                    UpdateSelectedMaterial(material => material.PhongExponentTexture = normalizedPath);
                    break;
            }
        }

        private TextBox? ResolveTexturePathTextBox(string targetName)
        {
            return targetName switch
            {
                nameof(BaseTextureTextBox) => BaseTextureTextBox,
                nameof(BumpMapTextBox) => BumpMapTextBox,
                nameof(EmissiveTextureTextBox) => EmissiveTextureTextBox,
                nameof(LightWarpTextureTextBox) => LightWarpTextureTextBox,
                nameof(EnvMapTextBox) => EnvMapTextBox,
                nameof(EnvMapMaskTextBox) => EnvMapMaskTextBox,
                nameof(PhongExponentTextureTextBox) => PhongExponentTextureTextBox,
                _ => null
            };
        }

        private void RefreshTexturePreviews()
        {
            ApplyTexturePreview(nameof(BaseTextureTextBox), BaseTextureTextBox.Text);
            ApplyTexturePreview(nameof(BumpMapTextBox), BumpMapTextBox.Text);
            ApplyTexturePreview(nameof(EmissiveTextureTextBox), EmissiveTextureTextBox.Text);
            ApplyTexturePreview(nameof(LightWarpTextureTextBox), LightWarpTextureTextBox.Text);
            ApplyTexturePreview(nameof(EnvMapTextBox), EnvMapTextBox.Text);
            ApplyTexturePreview(nameof(EnvMapMaskTextBox), EnvMapMaskTextBox.Text);
            ApplyTexturePreview(nameof(PhongExponentTextureTextBox), PhongExponentTextureTextBox.Text);
        }

        private void ApplyTexturePreview(string targetName, string? texturePath)
        {
            var previewImage = ResolveTexturePreviewImage(targetName);
            var previewBorder = ResolveTexturePreviewBorder(targetName);
            if (previewImage is null || previewBorder is null)
            {
                return;
            }

            previewImage.Source = CreateTexturePreviewSource(texturePath);
            ToolTipService.SetToolTip(
                previewBorder,
                string.IsNullOrWhiteSpace(texturePath) ? "No texture selected" : texturePath);
        }

        private Image? ResolveTexturePreviewImage(string targetName)
        {
            return targetName switch
            {
                nameof(BaseTextureTextBox) => BaseTexturePreviewImage,
                nameof(BumpMapTextBox) => BumpMapPreviewImage,
                nameof(EmissiveTextureTextBox) => EmissiveTexturePreviewImage,
                nameof(LightWarpTextureTextBox) => LightWarpTexturePreviewImage,
                nameof(EnvMapTextBox) => EnvMapPreviewImage,
                nameof(EnvMapMaskTextBox) => EnvMapMaskPreviewImage,
                nameof(PhongExponentTextureTextBox) => PhongExponentTexturePreviewImage,
                _ => null
            };
        }

        private Border? ResolveTexturePreviewBorder(string targetName)
        {
            return targetName switch
            {
                nameof(BaseTextureTextBox) => BaseTexturePreviewBorder,
                nameof(BumpMapTextBox) => BumpMapPreviewBorder,
                nameof(EmissiveTextureTextBox) => EmissiveTexturePreviewBorder,
                nameof(LightWarpTextureTextBox) => LightWarpTexturePreviewBorder,
                nameof(EnvMapTextBox) => EnvMapPreviewBorder,
                nameof(EnvMapMaskTextBox) => EnvMapMaskPreviewBorder,
                nameof(PhongExponentTextureTextBox) => PhongExponentTexturePreviewBorder,
                _ => null
            };
        }

        private static BitmapImage? CreateTexturePreviewSource(string? texturePath)
        {
            if (string.IsNullOrWhiteSpace(texturePath) || !File.Exists(texturePath))
            {
                return null;
            }

            try
            {
                return new BitmapImage(new Uri(texturePath, UriKind.Absolute));
            }
            catch (UriFormatException)
            {
                return null;
            }
        }

        private bool TryParseNullableTriplet(out Vector3? value)
        {
            var xText = PhongFresnelXNumberBox.Text?.Trim();
            var yText = PhongFresnelYNumberBox.Text?.Trim();
            var zText = PhongFresnelZNumberBox.Text?.Trim();

            var hasAnyValue = !string.IsNullOrWhiteSpace(xText)
                || !string.IsNullOrWhiteSpace(yText)
                || !string.IsNullOrWhiteSpace(zText);

            if (!hasAnyValue)
            {
                value = null;
                return true;
            }

            if (!float.TryParse(xText, NumberStyles.Float, CultureInfo.InvariantCulture, out var x)
                || !float.TryParse(yText, NumberStyles.Float, CultureInfo.InvariantCulture, out var y)
                || !float.TryParse(zText, NumberStyles.Float, CultureInfo.InvariantCulture, out var z))
            {
                value = null;
                return false;
            }

            value = new Vector3(x, y, z);
            return true;
        }

        private static bool TryParseNullableFloat(string? text, out float? value)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                value = null;
                return true;
            }

            if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedValue))
            {
                value = parsedValue;
                return true;
            }

            value = null;
            return false;
        }

        private static bool TryReadNullableFloat(NumberBox numberBox, double rawValue, out float? value)
        {
            if (string.IsNullOrWhiteSpace(numberBox.Text))
            {
                value = null;
                return true;
            }

            if (!double.IsNaN(rawValue))
            {
                value = (float)rawValue;
                return true;
            }

            return TryParseNullableFloat(numberBox.Text, out value);
        }

        private static bool TryParseNullableInt(string? text, out int? value)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                value = null;
                return true;
            }

            if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedValue))
            {
                value = parsedValue;
                return true;
            }

            value = null;
            return false;
        }

        private static bool TryReadNullableInt(NumberBox numberBox, double rawValue, out int? value)
        {
            if (string.IsNullOrWhiteSpace(numberBox.Text))
            {
                value = null;
                return true;
            }

            if (!double.IsNaN(rawValue))
            {
                value = Convert.ToInt32(Math.Round(rawValue, MidpointRounding.AwayFromZero));
                return true;
            }

            return TryParseNullableInt(numberBox.Text, out value);
        }

        private static string FormatNullableFloat(float? value)
        {
            return value?.ToString("0.###", CultureInfo.InvariantCulture) ?? string.Empty;
        }

        private static string FormatNullableInt(int? value)
        {
            return value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        }

        private static string FormatColor(Color3 color)
        {
            return $"({color.R:0.###}, {color.G:0.###}, {color.B:0.###})";
        }

        private static string FormatVector(Vector3 vector)
        {
            return $"({vector.X:0.###}, {vector.Y:0.###}, {vector.Z:0.###})";
        }

        private static string FormatTriplet(string x, string y, string z)
        {
            var hasAnyValue = !string.IsNullOrWhiteSpace(x)
                || !string.IsNullOrWhiteSpace(y)
                || !string.IsNullOrWhiteSpace(z);

            return hasAnyValue
                ? $"({NormalizeTripletValue(x)}, {NormalizeTripletValue(y)}, {NormalizeTripletValue(z)})"
                : "Unset";
        }

        private static string NormalizeTripletValue(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
        }

        private static Color ToUiColor(Color3 color)
        {
            return Color.FromArgb(
                255,
                ToByte(color.R),
                ToByte(color.G),
                ToByte(color.B));
        }

        private static Color3 FromUiColor(Color color)
        {
            return Color3.FromInt(color.R, color.G, color.B);
        }

        private static Color GetContrastingTextColor(Color color)
        {
            var luminance = ((0.299f * color.R) + (0.587f * color.G) + (0.114f * color.B)) / 255f;
            return luminance > 0.55f
                ? Color.FromArgb(255, 20, 20, 20)
                : Color.FromArgb(255, 245, 245, 245);
        }

        private static byte ToByte(float value)
        {
            var clamped = Math.Clamp(value, 0f, 1f);
            return (byte)Math.Round(clamped * 255f);
        }

        private void RegisterNumberBoxHandlers(params NumberBox[] numberBoxes)
        {
            foreach (var numberBox in numberBoxes)
            {
                numberBox.ValueChanged += MaterialNumberBox_ValueChanged;
                numberBox.LostFocus += MaterialNumberBox_LostFocus;
                numberBox.KeyDown += MaterialNumberBox_KeyDown;
            }
        }

        private void RegisterNumberBoxCommitHandlers(params NumberBox[] numberBoxes)
        {
            foreach (var numberBox in numberBoxes)
            {
                numberBox.LostFocus += MaterialNumberBox_LostFocus;
                numberBox.KeyDown += MaterialNumberBox_KeyDown;
            }
        }

        private void CommitNumberBox(NumberBox numberBox)
        {
            if (IsPhongFresnelNumberBox(numberBox))
            {
                CommitPhongFresnelRanges();
                return;
            }

            switch (numberBox.Name)
            {
                case nameof(AlphaTestReferenceNumberBox):
                    if (TryParseNullableFloat(numberBox.Text, out var alphaTestReference))
                    {
                        UpdateSelectedMaterial(material => material.AlphaTestReference = alphaTestReference);
                    }
                    break;
                case nameof(EmissiveBlendStrengthNumberBox):
                    if (TryParseNullableFloat(numberBox.Text, out var emissiveBlendStrength))
                    {
                        UpdateSelectedMaterial(material => material.EmissiveBlendStrength = emissiveBlendStrength);
                    }
                    break;
                case nameof(EnvMapContrastNumberBox):
                    if (TryParseNullableFloat(numberBox.Text, out var envMapContrast))
                    {
                        UpdateSelectedMaterial(material => material.EnvMapContrast = envMapContrast);
                    }
                    break;
                case nameof(PhongBoostNumberBox):
                    if (TryParseNullableFloat(numberBox.Text, out var phongBoost))
                    {
                        UpdateSelectedMaterial(material => material.PhongBoost = phongBoost);
                    }
                    break;
                case nameof(PhongExponentNumberBox):
                    if (TryParseNullableInt(numberBox.Text, out var phongExponent))
                    {
                        UpdateSelectedMaterial(material => material.PhongExponent = phongExponent);
                    }
                    break;
                case nameof(RimLightExponentNumberBox):
                    if (TryParseNullableInt(numberBox.Text, out var rimLightExponent))
                    {
                        UpdateSelectedMaterial(material => material.RimLightExponent = rimLightExponent);
                    }
                    break;
                case nameof(RimLightBoostNumberBox):
                    if (TryParseNullableFloat(numberBox.Text, out var rimLightBoost))
                    {
                        UpdateSelectedMaterial(material => material.RimLightBoost = rimLightBoost);
                    }
                    break;
            }
        }

        private void CommitPhongFresnelRanges()
        {
            PhongFresnelRangesValueTextBlock.Text = FormatTriplet(
                PhongFresnelXNumberBox.Text,
                PhongFresnelYNumberBox.Text,
                PhongFresnelZNumberBox.Text);

            if (TryParseNullableTriplet(out var phongFresnelRanges))
            {
                UpdateSelectedMaterial(material => material.PhongFresnelRanges = phongFresnelRanges);
            }
        }

        private static bool IsPhongFresnelNumberBox(NumberBox numberBox)
        {
            return numberBox.Name == nameof(PhongFresnelXNumberBox)
                || numberBox.Name == nameof(PhongFresnelYNumberBox)
                || numberBox.Name == nameof(PhongFresnelZNumberBox);
        }

        private static bool IsInteractiveInput(DependencyObject dependencyObject)
        {
            var current = dependencyObject;
            while (current is not null)
            {
                if (current is NumberBox
                    || current is TextBox
                    || current is ComboBox
                    || current is ToggleSwitch
                    || current is Button
                    || current is ColorPicker)
                {
                    return true;
                }

                current = VisualTreeHelper.GetParent(current);
            }

            return false;
        }

        private static void ApplyNullableFloatNumberBox(NumberBox numberBox, float? value)
        {
            numberBox.Value = value ?? double.NaN;
            numberBox.Text = FormatNullableFloat(value);
        }

        private static void ApplyNullableIntNumberBox(NumberBox numberBox, int? value)
        {
            numberBox.Value = value ?? double.NaN;
            numberBox.Text = FormatNullableInt(value);
        }
    }
}
