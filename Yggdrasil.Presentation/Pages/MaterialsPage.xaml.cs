using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using Windows.UI;
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
                ShaderComboBox.SelectedItem = selectedMaterial?.Shader ?? "VertexLitGeneric";
                BaseTextureTextBox.Text = selectedMaterial?.BaseTexture ?? string.Empty;
                BumpMapTextBox.Text = selectedMaterial?.BumpMap ?? string.Empty;
                ApplyColorPicker(TintColorPicker, TintValueTextBlock, TintValuePreviewBorder, selectedMaterial?.Tint, Color3.White);
                NoTintToggleSwitch.IsOn = selectedMaterial?.NoTint ?? false;

                AlphaTestToggleSwitch.IsOn = selectedMaterial?.AlphaTest ?? false;
                AlphaTestReferenceNumberBox.Text = FormatNullableFloat(selectedMaterial?.AlphaTestReference);
                AllowAlphaToCoverageToggleSwitch.IsOn = selectedMaterial?.AllowAlphaToCoverage ?? false;
                NoCullToggleSwitch.IsOn = selectedMaterial?.NoCull ?? false;
                TranslucentToggleSwitch.IsOn = selectedMaterial?.Translucent ?? false;
                AdditiveToggleSwitch.IsOn = selectedMaterial?.Additive ?? false;
                SelfIllumToggleSwitch.IsOn = selectedMaterial?.SelfIllum ?? false;
                EmissiveTextureTextBox.Text = selectedMaterial?.EmissiveTexture ?? string.Empty;
                EmissiveBlendStrengthNumberBox.Text = FormatNullableFloat(selectedMaterial?.EmissiveBlendStrength);

                HalfLambertToggleSwitch.IsOn = selectedMaterial?.HalfLambert ?? false;
                LightWarpTextureTextBox.Text = selectedMaterial?.LightWarpTexture ?? string.Empty;
                UseEnvMapProbesToggleSwitch.IsOn = selectedMaterial?.UseEnvMapProbes ?? false;
                EnvMapTextBox.Text = selectedMaterial?.EnvMap ?? string.Empty;
                EnvMapMaskTextBox.Text = selectedMaterial?.EnvMapMask ?? string.Empty;
                ApplyColorPicker(EnvMapTintColorPicker, EnvMapTintValueTextBlock, EnvMapTintValuePreviewBorder, selectedMaterial?.EnvMapTint, Color3.White);
                EnvMapContrastNumberBox.Text = FormatNullableFloat(selectedMaterial?.EnvMapContrast);

                PhongToggleSwitch.IsOn = selectedMaterial?.Phong ?? false;
                PhongBoostNumberBox.Text = FormatNullableFloat(selectedMaterial?.PhongBoost);
                PhongExponentNumberBox.Text = FormatNullableInt(selectedMaterial?.PhongExponent);
                PhongExponentTextureTextBox.Text = selectedMaterial?.PhongExponentTexture ?? string.Empty;
                PhongMaskTextBox.Text = selectedMaterial?.PhongMask ?? string.Empty;
                ApplyTripletEditors(selectedMaterial?.PhongFresnelRanges);
                ApplyColorPicker(PhongTintColorPicker, PhongTintValueTextBlock, PhongTintValuePreviewBorder, selectedMaterial?.PhongTint, Color3.White);
                RimLightToggleSwitch.IsOn = selectedMaterial?.RimLight ?? false;
                RimLightExponentNumberBox.Text = FormatNullableInt(selectedMaterial?.RimLightExponent);
                RimLightBoostNumberBox.Text = FormatNullableFloat(selectedMaterial?.RimLightBoost);
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

            AlphaTestReferenceNumberBox.ValueChanged += MaterialNumberBox_ValueChanged;
            EmissiveBlendStrengthNumberBox.ValueChanged += MaterialNumberBox_ValueChanged;
            EnvMapContrastNumberBox.ValueChanged += MaterialNumberBox_ValueChanged;
            PhongBoostNumberBox.ValueChanged += MaterialNumberBox_ValueChanged;
            PhongExponentNumberBox.ValueChanged += MaterialNumberBox_ValueChanged;
            RimLightExponentNumberBox.ValueChanged += MaterialNumberBox_ValueChanged;
            RimLightBoostNumberBox.ValueChanged += MaterialNumberBox_ValueChanged;
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

            switch (sender.Name)
            {
                case nameof(AlphaTestReferenceNumberBox):
                    if (TryParseNullableFloat(sender.Text, out var alphaTestReference))
                    {
                        UpdateSelectedMaterial(material => material.AlphaTestReference = alphaTestReference);
                    }
                    break;
                case nameof(EmissiveBlendStrengthNumberBox):
                    if (TryParseNullableFloat(sender.Text, out var emissiveBlendStrength))
                    {
                        UpdateSelectedMaterial(material => material.EmissiveBlendStrength = emissiveBlendStrength);
                    }
                    break;
                case nameof(EnvMapContrastNumberBox):
                    if (TryParseNullableFloat(sender.Text, out var envMapContrast))
                    {
                        UpdateSelectedMaterial(material => material.EnvMapContrast = envMapContrast);
                    }
                    break;
                case nameof(PhongBoostNumberBox):
                    if (TryParseNullableFloat(sender.Text, out var phongBoost))
                    {
                        UpdateSelectedMaterial(material => material.PhongBoost = phongBoost);
                    }
                    break;
                case nameof(PhongExponentNumberBox):
                    if (TryParseNullableInt(sender.Text, out var phongExponent))
                    {
                        UpdateSelectedMaterial(material => material.PhongExponent = phongExponent);
                    }
                    break;
                case nameof(RimLightExponentNumberBox):
                    if (TryParseNullableInt(sender.Text, out var rimLightExponent))
                    {
                        UpdateSelectedMaterial(material => material.RimLightExponent = rimLightExponent);
                    }
                    break;
                case nameof(RimLightBoostNumberBox):
                    if (TryParseNullableFloat(sender.Text, out var rimLightBoost))
                    {
                        UpdateSelectedMaterial(material => material.RimLightBoost = rimLightBoost);
                    }
                    break;
            }
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

            PhongFresnelRangesValueTextBlock.Text = FormatTriplet(
                PhongFresnelXNumberBox.Text,
                PhongFresnelYNumberBox.Text,
                PhongFresnelZNumberBox.Text);

            if (TryParseNullableTriplet(out var phongFresnelRanges))
            {
                UpdateSelectedMaterial(material => material.PhongFresnelRanges = phongFresnelRanges);
            }
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

        private void MaterialsPage_Unloaded(object sender, RoutedEventArgs e)
        {
            Host.Shell.HoveredMaterialName = null;
            Host.Shell.PropertyChanged -= Shell_PropertyChanged;
        }

        private void ApplyColorPicker(ColorPicker colorPicker, TextBlock valueTextBlock, Border previewBorder, Color3? color, Color3 defaultColor)
        {
            colorPicker.Color = ToUiColor(color ?? defaultColor);
            ApplyColorPreview(valueTextBlock, previewBorder, color, defaultColor);
        }

        private void ApplyColorPreview(TextBlock valueTextBlock, Border previewBorder, Color3? color)
        {
            ApplyColorPreview(valueTextBlock, previewBorder, color, Color3.White);
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

        private void ApplyTripletEditors(Vector3? value)
        {
            if (value is null)
            {
                PhongFresnelXNumberBox.Text = string.Empty;
                PhongFresnelYNumberBox.Text = string.Empty;
                PhongFresnelZNumberBox.Text = string.Empty;
                PhongFresnelRangesValueTextBlock.Text = "Unset";
                return;
            }

            PhongFresnelXNumberBox.Text = value.Value.X.ToString("0.###", CultureInfo.InvariantCulture);
            PhongFresnelYNumberBox.Text = value.Value.Y.ToString("0.###", CultureInfo.InvariantCulture);
            PhongFresnelZNumberBox.Text = value.Value.Z.ToString("0.###", CultureInfo.InvariantCulture);
            PhongFresnelRangesValueTextBlock.Text = FormatVector(value.Value);
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

        private void ApplyTexturePathChange(string targetName, string texturePath)
        {
            switch (targetName)
            {
                case nameof(BaseTextureTextBox):
                    UpdateSelectedMaterial(material => material.BaseTexture = texturePath);
                    break;
                case nameof(BumpMapTextBox):
                    UpdateSelectedMaterial(material => material.BumpMap = texturePath);
                    break;
                case nameof(EmissiveTextureTextBox):
                    UpdateSelectedMaterial(material => material.EmissiveTexture = texturePath);
                    break;
                case nameof(LightWarpTextureTextBox):
                    UpdateSelectedMaterial(material => material.LightWarpTexture = texturePath);
                    break;
                case nameof(EnvMapTextBox):
                    UpdateSelectedMaterial(material => material.EnvMap = texturePath);
                    break;
                case nameof(EnvMapMaskTextBox):
                    UpdateSelectedMaterial(material => material.EnvMapMask = texturePath);
                    break;
                case nameof(PhongExponentTextureTextBox):
                    UpdateSelectedMaterial(material => material.PhongExponentTexture = texturePath);
                    break;
                case nameof(PhongMaskTextBox):
                    UpdateSelectedMaterial(material => material.PhongMask = texturePath);
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
                nameof(PhongMaskTextBox) => PhongMaskTextBox,
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
            ApplyTexturePreview(nameof(PhongMaskTextBox), PhongMaskTextBox.Text);
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
                nameof(PhongMaskTextBox) => PhongMaskPreviewImage,
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
                nameof(PhongMaskTextBox) => PhongMaskPreviewBorder,
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
    }
}
