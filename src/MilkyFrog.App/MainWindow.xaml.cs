using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using MilkyFrog.App.Animation;
using MilkyFrog.Core.Abstractions;
using MilkyFrog.Core.Animation;
using MilkyFrog.Core.Assets;
using MilkyFrog.Core.Models;
using MilkyFrog.Core.Services;

namespace MilkyFrog.App;

public partial class MainWindow : Window
{
    private const double EdgeMargin = 20;
    private const double MaximumHorizontalEyeOffset = 6;
    private const double MaximumVerticalEyeOffset = 5;

    private const int WmNcLeftButtonDown = 0x00A1;
    private const int HtCaption = 0x0002;
    private const int WmMoving = 0x0216;

    private const uint MonitorDefaultToNearest = 0x00000002;

    private static readonly TimeSpan EyeTrackingInterval =
        TimeSpan.FromMilliseconds(33);

    private readonly ICursorPositionProvider _cursorPositionProvider;
    private readonly EyeMovementCalculator _eyeMovementCalculator;
    private readonly IAssetCatalog _assetCatalog;
    private readonly IAudioService _audioService;
    private readonly DispatcherTimer _eyeTrackingTimer;
    private readonly WpfAnimationPresenter _animationPresenter;

    private HwndSource? _windowSource;
    private Point2D? _previousCursorPosition;

    private UIElement? _capturedPetSurface;
    private Point _pressPoint;

    private bool _isMousePressed;
    private bool _isSystemDragActive;
    private bool _isApplicationExiting;

    public MainWindow(
        ICursorPositionProvider cursorPositionProvider,
        EyeMovementCalculator eyeMovementCalculator,
        IAssetCatalog assetCatalog,
        IAudioService audioService)
    {
        _cursorPositionProvider = cursorPositionProvider;
        _eyeMovementCalculator = eyeMovementCalculator;
        _assetCatalog = assetCatalog;
        _audioService = audioService;

        InitializeComponent();

        _animationPresenter = new WpfAnimationPresenter(
            CharacterAnimationImage,
            PlaceholderCharacter,
            assetCatalog);

        _animationPresenter.PlaybackCompleted +=
            AnimationPresenter_PlaybackCompleted;

        _eyeTrackingTimer = new DispatcherTimer(
            DispatcherPriority.Background)
        {
            Interval = EyeTrackingInterval
        };

        _eyeTrackingTimer.Tick += EyeTrackingTimer_Tick;

        SourceInitialized += MainWindow_SourceInitialized;
        Loaded += MainWindow_Loaded;
        IsVisibleChanged += MainWindow_IsVisibleChanged;
        Closing += MainWindow_Closing;

        PreviewMouseRightButtonDown +=
            MainWindow_PreviewMouseRightButtonDown;

        LostMouseCapture +=
            MainWindow_LostMouseCapture;

        ContextMenuOpening +=
            MainWindow_ContextMenuOpening;

        ContextMenuClosing +=
            MainWindow_ContextMenuClosing;
    }

    public void PrepareForApplicationExit()
    {
        _isApplicationExiting = true;
        CancelPointerInteraction();
        _audioService.Stop();
    }

    private void MainWindow_SourceInitialized(
        object? sender,
        EventArgs e)
    {
        _windowSource =
            PresentationSource.FromVisual(this) as HwndSource;

        _windowSource?.AddHook(WindowMessageHook);
    }

    private void MainWindow_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        PositionAtBottomRight();
        StartEyeTracking();
        UpdateEyePositions(true);
        _animationPresenter.Start(AnimationState.Idle);
    }

    private void MainWindow_IsVisibleChanged(
        object sender,
        DependencyPropertyChangedEventArgs e)
    {
        CancelPointerInteraction();

        if (IsVisible)
        {
            StartEyeTracking();
            UpdateEyePositions(true);
            _animationPresenter.Start(AnimationState.Idle);
            return;
        }

        StopEyeTracking();
        _animationPresenter.Stop();
        _audioService.Stop();
    }

    private void StartLaughing()
    {
        if (!IsVisible)
        {
            return;
        }

        _animationPresenter.Start(AnimationState.Laughing);

        if (_assetCatalog.TryGetAudio(
                "Laugh",
                out string audioPath))
        {
            _audioService.Play(audioPath);
        }
    }

    private void AnimationPresenter_PlaybackCompleted(
        object? sender,
        EventArgs e)
    {
        if (_isApplicationExiting ||
            !IsVisible ||
            _animationPresenter.CurrentState !=
            AnimationState.Laughing)
        {
            return;
        }

        _animationPresenter.Start(AnimationState.Idle);
    }

    private void StartEyeTracking()
    {
        if (!IsVisible || _eyeTrackingTimer.IsEnabled)
        {
            return;
        }

        _previousCursorPosition = null;
        _eyeTrackingTimer.Start();
    }

    private void StopEyeTracking()
    {
        _eyeTrackingTimer.Stop();
        _previousCursorPosition = null;
    }

    private void EyeTrackingTimer_Tick(
        object? sender,
        EventArgs e)
    {
        UpdateEyePositions(false);
    }

    private void UpdateEyePositions(bool forceUpdate)
    {
        if (!IsVisible ||
            !_cursorPositionProvider.TryGetPosition(
                out Point2D cursorPosition))
        {
            return;
        }

        if (!forceUpdate &&
            _previousCursorPosition == cursorPosition)
        {
            return;
        }

        _previousCursorPosition = cursorPosition;

        Point cursorInWindow = PointFromScreen(
            new Point(
                cursorPosition.X,
                cursorPosition.Y));

        Point leftCenter = GetElementCenter(LeftEye);
        Point rightCenter = GetElementCenter(RightEye);

        var gazeCenter = new Point2D(
            (leftCenter.X + rightCenter.X) / 2,
            (leftCenter.Y + rightCenter.Y) / 2);

        EyeOffset offset =
            _eyeMovementCalculator.CalculateShared(
                gazeCenter,
                new Point2D(
                    cursorInWindow.X,
                    cursorInWindow.Y),
                MaximumHorizontalEyeOffset,
                MaximumVerticalEyeOffset,
                220);

        LeftPupilTransform.X = offset.X;
        LeftPupilTransform.Y = offset.Y;

        RightPupilTransform.X = offset.X;
        RightPupilTransform.Y = offset.Y;
    }

    private Point GetElementCenter(
        FrameworkElement element)
    {
        return element.TranslatePoint(
            new Point(
                element.ActualWidth / 2,
                element.ActualHeight / 2),
            this);
    }

    private void PositionAtBottomRight()
    {
        Rect workArea = SystemParameters.WorkArea;

        Left = workArea.Right -
            ActualWidth -
            EdgeMargin;

        Top = workArea.Bottom -
            ActualHeight -
            EdgeMargin;
    }

    private void PetSurface_MouseLeftButtonDown(
        object sender,
        MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left ||
            sender is not UIElement petSurface)
        {
            return;
        }

        CancelPointerInteraction();

        if (_isSystemDragActive)
        {
            e.Handled = true;
            return;
        }

        if (e.ClickCount == 2)
        {
            StartLaughing();
            e.Handled = true;
            return;
        }

        _pressPoint = e.GetPosition(petSurface);
        _capturedPetSurface = petSurface;

        _isMousePressed =
            petSurface.CaptureMouse();

        if (!_isMousePressed)
        {
            _capturedPetSurface = null;
        }

        e.Handled = true;
    }

    private void PetSurface_MouseMove(
        object sender,
        MouseEventArgs e)
    {
        if (!_isMousePressed ||
            _isSystemDragActive ||
            sender is not UIElement petSurface)
        {
            return;
        }

        if (e.LeftButton != MouseButtonState.Pressed)
        {
            CancelPointerInteraction();
            return;
        }

        Point currentPoint =
            e.GetPosition(petSurface);

        double deltaX =
            currentPoint.X - _pressPoint.X;

        double deltaY =
            currentPoint.Y - _pressPoint.Y;

        bool passedDragThreshold =
            Math.Abs(deltaX) >=
                SystemParameters.MinimumHorizontalDragDistance ||
            Math.Abs(deltaY) >=
                SystemParameters.MinimumVerticalDragDistance;

        if (!passedDragThreshold)
        {
            return;
        }

        e.Handled = true;
        StartSystemDrag();
    }

    private void PetSurface_MouseLeftButtonUp(
        object sender,
        MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left)
        {
            return;
        }

        CancelPointerInteraction();
        e.Handled = true;
    }

    private void StartSystemDrag()
    {
        IntPtr windowHandle =
            _windowSource?.Handle ?? IntPtr.Zero;

        CancelPointerInteraction();

        if (windowHandle == IntPtr.Zero)
        {
            return;
        }

        _isSystemDragActive = true;

        try
        {
            ReleaseCaptureNative();

            SendMessage(
                windowHandle,
                WmNcLeftButtonDown,
                new IntPtr(HtCaption),
                IntPtr.Zero);
        }
        finally
        {
            _isSystemDragActive = false;
            CancelPointerInteraction();
        }
    }

    private void CancelPointerInteraction()
    {
        UIElement? capturedSurface =
            _capturedPetSurface;

        _capturedPetSurface = null;
        _isMousePressed = false;

        if (capturedSurface?.IsMouseCaptured == true)
        {
            capturedSurface.ReleaseMouseCapture();
        }
    }

    private void MainWindow_PreviewMouseRightButtonDown(
        object sender,
        MouseButtonEventArgs e)
    {
        CancelPointerInteraction();
    }

    private void MainWindow_LostMouseCapture(
        object sender,
        MouseEventArgs e)
    {
        if (_capturedPetSurface is not null &&
            !_capturedPetSurface.IsMouseCaptured)
        {
            CancelPointerInteraction();
        }
    }

    private void MainWindow_ContextMenuOpening(
        object sender,
        ContextMenuEventArgs e)
    {
        CancelPointerInteraction();
    }

    private void MainWindow_ContextMenuClosing(
        object sender,
        ContextMenuEventArgs e)
    {
        CancelPointerInteraction();
    }

    private IntPtr WindowMessageHook(
        IntPtr hwnd,
        int message,
        IntPtr wParam,
        IntPtr lParam,
        ref bool handled)
    {
        if (message != WmMoving ||
            lParam == IntPtr.Zero)
        {
            return IntPtr.Zero;
        }

        NativeRect proposed =
            Marshal.PtrToStructure<NativeRect>(lParam);

        IntPtr monitor = MonitorFromRect(
            ref proposed,
            MonitorDefaultToNearest);

        var info = new MonitorInfo
        {
            Size = Marshal.SizeOf<MonitorInfo>()
        };

        if (!GetMonitorInfo(monitor, ref info))
        {
            return IntPtr.Zero;
        }

        int width =
            proposed.Right - proposed.Left;

        int height =
            proposed.Bottom - proposed.Top;

        int left = Math.Clamp(
            proposed.Left,
            info.WorkArea.Left,
            info.WorkArea.Right - width);

        int top = Math.Clamp(
            proposed.Top,
            info.WorkArea.Top,
            info.WorkArea.Bottom - height);

        proposed.Left = left;
        proposed.Top = top;
        proposed.Right = left + width;
        proposed.Bottom = top + height;

        Marshal.StructureToPtr(
            proposed,
            lParam,
            false);

        handled = true;
        return IntPtr.Zero;
    }

    private void HideMenuItem_Click(
        object sender,
        RoutedEventArgs e)
    {
        CancelPointerInteraction();
        Hide();
    }

    private void ExitMenuItem_Click(
        object sender,
        RoutedEventArgs e)
    {
        CancelPointerInteraction();

        if (Application.Current is App app)
        {
            app.ExitApplication();
        }
    }

    private void MainWindow_Closing(
        object? sender,
        CancelEventArgs e)
    {
        CancelPointerInteraction();

        if (!_isApplicationExiting)
        {
            e.Cancel = true;
            Hide();
            return;
        }

        StopEyeTracking();
        _audioService.Stop();

        _eyeTrackingTimer.Tick -=
            EyeTrackingTimer_Tick;

        _animationPresenter.PlaybackCompleted -=
            AnimationPresenter_PlaybackCompleted;

        PreviewMouseRightButtonDown -=
            MainWindow_PreviewMouseRightButtonDown;

        LostMouseCapture -=
            MainWindow_LostMouseCapture;

        ContextMenuOpening -=
            MainWindow_ContextMenuOpening;

        ContextMenuClosing -=
            MainWindow_ContextMenuClosing;

        _animationPresenter.Dispose();

        _windowSource?.RemoveHook(
            WindowMessageHook);
    }

    [DllImport(
        "user32.dll",
        EntryPoint = "ReleaseCapture")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ReleaseCaptureNative();

    [DllImport(
        "user32.dll",
        CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(
        IntPtr windowHandle,
        int message,
        IntPtr wParam,
        IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromRect(
        ref NativeRect rect,
        uint flags);

    [DllImport(
        "user32.dll",
        CharSet = CharSet.Auto,
        SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(
        IntPtr monitor,
        ref MonitorInfo monitorInfo);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(
        LayoutKind.Sequential,
        CharSet = CharSet.Auto)]
    private struct MonitorInfo
    {
        public int Size;
        public NativeRect MonitorArea;
        public NativeRect WorkArea;
        public uint Flags;
    }
}
