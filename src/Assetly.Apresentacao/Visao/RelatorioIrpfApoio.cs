using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using static Assetly.Apresentacao.Visao.DialogoApoio;

namespace Assetly.Apresentacao.Visao;

/// <summary>Gera o relatório de apoio ao IRPF (gastos de saúde dedutíveis + bens e direitos) num arquivo de texto.</summary>
internal static class RelatorioIrpfApoio
{
    internal static void Gerar(ContextoAplicacao contexto)
    {
        var anoAtual = ContextoApoio.Hoje().Year;
        var anos = Enumerable.Range(0, 6).Select(i => new ItemValor<int>(anoAtual - i, (anoAtual - i).ToString())).ToList();
        // A declaração de cada ano é sobre o ano anterior.
        var ano = ApoioFichas.ComboItens(anos, valor => valor == anoAtual - 1);
        var grade = GradeFormulario();
        AdicionarCampo(grade, 0, 0, "Ano-calendário *", ano);
        var nota = UiApoio.Texto("Gastos de saúde dedutíveis (consultas, exames e procedimentos) por prestador e paciente, e os bens com o valor de aquisição. " +
            "Para a dedução, informe o CPF/CNPJ do prestador em cada gasto.", 11.5, FontWeights.Normal, (System.Windows.Media.Brush)Application.Current.Resources["TextMuted"]);
        nota.TextWrapping = TextWrapping.Wrap;
        AdicionarCampo(grade, 1, 0, "O que entra", nota, ocupaLinhaToda: true);

        var escolhido = 0;
        if (!MostrarFormulario("Relatório para o Imposto de Renda", grade, () => escolhido = ApoioFichas.ValorItem<int>(ano, "escolha o ano"), 520))
        {
            return;
        }

        var dialogo = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Salvar relatório para o Imposto de Renda",
            FileName = $"Assetly-IRPF-{escolhido}.txt",
            DefaultExt = ".txt",
            Filter = "Texto (*.txt)|*.txt",
            InitialDirectory = DiretorioInicial(null)
        };
        if (dialogo.ShowDialog(Application.Current.MainWindow) != true)
        {
            return;
        }

        var relatorio = contexto.GerarRelatorioIrpf().Executar(escolhido);
        File.WriteAllText(dialogo.FileName, relatorio.ParaTexto(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        if (MostrarConfirmacao("Relatório salvo",
                $"{relatorio.Pagamentos.Count} prestador(es) de saúde · total dedutível {Formatador.Dinheiro(Dominio.Compartilhado.Dinheiro.De(relatorio.TotalDedutivel, "BRL"))} · {relatorio.Bens.Count} bem(ns).\n\n{dialogo.FileName}\n\nDeseja abrir o relatório agora?",
                "Abrir"))
        {
            Process.Start(new ProcessStartInfo(dialogo.FileName) { UseShellExecute = true });
        }
    }
}
