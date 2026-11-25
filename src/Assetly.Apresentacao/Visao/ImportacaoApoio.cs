using System.IO;
using System.Windows;
using static Assetly.Apresentacao.Visao.DialogoApoio;

namespace Assetly.Apresentacao.Visao;

/// <summary>Importação: exportação JSON do próprio Assetly (todos os dados) e planilha CSV de bens (cadastro em massa).</summary>
internal static class ImportacaoApoio
{
    internal static void ImportarJson(ContextoAplicacao contexto)
    {
        var arquivo = Selecionar("Exportação do Assetly (JSON)", "JSON (*.json)|*.json|Todos os arquivos|*.*");
        if (arquivo is null
            || !MostrarConfirmacao("Importar dados",
                "Os registros do arquivo entram no Assetly: os que já existem (mesmo registro de uma exportação anterior) são atualizados com os " +
                "dados do arquivo e os demais são criados. Nada é apagado. Antes, o estado atual é copiado para a pasta de segurança.\n\n" +
                "O JSON não traz os arquivos dos documentos: os que não estiverem neste computador ficam marcados como ausentes.", "Importar"))
        {
            return;
        }

        var json = File.ReadAllText(arquivo);
        OperacaoEmSegundoPlano.ComEspera("Importando os dados…", () =>
        {
            var copia = contexto.CriarCopiaSeguranca();
            return (Resultado: contexto.ImportarDados().Executar(json), Copia: copia);
        }, saida =>
        {
            contexto.NotificarAlteracao();
            var r = saida.Resultado;
            MostrarInformacao("Importação concluída",
                $"{r.Bens} bem(ns) e {r.Registros} outro(s) registro(s) importados." +
                (r.Ignorados > 0 ? $"\n{r.Ignorados} registro(s) ligados a um bem que não está no arquivo foram ignorados." : string.Empty) +
                (r.DocumentosSemArquivo > 0 ? $"\n{r.DocumentosSemArquivo} documento(s) estão sem o arquivo neste computador (marcados como ausentes)." : string.Empty) +
                $"\n\nO estado anterior foi guardado em:\n{saida.Copia}");
        });
    }

    internal static void ImportarCsvBens(ContextoAplicacao contexto)
    {
        MostrarInformacao("Importar planilha de bens",
            "A primeira linha precisa ter os nomes das colunas. Só \"Nome\" é obrigatória; as demais são opcionais:\n\n" +
            "Tipo · Status · Data de compra · Preço · Moeda · Marca · Modelo · Número de série · Loja · Localização · Valor atual · Responsável · Observações\n\n" +
            "Datas em dd/mm/aaaa e valores como 1.234,56. Separador \";\" (Excel) ou \",\". O CSV exportado pela tabela de Bens também serve. " +
            "Se alguma linha tiver erro, nada é importado e a lista de problemas aparece.");
        var arquivo = Selecionar("Planilha de bens (CSV)", "Planilha CSV (*.csv)|*.csv|Texto (*.txt)|*.txt|Todos os arquivos|*.*");
        if (arquivo is null)
        {
            return;
        }

        var resultado = contexto.ImportarBensCsv().Executar(LerTexto(arquivo));
        contexto.NotificarAlteracao();
        MostrarInformacao("Importação concluída", $"{resultado.Criados} bem(ns) cadastrado(s)." +
            (resultado.JaCadastrados > 0 ? $"\n{resultado.JaCadastrados} linha(s) iguais a bens já cadastrados foram puladas." : string.Empty));
    }

    /// <summary>UTF-8 ou, se não for (o Excel em português salva CSV em ANSI), Latin-1 — os acentos chegam certos nos dois casos.</summary>
    private static string LerTexto(string arquivo)
    {
        var bytes = File.ReadAllBytes(arquivo);
        try
        {
            return new System.Text.UTF8Encoding(false, throwOnInvalidBytes: true).GetString(bytes);
        }
        catch (System.Text.DecoderFallbackException)
        {
            return System.Text.Encoding.Latin1.GetString(bytes);
        }
    }

    private static string? Selecionar(string titulo, string filtro)
    {
        var dialogo = new Microsoft.Win32.OpenFileDialog
        {
            Title = titulo,
            CheckFileExists = true,
            Multiselect = false,
            InitialDirectory = DiretorioInicial(null),
            Filter = filtro
        };
        return dialogo.ShowDialog(Application.Current.MainWindow) == true ? dialogo.FileName : null;
    }
}
