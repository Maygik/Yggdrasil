using System;
using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Yggdrasil.Presentation.Services;
using Yggdrasil.Renderer.Scene;
using Yggdrasil.Types;

namespace Yggdrasil.Presentation.Pages;

public sealed partial class ViewportPage : Page
{
    private bool _isInitializing = true;
    private bool _isRefreshing;

    public AppHost Host { get; }

    public ViewportPage()
    {
        Host = App.Instance.Host;
        InitializeComponent();
        Loaded += ViewportPage_Loaded;
    }

    private void ViewportPage_Loaded(object sender, RoutedEventArgs e)
    {
        RefreshFromViewportOptions();
        _isInitializing = false;
    }

    private void ShowFloorToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (!CanPushViewportOptions())
        {
            return;
        }

        PushViewportOptions();
    }

    private void ShowHeightPlaneToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (!CanPushViewportOptions())
        {
            return;
        }

        PushViewportOptions();
    }

    private void ShowBonesToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (!CanPushViewportOptions())
        {
            return;
        }

        PushViewportOptions();
    }

    private void ShowBoneConnectionsToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (!CanPushViewportOptions())
        {
            return;
        }

        PushViewportOptions();
    }

    private void BoneAxisLengthSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        UpdateBoneAxisLengthText((float)e.NewValue);

        if (!CanPushViewportOptions())
        {
            return;
        }

        PushViewportOptions();
    }

    private void AlignmentNumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (!CanPushViewportOptions())
        {
            return;
        }

        PushViewportOptions();
    }

    private void RefreshFromViewportOptions()
    {
        if (!AreViewportControlsReady())
        {
            return;
        }

        _isRefreshing = true;

        var viewportOptions = Host.Viewport.CurrentViewportOptions;
        ShowFloorToggle.IsOn = viewportOptions.ShowFloor;
        ShowHeightPlaneToggle.IsOn = viewportOptions.ShowHeightPlane;
        ShowBonesToggle.IsOn = viewportOptions.ShowBones;
        ShowBoneConnectionsToggle.IsOn = viewportOptions.ShowBoneConnections;
        BoneAxisLengthSlider.Value = Math.Clamp(
            viewportOptions.BoneAxisLength,
            (float)BoneAxisLengthSlider.Minimum,
            (float)BoneAxisLengthSlider.Maximum);
        ModelScaleNumberBox.Value = viewportOptions.ModelScale;
        ModelRotationXNumberBox.Value = viewportOptions.ModelRotationDegrees.X;
        ModelRotationYNumberBox.Value = viewportOptions.ModelRotationDegrees.Y;
        ModelRotationZNumberBox.Value = viewportOptions.ModelRotationDegrees.Z;
        ModelPositionXNumberBox.Value = viewportOptions.ModelPosition.X;
        ModelPositionYNumberBox.Value = viewportOptions.ModelPosition.Y;
        ModelPositionZNumberBox.Value = viewportOptions.ModelPosition.Z;
        HeightPlaneHeightNumberBox.Value = viewportOptions.HeightPlaneHeight;
        UpdateBoneAxisLengthText((float)BoneAxisLengthSlider.Value);

        _isRefreshing = false;
    }

    private void PushViewportOptions()
    {
        if (!AreViewportControlsReady())
        {
            return;
        }

        Host.Viewport.UpdateViewportOptions(new ViewportRenderOptions
        {
            ShowFloor = ShowFloorToggle.IsOn,
            ShowHeightPlane = ShowHeightPlaneToggle.IsOn,
            ShowBones = ShowBonesToggle.IsOn,
            ShowBoneConnections = ShowBoneConnectionsToggle.IsOn,
            BoneAxisLength = (float)BoneAxisLengthSlider.Value,
            ModelScale = GetNumberBoxValue(ModelScaleNumberBox, 1.0f),
            ModelRotationDegrees = new Vector3(
                GetNumberBoxValue(ModelRotationXNumberBox),
                GetNumberBoxValue(ModelRotationYNumberBox),
                GetNumberBoxValue(ModelRotationZNumberBox)),
            ModelPosition = new Vector3(
                GetNumberBoxValue(ModelPositionXNumberBox),
                GetNumberBoxValue(ModelPositionYNumberBox),
                GetNumberBoxValue(ModelPositionZNumberBox)),
            HeightPlaneHeight = GetNumberBoxValue(HeightPlaneHeightNumberBox)
        });
    }

    private void UpdateBoneAxisLengthText(float axisLength)
    {
        if (BoneAxisLengthValueText == null) return;
        BoneAxisLengthValueText.Text = $"Axis length: {axisLength.ToString("0.0#", CultureInfo.InvariantCulture)}";
    }

    private bool CanPushViewportOptions()
    {
        return !_isInitializing
            && !_isRefreshing
            && AreViewportControlsReady();
    }

    private bool AreViewportControlsReady()
    {
        return ShowFloorToggle != null
            && ShowHeightPlaneToggle != null
            && ShowBonesToggle != null
            && ShowBoneConnectionsToggle != null
            && BoneAxisLengthSlider != null
            && ModelScaleNumberBox != null
            && ModelRotationXNumberBox != null
            && ModelRotationYNumberBox != null
            && ModelRotationZNumberBox != null
            && ModelPositionXNumberBox != null
            && ModelPositionYNumberBox != null
            && ModelPositionZNumberBox != null
            && HeightPlaneHeightNumberBox != null;
    }

    private static float GetNumberBoxValue(NumberBox? numberBox, float fallback = 0.0f)
    {
        if (numberBox == null || double.IsNaN(numberBox.Value) || double.IsInfinity(numberBox.Value))
        {
            return fallback;
        }

        return (float)numberBox.Value;
    }
}
