using System.Threading;
using System.Threading.Tasks;
using Dalamud.Plugin;
using KamiToolKit;

namespace WondrousTailsSolver;

public sealed class WondrousTailsSolverPlugin(IDalamudPluginInterface pluginInterface) : IAsyncDalamudPlugin {
    private AddonWeeklyBingoController? addonController;
    private bool kamiToolKitInitialized;

    public async Task LoadAsync(CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();

        System.PerfectTails = new PerfectTails();
        await KamiToolKitLibrary.InitializeAsync(pluginInterface);
        kamiToolKitInitialized = true;

        cancellationToken.ThrowIfCancellationRequested();
        addonController = new AddonWeeklyBingoController();
        await addonController.EnableAsync();
    }

    public async ValueTask DisposeAsync() {
        if (addonController is not null) {
            await addonController.DisposeAsync();
        }

        if (kamiToolKitInitialized) {
            await KamiToolKitLibrary.DisposeAsync();
        }
    }
}
