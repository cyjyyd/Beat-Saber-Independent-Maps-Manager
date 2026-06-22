using System;

namespace BeatSaberIndependentMapsManager.Abstractions
{
    internal interface IMainView
    {
        void Log(string message);
        void UpdateStatus(string action, string status, int progress);
        void UpdateProgress(int progress);
        void InvokeLog(string message);
        bool IsDisposed { get; }
        void RunOnUIThread(Action action);
    }
}
