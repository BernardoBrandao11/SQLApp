using SQLite;
using SQLApp.Model;

namespace SQLApp.ModelView;

public sealed class LojaService
{
    private readonly SQLiteAsyncConnection db;
    private readonly SemaphoreSlim inicializacao = new(1, 1);
    private bool pronto;

    public LojaService(string caminho)
    {
        db = new SQLiteAsyncConnection(caminho,
            SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.FullMutex);
    }

    public async Task InicializarAsync()
    {
        await inicializacao.WaitAsync();
        try
        {
            if (pronto) return;
            await db.ExecuteAsync("PRAGMA foreign_keys = ON");
            await db.ExecuteAsync("""
                CREATE TABLE IF NOT EXISTS Cliente (
                    CodigoCliente INTEGER PRIMARY KEY AUTOINCREMENT,
                    Nome TEXT NOT NULL CHECK(length(trim(Nome)) BETWEEN 1 AND 50),
                    RG TEXT NOT NULL CHECK(length(RG) BETWEEN 1 AND 10),
                    Telefone TEXT NOT NULL CHECK(length(Telefone) BETWEEN 1 AND 14))
                """);
            await db.ExecuteAsync("""
                CREATE TABLE IF NOT EXISTS Produto (
                    CodigoProduto INTEGER PRIMARY KEY AUTOINCREMENT,
                    NomeProduto TEXT NOT NULL CHECK(length(trim(NomeProduto)) BETWEEN 1 AND 50),
                    Tipo TEXT NOT NULL CHECK(length(trim(Tipo)) BETWEEN 1 AND 50),
                    Preco INTEGER NOT NULL CHECK(Preco > 0),
                    QtdEstoque INTEGER NOT NULL CHECK(QtdEstoque >= 0))
                """);
            await db.ExecuteAsync("""
                CREATE TABLE IF NOT EXISTS Compra (
                    CodigoCompra INTEGER PRIMARY KEY AUTOINCREMENT,
                    DataCompra BIGINT NOT NULL,
                    ValorTotal INTEGER NOT NULL DEFAULT 0 CHECK(ValorTotal >= 0),
                    CodigoCliente INTEGER NOT NULL REFERENCES Cliente(CodigoCliente) ON DELETE RESTRICT)
                """);
            await db.ExecuteAsync("""
                CREATE TABLE IF NOT EXISTS ItemCompra (
                    CodigoItem INTEGER PRIMARY KEY AUTOINCREMENT,
                    CodigoProduto INTEGER NOT NULL REFERENCES Produto(CodigoProduto) ON DELETE RESTRICT,
                    CodigoCompra INTEGER NOT NULL REFERENCES Compra(CodigoCompra) ON DELETE CASCADE,
                    Quantidade INTEGER NOT NULL CHECK(Quantidade > 0),
                    ValorParcial INTEGER NOT NULL CHECK(ValorParcial > 0))
                """);
            await db.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_Compra_Cliente ON Compra(CodigoCliente)");
            await db.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_Item_Compra ON ItemCompra(CodigoCompra)");
            await db.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_Item_Produto ON ItemCompra(CodigoProduto)");
            pronto = true;
        }
        finally { inicializacao.Release(); }
    }

    private static void Exigir(bool condicao, string mensagem)
    {
        if (!condicao) throw new ArgumentException(mensagem);
    }

    private static void Texto(string? valor, int limite, string campo) =>
        Exigir(!string.IsNullOrWhiteSpace(valor) && valor.Trim().Length <= limite,
            $"{campo} é obrigatório e deve ter até {limite} caracteres.");

    private static void Id(int id) => Exigir(id > 0, "Selecione um registro válido.");

    public async Task<List<Cliente>> ClientesAsync(string filtro = "")
    {
        await InicializarAsync();
        return await db.QueryAsync<Cliente>("SELECT * FROM Cliente WHERE instr(lower(Nome), lower(?)) > 0 ORDER BY Nome COLLATE NOCASE", filtro);
    }

    public async Task SalvarClienteAsync(Cliente c)
    {
        Texto(c.Nome, 50, "Nome"); Texto(c.RG, 10, "RG"); Texto(c.Telefone, 14, "Telefone");
        Exigir(c.RG.All(char.IsAsciiDigit), "RG deve conter somente números, até 10 dígitos.");
        Exigir(c.Telefone.Count(char.IsAsciiDigit) is >= 8 and <= 11
            && c.Telefone.All(ch => char.IsAsciiDigit(ch) || " ()-+".Contains(ch)),
            "Telefone deve conter de 8 a 11 dígitos e até 14 caracteres com a formatação.");
        Exigir(c.CodigoCliente >= 0, "Código de cliente inválido.");
        c.Nome = c.Nome.Trim(); c.Telefone = c.Telefone.Trim();
        await InicializarAsync();
        if (c.CodigoCliente == 0) await db.InsertAsync(c);
        else Exigir(await db.UpdateAsync(c) == 1, "Cliente não encontrado.");
    }

    public async Task ExcluirClienteAsync(int id)
    {
        Id(id); await InicializarAsync();
        await db.RunInTransactionAsync(c =>
        {
            Exigir(c.Find<Cliente>(id) != null, "Cliente não encontrado.");
            Exigir(c.ExecuteScalar<int>("SELECT COUNT(*) FROM Compra WHERE CodigoCliente = ?", id) == 0,
                "Este cliente possui compras. Exclua as compras antes de excluir o cliente.");
            c.Delete<Cliente>(id);
        });
    }

    public async Task<List<Produto>> ProdutosAsync(string filtro = "")
    {
        await InicializarAsync();
        return await db.QueryAsync<Produto>("SELECT * FROM Produto WHERE instr(lower(NomeProduto), lower(?)) > 0 ORDER BY NomeProduto COLLATE NOCASE", filtro);
    }

    public async Task SalvarProdutoAsync(Produto p)
    {
        Texto(p.NomeProduto, 50, "Nome do produto"); Texto(p.Tipo, 50, "Tipo");
        Exigir(p.PrecoCentavos is > 0 and <= 100_000_000_000, "Preço deve ser positivo e até 1 bilhão.");
        Exigir(p.QtdEstoque >= 0, "Estoque deve ser um número inteiro maior ou igual a zero.");
        Exigir(p.CodigoProduto >= 0, "Código de produto inválido.");
        p.NomeProduto = p.NomeProduto.Trim(); p.Tipo = p.Tipo.Trim();
        await InicializarAsync();
        if (p.CodigoProduto == 0) await db.InsertAsync(p);
        else Exigir(await db.UpdateAsync(p) == 1, "Produto não encontrado.");
    }

    public async Task ExcluirProdutoAsync(int id)
    {
        Id(id); await InicializarAsync();
        await db.RunInTransactionAsync(c =>
        {
            Exigir(c.Find<Produto>(id) != null, "Produto não encontrado.");
            Exigir(c.ExecuteScalar<int>("SELECT COUNT(*) FROM ItemCompra WHERE CodigoProduto = ?", id) == 0,
                "Este produto possui itens de compra. Exclua os itens antes de excluir o produto.");
            c.Delete<Produto>(id);
        });
    }

    public async Task<List<CompraResumo>> ComprasAsync()
    {
        await InicializarAsync();
        return await db.QueryAsync<CompraResumo>("""
            SELECT co.*, cl.Nome AS NomeCliente FROM Compra co
            JOIN Cliente cl ON cl.CodigoCliente = co.CodigoCliente
            ORDER BY co.DataCompra DESC, co.CodigoCompra DESC
            """);
    }

    public async Task SalvarCompraAsync(Compra compra)
    {
        Id(compra.CodigoCliente);
        Exigir(compra.CodigoCompra >= 0, "Código de compra inválido.");
        Exigir(compra.DataCompra.Date >= new DateTime(2000, 1, 1) && compra.DataCompra.Date <= DateTime.Today,
            "Data da compra deve estar entre 01/01/2000 e hoje.");
        compra.DataCompra = compra.DataCompra.Date;
        await InicializarAsync();
        await db.RunInTransactionAsync(c =>
        {
            Exigir(c.Find<Cliente>(compra.CodigoCliente) != null, "Cliente não encontrado.");
            if (compra.CodigoCompra == 0)
            {
                compra.ValorTotalCentavos = 0;
                c.Insert(compra);
            }
            else
            {
                var atual = c.Find<Compra>(compra.CodigoCompra);
                Exigir(atual != null, "Compra não encontrada.");
                compra.ValorTotalCentavos = atual!.ValorTotalCentavos;
                c.Update(compra);
            }
        });
    }

    public async Task<List<ItemResumo>> ItensAsync(int compraId)
    {
        Id(compraId); await InicializarAsync();
        return await db.QueryAsync<ItemResumo>("""
            SELECT i.*, p.NomeProduto FROM ItemCompra i
            JOIN Produto p ON p.CodigoProduto = i.CodigoProduto
            WHERE i.CodigoCompra = ? ORDER BY i.CodigoItem
            """, compraId);
    }

    private static void Recalcular(SQLiteConnection c, int id) => c.Execute("""
        UPDATE Compra SET ValorTotal = (SELECT COALESCE(SUM(ValorParcial), 0)
        FROM ItemCompra WHERE CodigoCompra = ?) WHERE CodigoCompra = ?
        """, id, id);

    private static void DevolverEstoque(SQLiteConnection c, ItemCompra item)
    {
        var p = c.Find<Produto>(item.CodigoProduto)!;
        p.QtdEstoque = checked(p.QtdEstoque + item.Quantidade);
        c.Update(p);
    }

    public async Task SalvarItemAsync(ItemCompra item)
    {
        Id(item.CodigoCompra); Id(item.CodigoProduto);
        Exigir(item.CodigoItem >= 0, "Código de item inválido.");
        Exigir(item.Quantidade > 0, "Quantidade deve ser um número inteiro positivo.");
        await InicializarAsync();
        await db.RunInTransactionAsync(c =>
        {
            Exigir(c.Find<Compra>(item.CodigoCompra) != null, "Compra não encontrada.");
            ItemCompra? anterior = null;
            if (item.CodigoItem > 0)
            {
                anterior = c.Find<ItemCompra>(item.CodigoItem);
                Exigir(anterior != null && anterior.CodigoCompra == item.CodigoCompra, "Item não pertence à compra selecionada.");
                DevolverEstoque(c, anterior!);
            }
            var produto = c.Find<Produto>(item.CodigoProduto);
            Exigir(produto != null, "Produto não encontrado.");
            Exigir(produto!.QtdEstoque >= item.Quantidade, "Estoque insuficiente para a quantidade informada.");
            var preco = anterior != null && anterior.CodigoProduto == item.CodigoProduto
                ? anterior.ValorParcialCentavos / anterior.Quantidade : produto.PrecoCentavos;
            item.ValorParcialCentavos = checked(preco * item.Quantidade);
            produto.QtdEstoque -= item.Quantidade;
            c.Update(produto);
            if (item.CodigoItem == 0) c.Insert(item); else c.Update(item);
            Recalcular(c, item.CodigoCompra);
        });
    }

    public async Task ExcluirItemAsync(int id)
    {
        Id(id); await InicializarAsync();
        await db.RunInTransactionAsync(c =>
        {
            var item = c.Find<ItemCompra>(id);
            Exigir(item != null, "Item não encontrado.");
            DevolverEstoque(c, item!);
            c.Delete<ItemCompra>(id);
            Recalcular(c, item!.CodigoCompra);
        });
    }

    public async Task ExcluirCompraAsync(int id)
    {
        Id(id); await InicializarAsync();
        await db.RunInTransactionAsync(c =>
        {
            Exigir(c.Find<Compra>(id) != null, "Compra não encontrada.");
            foreach (var item in c.Table<ItemCompra>().Where(i => i.CodigoCompra == id).ToList())
                DevolverEstoque(c, item);
            c.Execute("DELETE FROM ItemCompra WHERE CodigoCompra = ?", id);
            c.Delete<Compra>(id);
        });
    }

    public Task FecharAsync() => db.CloseAsync();
}
