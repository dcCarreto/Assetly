namespace Assetly.Aplicacao.Excecao;

/// <summary>A restauração falhou depois de começar a substituir os dados atuais (banco ou documentos):
/// quem chamou precisa devolver o estado anterior. Falhas antes disso (senha errada, arquivo que não é
/// backup, versão mais nova) usam <see cref="InvalidOperationException"/> e não alteram nada.</summary>
public sealed class RestauracaoIncompletaExcecao(string mensagem, Exception causa) : Exception(mensagem, causa);
