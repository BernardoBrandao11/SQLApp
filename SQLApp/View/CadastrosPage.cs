using SQLApp.Model;

namespace SQLApp.View;

public abstract class CadastroPage<T> : PaginaBase where T : class
{
    protected readonly VerticalStackLayout Formulario = new() { Spacing = 6 };
    protected readonly CollectionView Registros;
    protected readonly SearchBar Busca = new() { Placeholder = "Buscar por nome" };
    protected int codigo;
    private int buscaVersao;

    protected CadastroPage(string titulo, string principal, string detalhes) : base(titulo)
    {
        Registros = Lista(principal, detalhes);
        Registros.SelectionChanged += (_, e) =>
        {
            if (e.CurrentSelection.FirstOrDefault() is T registro) Preencher(registro);
        };
        Busca.TextChanged += async (_, _) =>
        {
            var versao = ++buscaVersao;
            await Task.Delay(250);
            if (versao == buscaVersao) await Executar(Atualizar);
        };
        var layout = new Grid
        {
            Padding = 16, RowSpacing = 8,
            RowDefinitions = { new(GridLength.Auto), new(new GridLength(3, GridUnitType.Star)), new(GridLength.Auto), new(GridLength.Auto), new(new GridLength(2, GridUnitType.Star)) }
        };
        layout.Add(Estado, 0, 0);
        layout.Add(Painel(new ScrollView { Content = Formulario }), 0, 1);
        layout.Add(Acoes(Botao("Salvar", async () =>
        {
            await Salvar(); Limpar(); await Atualizar(); Estado.Text = "Salvo.";
        }), Botao("Novo", () => { Limpar(); return Task.CompletedTask; }), Botao("Excluir", async () =>
        {
            if (codigo == 0) throw new ArgumentException("Selecione um registro na lista para excluir.");
            if (!await DisplayAlertAsync("Excluir registro", "Confirma a exclusão do registro selecionado?", "Excluir", "Cancelar")) return;
            await Excluir(); Limpar(); await Atualizar();
        }, true)), 0, 2);
        layout.Add(Busca, 0, 3); layout.Add(Painel(Registros), 0, 4);
        Content = layout;
    }

    protected override async void OnAppearing() { base.OnAppearing(); await Executar(Atualizar); }
    protected abstract Task Atualizar();
    protected abstract Task Salvar();
    protected abstract Task Excluir();
    protected abstract void Preencher(T registro);
    protected abstract void LimparCampos();
    protected void Limpar()
    {
        codigo = 0; Registros.SelectedItem = null; LimparCampos();
        Estado.Text = "Novo cadastro";
    }
}

public sealed class ClientesPage : CadastroPage<Cliente>
{
    private readonly Entry nome = Campo("Nome completo", limite: 50);
    private readonly Entry rg = Campo("RG", Keyboard.Numeric, 10);
    private readonly Entry telefone = Campo("Telefone com DDD", Keyboard.Telephone, 14);

    public ClientesPage() : base("Clientes", nameof(Cliente.Descricao), nameof(Cliente.Telefone))
    {
        Formulario.Children.Add(Dica("Nome")); Formulario.Children.Add(nome);
        Formulario.Children.Add(Dica("RG")); Formulario.Children.Add(rg);
        Formulario.Children.Add(Dica("Telefone")); Formulario.Children.Add(telefone);
        Limpar();
    }
    protected override async Task Atualizar() => Registros.ItemsSource = await Banco.ClientesAsync(Busca.Text ?? "");
    protected override Task Salvar() => Banco.SalvarClienteAsync(new Cliente
    {
        CodigoCliente = codigo, Nome = nome.Text ?? "", RG = rg.Text ?? "", Telefone = telefone.Text ?? ""
    });
    protected override Task Excluir() => Banco.ExcluirClienteAsync(codigo);
    protected override void Preencher(Cliente c)
    {
        codigo = c.CodigoCliente; nome.Text = c.Nome; rg.Text = c.RG; telefone.Text = c.Telefone;
        Estado.Text = $"Editando cliente #{codigo}";
    }
    protected override void LimparCampos() { nome.Text = ""; rg.Text = ""; telefone.Text = ""; }
}

public sealed class ProdutosPage : CadastroPage<Produto>
{
    private readonly Entry nome = Campo("Nome do produto", limite: 50);
    private readonly Entry tipo = Campo("Tipo", limite: 50);
    private readonly Entry preco = Campo("0,00", Keyboard.Numeric, 20);
    private readonly Entry estoque = Campo("0", Keyboard.Numeric, 10);

    public ProdutosPage() : base("Produtos", nameof(Produto.Descricao), nameof(Produto.Tipo))
    {
        Formulario.Children.Add(Dica("Nome")); Formulario.Children.Add(nome);
        Formulario.Children.Add(Dica("Tipo")); Formulario.Children.Add(tipo);
        Formulario.Children.Add(Dica("Preço (R$)")); Formulario.Children.Add(preco);
        Formulario.Children.Add(Dica("Estoque")); Formulario.Children.Add(estoque);
        Limpar();
    }
    protected override async Task Atualizar() => Registros.ItemsSource = await Banco.ProdutosAsync(Busca.Text ?? "");
    protected override Task Salvar() => Banco.SalvarProdutoAsync(new Produto
    {
        CodigoProduto = codigo, NomeProduto = nome.Text ?? "", Tipo = tipo.Text ?? "",
        PrecoCentavos = Dinheiro.Ler(preco.Text), QtdEstoque = Inteiro(estoque.Text, "Estoque", true)
    });
    protected override Task Excluir() => Banco.ExcluirProdutoAsync(codigo);
    protected override void Preencher(Produto p)
    {
        codigo = p.CodigoProduto; nome.Text = p.NomeProduto; tipo.Text = p.Tipo;
        preco.Text = (p.PrecoCentavos / 100m).ToString("F2", Dinheiro.Cultura); estoque.Text = p.QtdEstoque.ToString();
        Estado.Text = $"Editando produto #{codigo}";
    }
    protected override void LimparCampos() { nome.Text = ""; tipo.Text = ""; preco.Text = ""; estoque.Text = "0"; }
}
