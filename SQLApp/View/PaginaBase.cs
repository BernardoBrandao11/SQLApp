using SQLApp.ModelView;

namespace SQLApp.View;

public abstract class PaginaBase : ContentPage
{
    protected static LojaService Banco => App.Banco;
    private bool ocupado;
    private readonly List<Button> botoes = new();
    protected static Color Cor(string chave) => (Color)Application.Current!.Resources[chave];
    protected readonly Label Estado = new() { FontSize = 14, FontAttributes = FontAttributes.Bold, TextColor = Cor("TextPrimary") };

    protected PaginaBase(string titulo) { Title = titulo; }

    protected async Task Executar(Func<Task> acao)
    {
        if (ocupado) return;
        ocupado = true;
        foreach (var botao in botoes) botao.IsEnabled = false;
        try { await acao(); }
        catch (ArgumentException ex) { await DisplayAlertAsync("Confira os dados", ex.Message, "OK"); }
        catch (Exception ex) { await DisplayAlertAsync("Não foi possível concluir", ex.Message, "OK"); }
        finally
        {
            foreach (var botao in botoes) botao.IsEnabled = true;
            ocupado = false;
        }
    }

    protected static Label Titulo(string texto) => new() { Text = texto, FontSize = 24, FontAttributes = FontAttributes.Bold, TextColor = Cor("TextPrimary") };
    protected static Label Dica(string texto) => new() { Text = texto, FontSize = 14, TextColor = Cor("TextSecondary") };
    protected static Border Painel(Microsoft.Maui.Controls.View conteudo) => new()
    {
        Content = conteudo, Padding = 12, BackgroundColor = Cor("Surface"),
        Stroke = new SolidColorBrush(Cor("Outline")), StrokeThickness = 1,
        StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 }
    };
    protected static Entry Campo(string dica, Keyboard? teclado = null, int limite = 100) =>
        new() { Placeholder = dica, Keyboard = teclado ?? Keyboard.Default, MaxLength = limite };

    protected Button Botao(string texto, Func<Task> acao, bool perigo = false)
    {
        var botao = new Button { Text = texto, BackgroundColor = perigo ? Color.FromArgb("B42335") : Color.FromArgb("2457A6"), TextColor = Colors.White };
        botoes.Add(botao);
        botao.Clicked += async (_, _) => await Executar(acao);
        return botao;
    }

    protected static Grid Acoes(params Microsoft.Maui.Controls.View[] views)
    {
        var grid = new Grid { ColumnSpacing = 8 };
        foreach (var view in views)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            grid.Add(view, grid.ColumnDefinitions.Count - 1);
        }
        return grid;
    }

    protected static CollectionView Lista(string principal, string detalhes) => new()
    {
        SelectionMode = SelectionMode.Single,
        BackgroundColor = Cor("Surface"),
        EmptyView = new Label { Text = "Nenhum registro.", Margin = new Thickness(12), TextColor = Cor("TextSecondary") },
        ItemTemplate = new DataTemplate(() =>
        {
            var nome = new Label { FontAttributes = FontAttributes.Bold, TextColor = Cor("TextPrimary"), LineBreakMode = LineBreakMode.WordWrap };
            nome.SetBinding(Label.TextProperty, principal);
            var info = new Label { FontSize = 14, TextColor = Cor("TextSecondary"), LineBreakMode = LineBreakMode.WordWrap };
            info.SetBinding(Label.TextProperty, detalhes);
            var linha = new VerticalStackLayout
            {
                Padding = new Thickness(8, 12), Spacing = 4,
                Children = { nome, info, new BoxView { HeightRequest = 1, Color = Cor("Outline") } }
            };
            var estados = new VisualStateGroup { Name = "CommonStates" };
            var normal = new VisualState { Name = "Normal" };
            normal.Setters.Add(new Setter { Property = BackgroundColorProperty, Value = Cor("Surface") });
            var selecionado = new VisualState { Name = "Selected" };
            selecionado.Setters.Add(new Setter { Property = BackgroundColorProperty, Value = Cor("SelectionBackground") });
            estados.States.Add(normal); estados.States.Add(selecionado);
            VisualStateManager.SetVisualStateGroups(linha, new VisualStateGroupList { estados });
            return linha;
        })
    };

    protected static int Inteiro(string? texto, string campo, bool permiteZero = false)
    {
        if (!int.TryParse(texto, out var valor) || valor < (permiteZero ? 0 : 1))
            throw new ArgumentException($"{campo} deve ser um número inteiro {(permiteZero ? "maior ou igual a zero" : "positivo")}.");
        return valor;
    }
}
