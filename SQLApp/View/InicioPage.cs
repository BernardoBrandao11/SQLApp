namespace SQLApp.View;

public sealed class InicioPage : PaginaBase
{
    public InicioPage() : base("SQLApp")
    {
        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 24, Spacing = 18,
                Children =
                {
                    Titulo("Menu principal"),
                    Painel(new VerticalStackLayout
                    {
                        Spacing = 14,
                        Children =
                        {
                            Botao("Clientes", () => Navigation.PushAsync(new ClientesPage())),
                            Botao("Produtos", () => Navigation.PushAsync(new ProdutosPage())),
                            Botao("Compras", () => Navigation.PushAsync(new ComprasPage()))
                        }
                    })
                }
            }
        };
    }
}
