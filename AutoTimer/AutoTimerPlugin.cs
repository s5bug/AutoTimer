using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using AutoTimer.Game;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using AutoTimer.Windows;
using Dalamud.Game;

namespace AutoTimer;

public sealed partial class AutoTimerPlugin : IDalamudPlugin {
    public string Name => "Auto Timer";
    private const string ToggleTimerCommand = "/autotimer";
    private const string ConfigTimerCommand = "/autotimerconfig";

    private DalamudPluginInterface PluginInterface { get; init; }
    private ICommandManager CommandManager { get; init; }
    public Configuration Configuration { get; init; }
    public WindowSystem WindowSystem = new("AutoTimer");

    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }
    
    public AutoTimerHooksListener HooksListener { get; init; }
    private Hooks Hooks { get; init;  }
    
    public AutoCalculator AutoCalculator { get; init; }

    public AutoTimerPlugin(
        [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
        [RequiredVersion("1.0")] ICommandManager commandManager,
        [RequiredVersion("1.0")] ISigScanner sigScanner,
        [RequiredVersion("1.0")] IGameInteropProvider gameInteropProvider,
        [RequiredVersion("1.0")] IClientState clientState,
        [RequiredVersion("1.0")] IDataManager dataManager,
        [RequiredVersion("1.0")] IFramework framework
    ) {
        this.PluginInterface = pluginInterface;
        this.CommandManager = commandManager;

        this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        this.Configuration.Initialize(this.PluginInterface);

        this.HooksListener = new AutoTimerHooksListener(clientState);
        this.Hooks = new Hooks(this.HooksListener, sigScanner, gameInteropProvider);
        this.Hooks.Enable();

        framework.Update += this.HooksListener.FrameworkUpdate;

        this.AutoCalculator = new AutoCalculator(clientState, dataManager);

        // you might normally want to embed resources and load them from the manifest stream
        var gaugePath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "autoattack_gauge.png");
        var gaugeImage = this.PluginInterface.UiBuilder.LoadImage(gaugePath);

        // TODO I need to make loading these images not completely terrible
        var gaugeMonkPath = Path.Combine(this.PluginInterface.AssemblyLocation.Directory?.FullName!,
                                         "autoattack_gauge_monk.png");
        var gaugeMonkImage = this.PluginInterface.UiBuilder.LoadImage(gaugeMonkPath);
        
        var gaugeNinjaPath = Path.Combine(this.PluginInterface.AssemblyLocation.Directory?.FullName!,
                                         "autoattack_gauge_ninja.png");
        var gaugeNinjaImage = this.PluginInterface.UiBuilder.LoadImage(gaugeNinjaPath);

        var progressPath =
            Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "autoattack_progress.png");
        var progressImage = this.PluginInterface.UiBuilder.LoadImage(progressPath);
        
        var tcjProgressPath =
            Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "tcj_progress.png");
        var tcjProgressImage = this.PluginInterface.UiBuilder.LoadImage(tcjProgressPath);

        ConfigWindow = new ConfigWindow(this);
        MainWindow = new MainWindow(
            this,
            clientState,
            gaugeImage,
            gaugeMonkImage,
            gaugeNinjaImage,
            progressImage,
            tcjProgressImage
            );

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);

        this.CommandManager.AddHandler(ToggleTimerCommand, new CommandInfo(this.OnCommand) {
            HelpMessage = "Toggles the autotimer bar"
        });
        this.CommandManager.AddHandler(ConfigTimerCommand, new CommandInfo(this.OnCommand) {
            HelpMessage = "Toggles the autotimer configuration window"
        });

        this.PluginInterface.UiBuilder.Draw += DrawUI;
        this.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;

        this.MainWindow.IsOpen = this.Configuration.BarOpen;
    }

    public void Dispose() {
        this.WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();
        MainWindow.Dispose();
        this.Hooks.Dispose();

        this.CommandManager.RemoveHandler(ToggleTimerCommand);
    }

    private void OnCommand(string command, string args) {
        switch (command) {
            case "/autotimer":
                this.MainWindow.IsOpen = !this.MainWindow.IsOpen;
                this.Configuration.BarOpen = this.MainWindow.IsOpen;
                this.Configuration.Save();
                break;
            case "/autotimerconfig":
                this.ConfigWindow.IsOpen = true;
                this.ConfigWindow.BringToFront();
                break;
        }
    }

    private void DrawUI() {
        this.WindowSystem.Draw();
    }

    public void DrawConfigUI() {
        ConfigWindow.IsOpen = true;
    }
    
    
}
