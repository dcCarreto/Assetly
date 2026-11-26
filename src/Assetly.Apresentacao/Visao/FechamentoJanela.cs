using System.ComponentModel;
using System.Windows;

namespace Assetly.Apresentacao.Visao;

/// <summary>
/// O que acontece quando o usuário fecha a janela principal. Com a bandeja ligada, fechar só esconde a janela. Sem ela,
/// o primeiro pedido é adiado: <paramref name="finalizar"/> termina a operação em andamento e envia a sincronização e
/// então chama a continuação, que fecha de verdade; um segundo pedido enquanto isso é ignorado.
/// <para>Fechar ou esconder acontece sempre depois que o evento de fechamento termina: o WPF não deixa chamar
/// <c>Close</c> de dentro do próprio <c>Closing</c> (era o erro "não será possível chamar Close enquanto uma janela
/// estiver sendo fechada", quando não havia nada a terminar e a continuação vinha na hora).</para>
/// </summary>
internal sealed class FechamentoJanela
{
    private readonly Window _janela;
    private readonly Func<bool> _esconderNaBandeja;
    private readonly Action<Action> _finalizar;
    private readonly Action _aoEsconder;
    private bool _podeFechar;

    internal FechamentoJanela(Window janela, Func<bool> esconderNaBandeja, Action<Action> finalizar, Action aoEsconder)
    {
        _janela = janela;
        _esconderNaBandeja = esconderNaBandeja;
        _finalizar = finalizar;
        _aoEsconder = aoEsconder;
        janela.Closing += AoFechar;
    }

    /// <summary>Já está terminando o que havia em andamento para fechar.</summary>
    internal bool Finalizando { get; private set; }

    /// <summary>Fechar de verdade mesmo com a bandeja ligada (Sair, atualização, desligamento do Windows).</summary>
    internal bool SairDeVerdade { get; set; }

    private void AoFechar(object? remetente, CancelEventArgs evento)
    {
        if (_podeFechar)
        {
            return;
        }

        evento.Cancel = true;
        if (!SairDeVerdade && _esconderNaBandeja())
        {
            _janela.Dispatcher.BeginInvoke(() =>
            {
                _janela.Hide();
                _aoEsconder();
            });
            return;
        }

        if (Finalizando)
        {
            return;
        }

        Finalizando = true;
        _finalizar(() => _janela.Dispatcher.BeginInvoke(() =>
        {
            _podeFechar = true;
            _janela.Close();
        }));
    }
}
