using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Assetly.Aplicacao.Dados;
using Assetly.Dominio.Alerta;
using Assetly.Dominio.Bem;
using Assetly.Dominio.Documento;
using Assetly.Dominio.Manutencao;

namespace Assetly.Apresentacao.Visao;

internal sealed partial class TelaPrincipal
{
    private void ConcluirManutencao()
    {
        var manutencao = Selecionado<ManutencaoLinha>(_tabelaManutencoes, "Selecione uma manutenção para concluir.");
        ConcluirManutencao(manutencao.Origem);
    }

    private void ConcluirManutencao(Manutencao manutencao)
    {
        var concluidaEm = DataPicker(Hoje());
        var custo = CampoTexto("Custo", string.Empty);
        var moeda = CampoTexto("Moeda", "BRL");
        var grade = GradeFormulario();
        AdicionarLinha(grade, 0, "Concluída em", concluidaEm);
        AdicionarLinha(grade, 1, "Custo", custo);
        AdicionarLinha(grade, 2, "Moeda", moeda);

        if (MostrarFormulario("Concluir manutenção", grade, () => _contexto.ConcluirManutencao().Executar(new ConcluirManutencaoComando(
                manutencao.Id, DataObrigatoria(concluidaEm, "conclusão"), DecimalOpcional(custo.Text), TextoOpcional(moeda.Text)))))
        {
            AtualizarTudo();
        }
    }

    private void CancelarManutencao()
    {
        var manutencao = Selecionado<ManutencaoLinha>(_tabelaManutencoes, "Selecione uma manutenção para cancelar.");
        CancelarManutencao(manutencao.Origem);
    }

    private void CancelarManutencao(Manutencao manutencao)
    {
        _contexto.CancelarManutencao().Executar(manutencao.Id);
        AtualizarTudo();
    }

    private void AbrirDocumentoSelecionado()
    {
        AbrirDocumento(Selecionado<DocumentoLinha>(_tabelaDocumentos, "Selecione um documento para abrir.").Origem);
    }

    private static void AbrirDocumento(Documento documento)
    {
        if (documento.Status == StatusDocumento.Ausente)
        {
            throw new InvalidOperationException("documento marcado como ausente não possui arquivo disponível para abrir.");
        }

        var arquivo = Path.GetFullPath(documento.CaminhoLocal.Valor);
        if (!File.Exists(arquivo))
        {
            throw new InvalidOperationException($"arquivo do documento não foi encontrado: {arquivo}");
        }

        try
        {
            Process.Start(new ProcessStartInfo(arquivo) { UseShellExecute = true });
        }
        catch (Exception excecao) when (excecao is Win32Exception or InvalidOperationException)
        {
            throw new InvalidOperationException($"não foi possível abrir o documento: {arquivo}", excecao);
        }
    }

    private void MarcarDocumentoAusente()
    {
        var documento = Selecionado<DocumentoLinha>(_tabelaDocumentos, "Selecione um documento.");
        MarcarDocumentoAusente(documento.Origem);
    }

    private void MarcarDocumentoAusente(Documento documento)
    {
        _contexto.MarcarDocumentoAusente().Executar(documento.Id);
        AtualizarTudo();
    }

    private void ArquivarDocumento()
    {
        var documento = Selecionado<DocumentoLinha>(_tabelaDocumentos, "Selecione um documento.");
        ArquivarDocumento(documento.Origem);
    }

    private void ArquivarDocumento(Documento documento)
    {
        _contexto.ArquivarDocumento().Executar(documento.Id);
        AtualizarTudo();
    }

    private void TomarCienciaAlerta()
    {
        var alerta = Selecionado<AlertaLinha>(_tabelaAlertas, "Selecione um alerta.");
        TomarCienciaAlerta(alerta.Origem);
    }

    private void TomarCienciaAlerta(Alerta alerta)
    {
        _contexto.TomarCienciaAlerta().Executar(alerta.Id);
        AtualizarTudo();
    }

    private void ResolverAlerta()
    {
        var alerta = Selecionado<AlertaLinha>(_tabelaAlertas, "Selecione um alerta.");
        ResolverAlerta(alerta.Origem);
    }

    private void ResolverAlerta(Alerta alerta)
    {
        _contexto.ResolverAlerta().Executar(alerta.Id);
        AtualizarTudo();
    }

    private void AlterarStatusBem(StatusBem status)
    {
        var bem = Selecionado<BemLinha>(_tabelaBens, "Selecione um bem.");
        AlterarStatusBem(bem.Origem, status);
    }

    private void AlterarStatusBem(Bem bem, StatusBem status)
    {
        _contexto.AlterarStatusBem().Executar(new AlterarStatusBemComando(bem.Id, status));
        AtualizarTudo();
    }

    private void MostrarDetalhesBem()
    {
        var bem = Selecionado<BemLinha>(_tabelaBens, "Selecione um bem.");
        MostrarDetalhesBem(bem.Origem);
    }

    private void MostrarDetalhesBem(Bem bem)
    {
        var garantias = _contexto.RepositorioGarantia.ListarPorBem(bem.Id).Count;
        var manutencoes = _contexto.RepositorioManutencao.ListarPorBem(bem.Id).Count;
        var documentos = _contexto.RepositorioDocumento.ListarPorBem(bem.Id).Count;
        var alertas = _contexto.RepositorioAlerta.ListarPorBem(bem.Id).Count(a => a.Status != StatusAlerta.Resolvido);

        var mensagem = $"""
            Nome: {bem.Nome.Valor}
            Tipo: {Formatador.TextoEnum(bem.Tipo)}
            Status: {Formatador.TextoEnum(bem.Status)}
            Compra: {Formatador.Data(bem.CompradoEm)}
            Valor: {Formatador.Dinheiro(bem.PrecoCompra)}
            Observações: {bem.Observacoes}

            Garantias: {garantias}
            Manutenções: {manutencoes}
            Documentos: {documentos}
            Alertas abertos: {alertas}
            """;
        MostrarInformacao("Detalhes do bem", mensagem);
    }

    private static T Selecionado<T>(DataGrid tabela, string mensagem) where T : class
    {
        if (tabela.SelectedItem is T item)
        {
            return item;
        }

        throw new InvalidOperationException(mensagem);
    }

    private string NomeBem(BemId bemId) => _contexto.RepositorioBem.BuscarPorId(bemId)?.Nome.Valor ?? "Bem não encontrado";

    private void ExecutarComTratamento(Action acao)
    {
        try
        {
            acao();
        }
        catch (Exception excecao) when (excecao is not OutOfMemoryException and not StackOverflowException)
        {
            MostrarErro(excecao);
        }
    }

    private static FrameworkElement BarraAcoes(params Button[] botoes)
    {
        var barra = new WrapPanel();
        foreach (var botao in botoes)
        {
            botao.Margin = new Thickness(0, 0, 8, 8);
            barra.Children.Add(botao);
        }

        return new Border
        {
            Background = (Brush)Application.Current.Resources["CardFundoBrush"],
            BorderBrush = (Brush)Application.Current.Resources["CardBordaBrush"],
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10),
            Child = barra
        };
    }

    private Button BotaoPrimario(string texto, Action acao)
    {
        var botao = new Button { Content = texto, Style = (Style)Application.Current.Resources["BotaoPrimarioStyle"] };
        botao.Click += (_, _) => ExecutarComTratamento(acao);
        return botao;
    }

    private Button BotaoSecundario(string texto, Action acao)
    {
        var botao = new Button { Content = texto, Style = (Style)Application.Current.Resources["BotaoSecundarioStyle"] };
        botao.Click += (_, _) => ExecutarComTratamento(acao);
        return botao;
    }

    private Button BotaoPerigo(string texto, Action acao)
    {
        var botao = new Button { Content = texto, Style = (Style)Application.Current.Resources["BotaoPerigoStyle"] };
        botao.Click += (_, _) => ExecutarComTratamento(acao);
        return botao;
    }
}
