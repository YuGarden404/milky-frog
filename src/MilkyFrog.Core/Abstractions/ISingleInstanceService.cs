namespace MilkyFrog.Core.Abstractions;

public interface ISingleInstanceService : IDisposable
{
    bool IsPrimaryInstance { get; }

    event EventHandler? ActivationRequested;

    void StartListening();

    void SignalPrimaryInstance();
}