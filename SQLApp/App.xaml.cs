using Microsoft.Extensions.DependencyInjection;

namespace SQLApp
{
    public partial class App : Application
    {
        public static SQLApp.ModelView.LojaService Banco { get; } = new(Path.Combine(FileSystem.AppDataDirectory, "loja-tde.db3"));
        public App()
        {
            InitializeComponent();
            UserAppTheme = AppTheme.Light;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}
