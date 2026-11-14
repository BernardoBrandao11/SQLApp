using SQLite;
using System.Globalization;

namespace SQLApp.Model;

[Table("Cliente")]
public class Cliente
{
    [PrimaryKey, AutoIncrement] public int CodigoCliente { get; set; }
    public string Nome { get; set; } = "";
    public string RG { get; set; } = "";
    public string Telefone { get; set; } = "";
    [Ignore] public string Descricao => $"#{CodigoCliente} · {Nome}";
}

[Table("Produto")]
public class Produto
{
    [PrimaryKey, AutoIncrement] public int CodigoProduto { get; set; }
    public string NomeProduto { get; set; } = "";
    public string Tipo { get; set; } = "";
    [Column("Preco")] public long PrecoCentavos { get; set; }
    public int QtdEstoque { get; set; }
    [Ignore] public string Descricao => $"#{CodigoProduto} · {NomeProduto} · {Dinheiro.Formatar(PrecoCentavos)} · estoque: {QtdEstoque}";
}

[Table("Compra")]
public class Compra
{
    [PrimaryKey, AutoIncrement] public int CodigoCompra { get; set; }
    public DateTime DataCompra { get; set; }
    [Column("ValorTotal")] public long ValorTotalCentavos { get; set; }
    [Indexed] public int CodigoCliente { get; set; }
}

[Table("ItemCompra")]
public class ItemCompra
{
    [PrimaryKey, AutoIncrement] public int CodigoItem { get; set; }
    [Indexed] public int CodigoProduto { get; set; }
    [Indexed] public int CodigoCompra { get; set; }
    public int Quantidade { get; set; }
    [Column("ValorParcial")] public long ValorParcialCentavos { get; set; }
}

public static class Dinheiro
{
    public static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("pt-BR");
    public static string Formatar(long centavos) => (centavos / 100m).ToString("C2", Cultura);
    public static long Ler(string? texto)
    {
        if (!decimal.TryParse(texto, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, Cultura, out var valor)
            || valor <= 0 || decimal.Round(valor, 2) != valor || valor > 1_000_000_000m)
            throw new ArgumentException("Informe um preço positivo, até 1 bilhão, com no máximo duas casas decimais (ex.: 19,90)." );
        return checked((long)(valor * 100));
    }
}

public class CompraResumo : Compra
{
    public string NomeCliente { get; set; } = "";
    [Ignore] public string Descricao => $"Compra #{CodigoCompra} · {NomeCliente}";
    [Ignore] public string Detalhes => $"{DataCompra:dd/MM/yyyy} · {Dinheiro.Formatar(ValorTotalCentavos)}";
}

public class ItemResumo : ItemCompra
{
    public string NomeProduto { get; set; } = "";
    [Ignore] public string Descricao => $"Item #{CodigoItem} · {NomeProduto}";
    [Ignore] public string Detalhes => $"Quantidade: {Quantidade} · Subtotal: {Dinheiro.Formatar(ValorParcialCentavos)}";
}
