using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Yggdrasil.Renderer.Camera;
using Yggdrasil.Renderer.Scene;
using Yggdrasil.Types;

namespace Yggdrasil.Presentation.Rendering;

public sealed class OrbitCameraController
{
    private const float OrbitSensitivity = 0.01f;
    private const float PitchLimit = 1.55f;
    private const float MinDistance = 0.1f;
    private const float MaxDistance = 5000.0f;

    private UIElement? _element;
    private InteractionMode _interactionMode;
    private uint? _activePointerId;
    private Vector2 _lastPointerPosition;

    public OrbitCameraState CurrentState { get; private set; } = OrbitCameraState.Default;

    public event Action<OrbitCameraState, bool>? StateChanged;

    public void Attach(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);

        if (ReferenceEquals(_element, element))
        {
            return;
        }

        Detach();

        _element = element;
        _element.PointerPressed += OnPointerPressed;
        _element.PointerMoved += OnPointerMoved;
        _element.PointerReleased += OnPointerReleased;
        _element.PointerCaptureLost += OnPointerCaptureLost;
        _element.PointerCanceled += OnPointerCanceled;
        _element.PointerWheelChanged += OnPointerWheelChanged;
    }

    public void Detach()
    {
        if (_element == null)
        {
            return;
        }

        _element.PointerPressed -= OnPointerPressed;
        _element.PointerMoved -= OnPointerMoved;
        _element.PointerReleased -= OnPointerReleased;
        _element.PointerCaptureLost -= OnPointerCaptureLost;
        _element.PointerCanceled -= OnPointerCanceled;
        _element.PointerWheelChanged -= OnPointerWheelChanged;
        _element = null;

        _interactionMode = InteractionMode.None;
        _activePointerId = null;
    }

    public void SetState(OrbitCameraState cameraState)
    {
        CurrentState = cameraState ?? throw new ArgumentNullException(nameof(cameraState));
    }

    private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (_element == null || _interactionMode != InteractionMode.None)
        {
            return;
        }

        var point = e.GetCurrentPoint(_element);
        if (point.Properties.IsLeftButtonPressed)
        {
            BeginInteraction(InteractionMode.Orbit, e, ToVector2(point.Position.X, point.Position.Y));
        }
        else if (point.Properties.IsMiddleButtonPressed || point.Properties.IsRightButtonPressed)
        {
            BeginInteraction(InteractionMode.Pan, e, ToVector2(point.Position.X, point.Position.Y));
        }
    }

    private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_element == null || _activePointerId != e.Pointer.PointerId || _interactionMode == InteractionMode.None)
        {
            return;
        }

        var point = e.GetCurrentPoint(_element);
        var position = ToVector2(point.Position.X, point.Position.Y);
        var delta = position - _lastPointerPosition;
        if (delta.LengthSquared() <= float.Epsilon)
        {
            return;
        }

        _lastPointerPosition = position;

        CurrentState = _interactionMode switch
        {
            InteractionMode.Orbit => ApplyOrbit(CurrentState, delta),
            InteractionMode.Pan => ApplyPan(CurrentState, delta, _element),
            _ => CurrentState
        };

        StateChanged?.Invoke(CurrentState, true);
        e.Handled = true;
    }

    private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (_activePointerId != e.Pointer.PointerId)
        {
            return;
        }

        _element?.ReleasePointerCapture(e.Pointer);
        EndInteraction();
        e.Handled = true;
    }

    private void OnPointerCaptureLost(object sender, PointerRoutedEventArgs e)
    {
        if (_activePointerId != e.Pointer.PointerId)
        {
            return;
        }

        EndInteraction();
    }

    private void OnPointerCanceled(object sender, PointerRoutedEventArgs e)
    {
        if (_activePointerId != e.Pointer.PointerId)
        {
            return;
        }

        _element?.ReleasePointerCapture(e.Pointer);
        EndInteraction();
    }

    private void OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        if (_element == null)
        {
            return;
        }

        var point = e.GetCurrentPoint(_element);
        var wheelDelta = point.Properties.MouseWheelDelta;
        if (wheelDelta == 0)
        {
            return;
        }

        var zoomFactor = MathF.Pow(0.9f, wheelDelta / 120.0f);
        CurrentState = new OrbitCameraState
        {
            Target = CurrentState.Target,
            Distance = Math.Clamp(CurrentState.Distance * zoomFactor, MinDistance, MaxDistance),
            YawRadians = CurrentState.YawRadians,
            PitchRadians = CurrentState.PitchRadians,
            FieldOfViewDegrees = CurrentState.FieldOfViewDegrees
        };

        StateChanged?.Invoke(CurrentState, false);
        e.Handled = true;
    }

    private void BeginInteraction(InteractionMode interactionMode, PointerRoutedEventArgs e, Vector2 position)
    {
        if (_element == null)
        {
            return;
        }

        _interactionMode = interactionMode;
        _activePointerId = e.Pointer.PointerId;
        _lastPointerPosition = position;
        _element.CapturePointer(e.Pointer);
    }

    private void EndInteraction()
    {
        _interactionMode = InteractionMode.None;
        _activePointerId = null;
        StateChanged?.Invoke(CurrentState, false);
    }

    private static OrbitCameraState ApplyOrbit(OrbitCameraState currentState, Vector2 delta)
    {
        return new OrbitCameraState
        {
            Target = currentState.Target,
            Distance = currentState.Distance,
            YawRadians = currentState.YawRadians - (delta.X * OrbitSensitivity),
            PitchRadians = Math.Clamp(currentState.PitchRadians - (delta.Y * OrbitSensitivity), -PitchLimit, PitchLimit),
            FieldOfViewDegrees = currentState.FieldOfViewDegrees
        };
    }

    private static OrbitCameraState ApplyPan(OrbitCameraState currentState, Vector2 delta, UIElement element)
    {
        var frameworkElement = element as FrameworkElement;
        var height = Math.Max((float)(frameworkElement?.ActualHeight ?? 1.0), 1.0f);

        var cameraPosition = CalculateCameraPosition(currentState);
        var forward = (currentState.Target - cameraPosition).Normalized();
        var worldUp = new Vector3(0.0f, 0.0f, 1.0f);
        var right = Vector3.Cross(forward, worldUp).Normalized();
        if (right.LengthSquared() <= float.Epsilon)
        {
            right = new Vector3(0.0f, 1.0f, 0.0f);
        }

        var up = Vector3.Cross(right, forward).Normalized();
        var worldUnitsPerPixel = (2.0f * currentState.Distance * MathF.Tan((currentState.FieldOfViewDegrees * MathF.PI / 180.0f) * 0.5f)) / height;
        var targetOffset = (((right * -delta.X) + (up * delta.Y)) * worldUnitsPerPixel);

        return new OrbitCameraState
        {
            Target = currentState.Target + targetOffset,
            Distance = currentState.Distance,
            YawRadians = currentState.YawRadians,
            PitchRadians = currentState.PitchRadians,
            FieldOfViewDegrees = currentState.FieldOfViewDegrees
        };
    }

    private static Vector3 CalculateCameraPosition(OrbitCameraState cameraState)
    {
        return OrbitCameraMath.CalculateCameraPosition(cameraState);
    }

    private static Vector2 ToVector2(double x, double y)
    {
        return new Vector2((float)x, (float)y);
    }

    private enum InteractionMode
    {
        None,
        Orbit,
        Pan
    }
}
