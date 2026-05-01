using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AutoTimer.Game;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using AutoTimer.Windows;
using Dalamud.Game;

namespace AutoTimer;

public sealed partial class AutoTimerPlugin : IAsyncDalamudPlugin {
    public string Name => "Auto Timer";
    private const string ToggleTimerCommand = "/autotimer";
    private const string ConfigTimerCommand = "/autotimerconfig";

    private IDalamudPluginInterface PluginInterface { get; init; }
    private ICommandManager CommandManager { get; init; }
    private ITextureProvider TextureProvider { get; init; }
    private IPlayerState PlayerState { get; init; }
    private ICondition Condition { get; init; }
    public Configuration Configuration { get; init; }
    public WindowSystem WindowSystem = new("AutoTimer");

    private ConfigWindow ConfigWindow { get; set; }
    private MainWindow MainWindow { get; set; }

    public AutoTimerHooksListener HooksListener { get; init; }
    private Hooks Hooks { get; init; }

    public AutoCalculator AutoCalculator { get; init; }

    public AutoTimerPlugin(
        IDalamudPluginInterface pluginInterface,
        ICommandManager commandManager,
        ISigScanner sigScanner,
        IGameInteropProvider gameInteropProvider,
        IPlayerState playerState,
        IObjectTable objectTable,
        ICondition condition,
        IDataManager dataManager,
        IFramework framework,
        ITextureProvider textureProvider
    ) {
        this.PluginInterface = pluginInterface;
        this.CommandManager = commandManager;

        this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        this.Configuration.Initialize(this.PluginInterface);

        this.HooksListener = new AutoTimerHooksListener(playerState, objectTable, dataManager);
        this.Hooks = new Hooks(this.HooksListener, sigScanner, gameInteropProvider);
        this.Hooks.Enable();

        framework.Update += this.HooksListener.FrameworkUpdate;

        this.AutoCalculator = new AutoCalculator(playerState, objectTable, dataManager);
        
        this.TextureProvider = textureProvider;
        this.PlayerState = playerState;
        this.Condition = condition;
    }

    public async Task LoadAsync(CancellationToken cancellationToken) {
        // you might normally want to embed resources and load them from the manifest stream
        var gaugePath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "autoattack_gauge.png");
        var gaugeImage =
            this.TextureProvider.CreateFromImageAsync(File.OpenRead(gaugePath), cancellationToken: cancellationToken);

        // autoattack_gauge_label.png
        var gaugeLabelPath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "autoattack_gauge_label.png");
        var gaugeLabelImage =
            this.TextureProvider.CreateFromImageAsync(File.OpenRead(gaugeLabelPath), cancellationToken: cancellationToken);

        // TODO I need to make loading these images not completely terrible
        var gaugeMonkPath = Path.Combine(this.PluginInterface.AssemblyLocation.Directory?.FullName!,
                                         "autoattack_gauge_monk.png");
        var gaugeMonkImage =
            this.TextureProvider.CreateFromImageAsync(File.OpenRead(gaugeMonkPath), cancellationToken: cancellationToken);

        var gaugeNinjaPath = Path.Combine(this.PluginInterface.AssemblyLocation.Directory?.FullName!,
                                         "autoattack_gauge_ninja.png");
        var gaugeNinjaImage =
            this.TextureProvider.CreateFromImageAsync(File.OpenRead(gaugeNinjaPath), cancellationToken: cancellationToken);

        var progressPath =
            Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "autoattack_progress.png");
        var progressImage =
            this.TextureProvider.CreateFromImageAsync(File.OpenRead(progressPath), cancellationToken: cancellationToken);

        var tcjProgressPath =
            Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "tcj_progress.png");
        var tcjProgressImage =
            this.TextureProvider.CreateFromImageAsync(File.OpenRead(tcjProgressPath), cancellationToken: cancellationToken);

        this.ConfigWindow = new ConfigWindow(this);
        this.MainWindow = new MainWindow(
            this,
            this.PlayerState,
            this.Condition,
            await gaugeImage,
            await gaugeMonkImage,
            await gaugeNinjaImage,
            await gaugeLabelImage,
            await progressImage,
            await tcjProgressImage
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

    public ValueTask DisposeAsync() {
        this.WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();
        MainWindow.Dispose();
        this.Hooks.Dispose();

        this.CommandManager.RemoveHandler(ToggleTimerCommand);
        
        return ValueTask.CompletedTask;
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
