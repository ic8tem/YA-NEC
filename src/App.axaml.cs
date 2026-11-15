using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace YaNec
{
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
                Config cfg = new Config(System.IO.Path.Combine(AppPaths.DataDir, "config.ini"));
                int schema = (int)cfg.GetD("tema", 0);
                Pal.Load(schema);
                // 0.5.3: lingua dell'interfaccia (config.ini, chiave "lingua"; al primo avvio quella del sistema se c'e')
                Lang.Init(cfg.Get("lingua", ""));
                Lang.Installa();
                desktop.MainWindow = new MainWindow(cfg);
            }
            base.OnFrameworkInitializationCompleted();
        }
    }
}
