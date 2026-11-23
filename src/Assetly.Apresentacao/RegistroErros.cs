using System.Globalization;
using System.IO;
using System.Text;
using Assetly.Infraestrutura.Armazenamento;

namespace Assetly.Apresentacao;

/// <summary>Grava erros inesperados em <c>~/.assetly/logs/assetly-AAAA-MM-DD.log</c> — só no
/// computador do usuário, nunca enviado a lugar nenhum. Serve para diagnosticar um problema relatado
/// ("o que aconteceu ontem às 15h?") sem precisar reproduzi-lo. Mantém os últimos 30 arquivos.</summary>
internal static class RegistroErros
{
    private const int DiasRetidos = 30;
    private static readonly object Trava = new();

    internal static string DiretorioLogs => CaminhosArmazenamentoLocal.DiretorioLogs();

    internal static void Registrar(string contexto, Exception excecao)
    {
        try
        {
            lock (Trava)
            {
                Directory.CreateDirectory(DiretorioLogs);
                var arquivo = Path.Combine(DiretorioLogs, $"assetly-{DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}.log");
                var texto = new StringBuilder()
                    .Append('[').Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)).Append("] ").AppendLine(contexto)
                    .AppendLine(excecao.ToString())
                    .AppendLine();
                File.AppendAllText(arquivo, texto.ToString(), Encoding.UTF8);
                LimparAntigos();
            }
        }
        catch (Exception excecaoLog) when (excecaoLog is IOException or UnauthorizedAccessException)
        {
            // Registrar o erro nunca pode virar um segundo erro para o usuário.
        }
    }

    private static void LimparAntigos()
    {
        foreach (var antigo in Directory.EnumerateFiles(DiretorioLogs, "assetly-*.log")
                     .OrderByDescending(caminho => caminho, StringComparer.Ordinal)
                     .Skip(DiasRetidos))
        {
            File.Delete(antigo);
        }
    }
}
