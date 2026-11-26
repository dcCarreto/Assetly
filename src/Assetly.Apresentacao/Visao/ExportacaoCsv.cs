using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Assetly.Apresentacao.Visao;

/// <summary>
/// Exporta para CSV exatamente o que a tabela está mostrando (filtro, busca e ordenação atuais), com as
/// colunas de dados (inclusive as escondidas só por falta de espaço) e os textos já formatados. Separador ";" e UTF-8 com BOM: o Excel em português abre
/// direto, com acentos e colunas certas.
/// </summary>
internal static class ExportacaoCsv
{
    internal static string Gerar(DataGrid tabela)
    {
        // Inclui as colunas escondidas só por falta de espaço na janela (ver TabelaApoio.ColunasOpcionais).
        var colunas = tabela.Columns.Where(c => c.ClipboardContentBinding is not null)
            .OrderBy(c => c.DisplayIndex).ToList();
        var csv = new StringBuilder();
        csv.AppendLine(string.Join(';', colunas.Select(c => Escapar(c.Header?.ToString()))));
        foreach (var item in tabela.Items)
        {
            csv.AppendLine(string.Join(';', colunas.Select(c => Escapar(c.OnCopyingCellClipboardContent(item)?.ToString()))));
        }

        return csv.ToString();
    }

    internal static string Escapar(string? valor)
    {
        var texto = (valor ?? string.Empty).Replace("\r\n", " ").Replace('\n', ' ');
        return texto.IndexOfAny([';', '"', '\n']) >= 0 || texto.StartsWith(' ') || texto.EndsWith(' ')
            ? "\"" + texto.Replace("\"", "\"\"") + "\""
            : texto;
    }

    /// <summary>Pergunta onde salvar e grava o CSV da tabela.</summary>
    internal static void Salvar(DataGrid tabela, string nomeTela)
    {
        if (tabela.Items.Count == 0)
        {
            DialogoApoio.MostrarInformacao("Exportar CSV", "A tabela está vazia (verifique o filtro e a busca).");
            return;
        }

        var dialogo = new Microsoft.Win32.SaveFileDialog
        {
            Title = $"Exportar {nomeTela} para CSV",
            FileName = $"Assetly - {nomeTela} - {DateTime.Now:yyyy-MM-dd}.csv",
            DefaultExt = ".csv",
            Filter = "Planilha CSV (separada por ponto e vírgula)|*.csv",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };
        if (dialogo.ShowDialog(Application.Current.MainWindow) != true)
        {
            return;
        }

        File.WriteAllText(dialogo.FileName, Gerar(tabela), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        DialogoApoio.MostrarInformacao("Exportar CSV", $"{tabela.Items.Count} linha(s) exportada(s) para:\n{dialogo.FileName}");
    }
}
