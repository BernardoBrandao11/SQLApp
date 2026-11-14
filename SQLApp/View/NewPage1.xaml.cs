
using SQLApp.Model;
using SQLApp.ModelView;
using System.Collections.ObjectModel;

namespace SQLApp.View;

public partial class NewPage1 : ContentPage
{
    CPessoaService _pessoaService;

    ObservableCollection<CPessoa> _ListaPessoas = 
        new ObservableCollection<CPessoa>();

    public NewPage1()
	{
		InitializeComponent();
        
        _pessoaService = new CPessoaService();

        lvListaPessoas.ItemsSource = _ListaPessoas;
    }

    protected async override void OnAppearing()
    {
        try
        {
            _ListaPessoas.Clear();
            List<CPessoa> aux = await _pessoaService.GetPessoas();
            aux.ForEach(i => _ListaPessoas.Add(i));
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Erro", ex.Message, "OK");
        }
    }

    private async void btConsultar_Clicked(object sender, EventArgs e)
    {
        CPessoa p = new CPessoa();

        if (string.IsNullOrEmpty(txID.Text))
        {
            await DisplayAlertAsync("Erro", "ID é obrigatório para consulta.", "OK");
            return;
        }

        await _pessoaService.ConsultarID(txID.Text).ContinueWith(async t =>
        {
            if (t.IsCompletedSuccessfully && t.Result.Count > 0)
                p = t.Result[0];
            else
            {
                await DisplayAlertAsync("Erro", "Pessoa não encontrada.", "OK");
                return;
            }
        });

        txNome.Text = p.Nome;
        txEmail.Text = p.Email;
    }

    private void btLimpar_Clicked(object sender, EventArgs e)
    {
        txID.Text = string.Empty;
        txNome.Text = string.Empty;
        txEmail.Text = string.Empty;
        lvListaPessoas.SelectedItem = null;
        txNome.Focus();
    }

    private async void btIncluir_Clicked(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(txNome.Text) || string.IsNullOrEmpty(txEmail.Text))
        {
            await DisplayAlertAsync("Erro", "Nome e Email são obrigatórios.", "OK");
            return;
        }

        CPessoa p = new CPessoa
        {
            Nome = txNome.Text,
            Email = txEmail.Text
        };

        await _pessoaService.InsertPessoa(p).ContinueWith(async t =>
        {
            if (t.IsCompletedSuccessfully)
                await DisplayAlertAsync("Sucesso", "Pessoa incluída com sucesso.", "OK");
            else
                await DisplayAlertAsync("Erro", "Não foi possível incluir a pessoa.", "OK");
        });

        btLimpar_Clicked(sender, e);

        rvListaPessoas.IsRefreshing = true;
    }

    private async void btAlterar_Clicked(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(txID.Text))
        {
            await DisplayAlertAsync("Erro", "ID é obrigatório para alteração.", "OK");
            return;
        }

        CPessoa p = new CPessoa
        {
            Id = Convert.ToInt32(txID.Text),
            Nome = txNome.Text,
            Email = txEmail.Text
        };

        await _pessoaService.UpdatePessoa(p).ContinueWith(async t =>
        {
            if (t.IsCompletedSuccessfully)
                await DisplayAlertAsync("Sucesso", "Pessoa alterada com sucesso.", "OK");
            else
                await DisplayAlertAsync("Erro", "Não foi possível alterar a pessoa.", "OK");
        });

        btLimpar_Clicked(sender, e);

        rvListaPessoas.IsRefreshing = true;
    }

    private async void btExcluir_Clicked(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(txID.Text))
        {
            await DisplayAlertAsync("Erro", "ID é obrigatório para deletar.", "OK");
            return;
        }

        CPessoa p = new CPessoa
        {
            Id = Convert.ToInt32(txID.Text),
        };

        await _pessoaService.DeletePessoa(p).ContinueWith(async t =>
        {
            if (t.IsCompletedSuccessfully)
                await DisplayAlertAsync("Sucesso", "Pessoa excluída com sucesso.", "OK");
            else
                await DisplayAlertAsync("Erro", "Não foi possível excluir a pessoa.", "OK");
        });

        btLimpar_Clicked(sender, e);

        rvListaPessoas.IsRefreshing = true;
    }

    private void pkOrdem_SelectedIndexChanged(object sender, EventArgs e)
    {
        var picker = (Picker)sender;

        if (picker.SelectedIndex == 0)
            _pessoaService.OdernarPorNome(true);
        else if (picker.SelectedIndex == 1)
            _pessoaService.OdernarPorNome(false);

        lvListaPessoas.ItemsSource = _pessoaService.PessoasFiltradas;
    }

    private async void sbBuscaPessoa_TextChanged(object sender, TextChangedEventArgs e)
    {
        try
        {
            rvListaPessoas.IsRefreshing = false;

            string a = e.OldTextValue; 
            string b = e.NewTextValue;

            _ListaPessoas.Clear();

            List<CPessoa> aux = await _pessoaService.ConsultarNome(b);

            aux.ForEach(i => _ListaPessoas.Add(i));

            lvListaPessoas.ItemsSource = _ListaPessoas;
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Erro", ex.Message, "OK");
        }

        rvListaPessoas.IsRefreshing = false; 
    }

    private async void rvListaPessoas_Refreshing(object sender, EventArgs e)
    {
        try
        {
            _ListaPessoas.Clear();
            List<CPessoa> aux = await _pessoaService.GetPessoas();
            aux.ForEach(i => _ListaPessoas.Add(i));
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Erro", ex.Message, "OK");
        }

        rvListaPessoas.IsRefreshing = false;
    }

    private void lvListaPessoas_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var p = e.CurrentSelection.FirstOrDefault() as CPessoa;

        if (p != null)
        {
            txID.Text = p.Id.ToString();
            txNome.Text = p.Nome;
            txEmail.Text = p.Email;
            txNome.Focus();
        }

    }
}