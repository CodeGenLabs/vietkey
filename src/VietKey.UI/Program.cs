using VietKey.Core.Config;

namespace VietKey.UI;

public static class Program
{
    public static int Main(string[] args)
    {
        var store = new ConfigStore();
        var app = new App(store, processExitCallback: () => { });
        if (args.Contains("--verify-close"))
        {
            app.MainWindow.CloseAndFlush();
        }
        return 0;
    }
}
