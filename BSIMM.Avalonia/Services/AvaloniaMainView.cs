using Avalonia.Controls;
using Avalonia.Threading;
using BeatSaberIndependentMapsManager.Abstractions;
using System;

namespace BSIMM.Avalonia.Services;

internal class AvaloniaMainView : IMainView
{
    private readonly Window _window;
    private bool _isDisposed;

    public AvaloniaMainView(Window window)
    {
        _window = window;
        _window.Closed += (_, _) => _isDisposed = true;
    }

    public bool IsDisposed => _isDisposed;

    public void Log(string message)
    {
        Dispatcher.UIThread.Post(() => LogCore(message));
    }

    public void InvokeLog(string message)
    {
        if (Dispatcher.UIThread.CheckAccess())
            LogCore(message);
        else
            Dispatcher.UIThread.Invoke(() => LogCore(message));
    }

    public void UpdateStatus(string action, string status, int progress)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _window.Title = $"BSIMM - {action}";
        });
    }

    public void UpdateProgress(int progress)
    {
        // Progress bar will be wired later via ViewModel binding
    }

    public void RunOnUIThread(Action action)
    {
        if (Dispatcher.UIThread.CheckAccess())
            action();
        else
            Dispatcher.UIThread.Post(action);
    }

    private void LogCore(string message)
    {
        System.Diagnostics.Debug.WriteLine($"[BSIMM] {message}");
    }
}
