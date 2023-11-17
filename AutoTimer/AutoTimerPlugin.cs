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
    private const string CommandName = "/autotimer";

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
        [RequiredVersion("1.0")] IDataManager dataManager
    ) {
        this.PluginInterface = pluginInterface;
        this.CommandManager = commandManager;

        this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        this.Configuration.Initialize(this.PluginInterface);

        this.HooksListener = new AutoTimerHooksListener(clientState);
        this.Hooks = new Hooks(this.HooksListener, sigScanner, gameInteropProvider);
        this.Hooks.Enable();

        this.AutoCalculator = new AutoCalculator(clientState, dataManager);

        // you might normally want to embed resources and load them from the manifest stream
        var gaugePath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "autoattack_gauge.png");
        var gaugeImage = this.PluginInterface.UiBuilder.LoadImage(gaugePath);

        var gaugeMonkPath = Path.Combine(this.PluginInterface.AssemblyLocation.Directory?.FullName!,
                                         "autoattack_gauge_monk.png");
        var gaugeMonkImage = this.PluginInterface.UiBuilder.LoadImage(gaugeMonkPath);

        var progressPath =
            Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "autoattack_progress.png");
        var progressImage = this.PluginInterface.UiBuilder.LoadImage(progressPath);

        ConfigWindow = new ConfigWindow(this);
        MainWindow = new MainWindow(this, gaugeImage, gaugeMonkImage, progressImage);

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);

        this.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand) {
            HelpMessage = "A useful message to display in /xlhelp"
        });

        this.PluginInterface.UiBuilder.Draw += DrawUI;
        this.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;

        this.MainWindow.IsOpen = true;
    }

    public void Dispose() {
        this.WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();
        MainWindow.Dispose();
        this.Hooks.Dispose();

        this.CommandManager.RemoveHandler(CommandName);
    }

    private void OnCommand(string command, string args) {
        // in response to the slash command, just display our main ui
        MainWindow.IsOpen = true;
    }

    private void DrawUI() {
        this.WindowSystem.Draw();
    }

    public void DrawConfigUI() {
        ConfigWindow.IsOpen = true;
    }
}
