using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using BSIMM.Avalonia.Services;
using BSIMM.Avalonia.Views;
using BeatSaberIndependentMapsManager;
using BeatSaberIndependentMapsManager.ViewModels;

namespace BSIMM.Avalonia;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var config = new Config();
            var mainWindow = new MainWindow();
            var mainView = new AvaloniaMainView(mainWindow);
            var mainViewModel = new MainViewModel(mainView, config);

            mainWindow.DataContext = mainViewModel;
            desktop.MainWindow = mainWindow;

            mainViewModel.InitializeGameDetection();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
