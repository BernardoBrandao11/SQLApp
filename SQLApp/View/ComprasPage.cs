using SQLApp.Model;

namespace SQLApp.View;

public sealed class ComprasPage : PaginaBase
{
    private readonly Picker clientes = new() { Title = "Selecione o cliente", ItemDisplayBinding = new Binding(nameof(Cliente.Descricao)) };
    private readonly DatePicker data = new() { Format = "dd/MM/yyyy", MinimumDate = new DateTime(2000, 1, 1), MaximumDate = DateTime.Today, Date = DateTime.Today };
    private readonly CollectionView compras = Lista(nameof(CompraResumo.Descricao), nameof(CompraResumo.Detalhes));
    private int codigo;

    public ComprasPage() : base("Compras")
    {
        compras.SelectionChanged += (_, e) =>
        {
            if (e.CurrentSelection.FirstOrDefault() is not CompraResumo c) return;
            codigo = c.CodigoCompra; data.Date = c.DataCompra;
            clientes.SelectedItem = (clientes.ItemsSource as List<Cliente>)?.FirstOrDefault(x => x.CodigoCliente == c.CodigoCliente);
            Estado.Text = $"Compra #{codigo} · Total: {Dinheiro.Formatar(c.ValorTotalCentavos)}";
        };
        var form = new VerticalStackLayout
        {
            Spacing = 8, Children =
            {
                Dica("Cliente"), clientes, Dica("Data"), data,
                Acoes(Botao("Salvar compra", async () =>
                {
                    if (clientes.SelectedItem is not Cliente cliente) throw new ArgumentException("Selecione um cliente.");
                    var compra = new Compra { CodigoCompra = codigo, CodigoCliente = cliente.CodigoCliente, DataCompra = data.Date ?? DateTime.Today };
                    await Banco.SalvarCompraAsync(compra);
                    codigo = compra.CodigoCompra;
                    await Atualizar(); Selecionar(codigo);
                }), Botao("Nova", () => { Limpar(); return Task.CompletedTask; })),
                Botao("Ver itens", async () =>
                {
                    if (codigo == 0) throw new ArgumentException("Salve ou selecione uma compra antes de abrir os itens.");
                    await Navigation.PushAsync(new ItensPage(codigo));
                }),
                Botao("Excluir compra", async () =>
                {
                    if (codigo == 0) throw new ArgumentException("Selecione uma compra para excluir.");
                    if (!await DisplayAlertAsync("Excluir compra", "Excluir esta compra e todos os seus itens? As quantidades serão devolvidas ao estoque.", "Excluir", "Cancelar")) return;
                    await Banco.ExcluirCompraAsync(codigo); Limpar(); await Atualizar();
                }, true)
            }
        };
        var grid = new Grid
        {
            Padding = 16, RowSpacing = 10,
            RowDefinitions = { new(GridLength.Auto), new(new GridLength(3, GridUnitType.Star)), new(GridLength.Auto), new(new GridLength(2, GridUnitType.Star)) }
        };
        grid.Add(Estado, 0, 0); grid.Add(Painel(new ScrollView { Content = form }), 0, 1);
        grid.Add(Dica("Compras cadastradas"), 0, 2); grid.Add(Painel(compras), 0, 3);
        Content = grid; Limpar();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await Executar(async () =>
        {
            var clienteId = (clientes.SelectedItem as Cliente)?.CodigoCliente;
            var lista = await Banco.ClientesAsync(); clientes.ItemsSource = lista;
            clientes.SelectedItem = lista.FirstOrDefault(c => c.CodigoCliente == clienteId);
            await Atualizar();
            if (codigo > 0) Selecionar(codigo);
        });
    }
    private async Task Atualizar() => compras.ItemsSource = await Banco.ComprasAsync();
    private void Selecionar(int id) => compras.SelectedItem = (compras.ItemsSource as List<CompraResumo>)?.FirstOrDefault(c => c.CodigoCompra == id);
    private void Limpar()
    {
        codigo = 0; clientes.SelectedItem = null; data.Date = DateTime.Today; compras.SelectedItem = null;
        Estado.Text = "Nova compra";
    }
}

public sealed class ItensPage : PaginaBase
{
    private readonly int compraId;
    private int codigoItem;
    private readonly Picker produtos = new() { Title = "Selecione o produto", ItemDisplayBinding = new Binding(nameof(Produto.Descricao)) };
    private readonly Entry quantidade = Campo("Quantidade", Keyboard.Numeric, 10);
    private readonly CollectionView itens = Lista(nameof(ItemResumo.Descricao), nameof(ItemResumo.Detalhes));

    public ItensPage(int id) : base($"Itens da compra #{id}")
    {
        compraId = id;
        itens.SelectionChanged += (_, e) =>
        {
            if (e.CurrentSelection.FirstOrDefault() is not ItemResumo item) return;
            codigoItem = item.CodigoItem; quantidade.Text = item.Quantidade.ToString();
            produtos.SelectedItem = (produtos.ItemsSource as List<Produto>)?.FirstOrDefault(p => p.CodigoProduto == item.CodigoProduto);
            Estado.Text = $"Editando item #{codigoItem} · {Dinheiro.Formatar(item.ValorParcialCentavos)}";
        };
        var form = new VerticalStackLayout
        {
            Spacing = 8, Children =
            {
                Dica("Produto"), produtos, Dica("Quantidade"), quantidade,
                Acoes(Botao("Salvar item", async () =>
                {
                    if (produtos.SelectedItem is not Produto produto) throw new ArgumentException("Selecione um produto.");
                    await Banco.SalvarItemAsync(new ItemCompra
                    {
                        CodigoItem = codigoItem, CodigoCompra = compraId,
                        CodigoProduto = produto.CodigoProduto, Quantidade = Inteiro(quantidade.Text, "Quantidade")
                    });
                    Limpar(); await Atualizar();
                }), Botao("Novo", () => { Limpar(); return Task.CompletedTask; })),
                Botao("Excluir item", async () =>
                {
                    if (codigoItem == 0) throw new ArgumentException("Selecione um item para excluir.");
                    if (!await DisplayAlertAsync("Excluir item", "Excluir este item e devolver sua quantidade ao estoque?", "Excluir", "Cancelar")) return;
                    await Banco.ExcluirItemAsync(codigoItem); Limpar(); await Atualizar();
                }, true)
            }
        };
        var grid = new Grid
        {
            Padding = 16, RowSpacing = 10,
            RowDefinitions = { new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto), new(GridLength.Star) }
        };
        grid.Add(Estado, 0, 0); grid.Add(Painel(new ScrollView { Content = form }), 0, 1);
        grid.Add(Dica("Itens"), 0, 2); grid.Add(Painel(itens), 0, 3);
        Content = grid; Limpar();
    }

    protected override async void OnAppearing() { base.OnAppearing(); await Executar(Atualizar); }
    private async Task Atualizar()
    {
        var produtoId = (produtos.SelectedItem as Produto)?.CodigoProduto;
        produtos.ItemsSource = await Banco.ProdutosAsync();
        produtos.SelectedItem = (produtos.ItemsSource as List<Produto>)?.FirstOrDefault(p => p.CodigoProduto == produtoId);
        var lista = await Banco.ItensAsync(compraId); itens.ItemsSource = lista;
        if (codigoItem > 0) itens.SelectedItem = lista.FirstOrDefault(i => i.CodigoItem == codigoItem);
        Estado.Text = $"Compra #{compraId} · {lista.Count} item(ns) · Total: {Dinheiro.Formatar(lista.Sum(i => i.ValorParcialCentavos))}";
    }
    private void Limpar() { codigoItem = 0; produtos.SelectedItem = null; quantidade.Text = "1"; itens.SelectedItem = null; Estado.Text = "Novo item"; }
}
