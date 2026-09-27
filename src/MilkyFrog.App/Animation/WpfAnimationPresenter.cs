using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using MilkyFrog.Core.Animation;
using MilkyFrog.Core.Assets;

namespace MilkyFrog.App.Animation;

public sealed class WpfAnimationPresenter : IDisposable
{
    private readonly Image _image;
    private readonly FrameworkElement _fallback;
    private readonly IAssetCatalog _assetCatalog;
    private readonly ImageAssetLoader _imageLoader;
    private readonly AnimationPlayer _player;
    private readonly DispatcherTimer _timer;
    private readonly Stopwatch _clock;

    private AnimationState _currentState;
    private bool _isDisposed;

    public WpfAnimationPresenter(
        Image image,
        FrameworkElement fallback,
        IAssetCatalog assetCatalog)
    {
        _image = image;
        _fallback = fallback;
        _assetCatalog = assetCatalog;
        _imageLoader = new ImageAssetLoader();
        _player = new AnimationPlayer();
        _clock = new Stopwatch();

        _timer = new DispatcherTimer(
            DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };

        _timer.Tick += Timer_Tick;
    }

    public event EventHandler? PlaybackCompleted;

    public AnimationState CurrentState =>
        _currentState;

    public void Start(AnimationState state)
    {
        ObjectDisposedException.ThrowIf(
            _isDisposed,
            this);

        _currentState = state;

        AnimationClip clip =
            _assetCatalog.GetClip(state);

        _player.Play(
            clip,
            restart: true);

        RenderCurrentFrame();

        _timer.Stop();
        _clock.Reset();

        // 单帧循环资源是静态图片，不需要持续唤醒 UI。
        if (clip.Loop &&
            clip.Frames.Count == 1)
        {
            return;
        }

        _clock.Start();
        _timer.Start();
    }

    public void Stop()
    {
        if (_isDisposed)
        {
            return;
        }

        _timer.Stop();
        _clock.Stop();
        _player.Stop();
    }

    private void Timer_Tick(
        object? sender,
        EventArgs e)
    {
        if (_isDisposed)
        {
            return;
        }

        TimeSpan elapsed = _clock.Elapsed;
        _clock.Restart();

        if (_player.Advance(elapsed))
        {
            RenderCurrentFrame();
        }

        if (!_player.IsCompleted)
        {
            return;
        }

        _timer.Stop();
        _clock.Stop();

        PlaybackCompleted?.Invoke(
            this,
            EventArgs.Empty);
    }

    private void RenderCurrentFrame()
    {
        AnimationFrame? frame =
            _player.CurrentFrame;

        BitmapSource? source = frame is null
            ? null
            : _imageLoader.Load(frame.AssetPath);

        _image.Source = source;

        bool hasImage = source is not null;

        _image.Visibility = hasImage
            ? Visibility.Visible
            : Visibility.Collapsed;

        _fallback.Visibility = hasImage
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        PlaybackCompleted = null;

        _timer.Tick -= Timer_Tick;
        _timer.Stop();
        _clock.Stop();
        _player.Stop();
        _imageLoader.Clear();
    }
}