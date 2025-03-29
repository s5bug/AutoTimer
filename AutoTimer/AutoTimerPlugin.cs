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

    private IDalamudPluginInterface PluginInterface { get; init; }
    private ICommandManager CommandManager { get; init; }
    public Configuration Configuration { get; init; }
    public WindowSystem WindowSystem = new("AutoTimer");

    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }

    public AutoTimerHooksListener HooksListener { get; init; }
    private Hooks Hooks { get; init; }

    public AutoCalculator AutoCalculator { get; init; }

    public AutoTimerPlugin(
        IDalamudPluginInterface pluginInterface,
        ICommandManager commandManager,
        ISigScanner sigScanner,
        IGameInteropProvider gameInteropProvider,
        IClientState clientState,
        ICondition condition,
        IDataManager dataManager,
        IFramework framework,
        ITextureProvider textureProvider
    ) {
        this.PluginInterface = pluginInterface;
        this.CommandManager = commandManager;

        this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        this.Configuration.Initialize(this.PluginInterface);

        this.HooksListener = new AutoTimerHooksListener(clientState, dataManager);
        this.Hooks = new Hooks(this.HooksListener, sigScanner, gameInteropProvider);
        this.Hooks.Enable();

        framework.Update += this.HooksListener.FrameworkUpdate;

        this.AutoCalculator = new AutoCalculator(clientState, dataManager);

        // you might normally want to embed resources and load them from the manifest stream
        var gaugePath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "autoattack_gauge.png");
        var gaugeImage =
            textureProvider.CreateFromImageAsync(File.OpenRead(gaugePath));

        // autoattack_gauge_label.png
        var gaugeLabelPath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "autoattack_gauge_label.png");
        var gaugeLabelImage =
            textureProvider.CreateFromImageAsync(File.OpenRead(gaugeLabelPath));

        // TODO I need to make loading these images not completely terrible
        var gaugeMonkPath = Path.Combine(this.PluginInterface.AssemblyLocation.Directory?.FullName!,
                                         "autoattack_gauge_monk.png");
        var gaugeMonkImage =
            textureProvider.CreateFromImageAsync(File.OpenRead(gaugeMonkPath));

        var gaugeNinjaPath = Path.Combine(this.PluginInterface.AssemblyLocation.Directory?.FullName!,
                                         "autoattack_gauge_ninja.png");
        var gaugeNinjaImage =
            textureProvider.CreateFromImageAsync(File.OpenRead(gaugeNinjaPath));

        var progressPath =
            Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "autoattack_progress.png");
        var progressImage =
            textureProvider.CreateFromImageAsync(File.OpenRead(progressPath));

        var tcjProgressPath =
            Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "tcj_progress.png");
        var tcjProgressImage =
            textureProvider.CreateFromImageAsync(File.OpenRead(tcjProgressPath));

        ConfigWindow = new ConfigWindow(this);
        MainWindow = new MainWindow(
            this,
            clientState,
            condition,
            gaugeImage.Result,
            gaugeMonkImage.Result,
            gaugeNinjaImage.Result,
            gaugeLabelImage.Result,
            progressImage.Result,
            tcjProgressImage.Result
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
                this.Configuration.BarOpen = !this.Configuration.BarOpen;
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
