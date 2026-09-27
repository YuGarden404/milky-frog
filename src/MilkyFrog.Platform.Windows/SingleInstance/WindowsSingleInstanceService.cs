using MilkyFrog.Core.Abstractions;

namespace MilkyFrog.Platform.Windows.SingleInstance;

public sealed class WindowsSingleInstanceService
    : ISingleInstanceService
{
    private const string MutexName =
        @"Local\MilkyFrog.SingleInstance.8B877B34";

    private const string ActivationEventName =
        @"Local\MilkyFrog.Activate.8B877B34";

    private readonly Mutex _instanceMutex;
    private readonly EventWaitHandle _activationEvent;

    private RegisteredWaitHandle? _registeredWait;
    private bool _isDisposed;

    public WindowsSingleInstanceService()
    {
        _instanceMutex = new Mutex(
            initiallyOwned: false,
            MutexName,
            out bool createdNew);

        IsPrimaryInstance = createdNew;

        _activationEvent = new EventWaitHandle(
            initialState: false,
            EventResetMode.AutoReset,
            ActivationEventName);
    }

    public bool IsPrimaryInstance { get; }

    public event EventHandler? ActivationRequested;

    public void StartListening()
    {
        ObjectDisposedException.ThrowIf(
            _isDisposed,
            this);

        if (!IsPrimaryInstance ||
            _registeredWait is not null)
        {
            return;
        }

        _registeredWait =
            ThreadPool.RegisterWaitForSingleObject(
                _activationEvent,
                ActivationEventCallback,
                state: null,
                millisecondsTimeOutInterval: Timeout.Infinite,
                executeOnlyOnce: false);
    }

    public void SignalPrimaryInstance()
    {
        ObjectDisposedException.ThrowIf(
            _isDisposed,
            this);

        if (IsPrimaryInstance)
        {
            return;
        }

        _activationEvent.Set();
    }

    private void ActivationEventCallback(
        object? state,
        bool timedOut)
    {
        if (timedOut || _isDisposed)
        {
            return;
        }

        ActivationRequested?.Invoke(
            this,
            EventArgs.Empty);
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        _registeredWait?.Unregister(
            waitObject: null);

        _registeredWait = null;

        _activationEvent.Dispose();
        _instanceMutex.Dispose();
    }
}