using System.Windows;

namespace Assetly.Apresentacao.Visao;

/// <summary>
/// Contrato de um módulo da aplicação — cada item do menu lateral (Painel, Bens, Garantias, ...,
/// Backup) é uma classe autônoma que implementa esta interface. <see cref="TelaPrincipal"/> não
/// conhece nenhum módulo específico: ela só monta a lista, itera para desenhar a sidebar e chama
/// <see cref="Atualizar"/> em cada um a cada ciclo de atualização (intervalo configurável). Adicionar um módulo novo é
/// criar uma classe + uma linha na lista — não exige tocar em nenhum outro módulo nem na casca.
/// </summary>
internal interface IModulo
{
    string Titulo { get; }

    string Descricao { get; }

    /// <summary>Se falso (ex.: Painel, Backup), a sidebar não mostra contador para este módulo.</summary>
    bool ComContador { get; }

    /// <summary>Construído uma vez, na criação do módulo — mesma janela de inicialização eager que a
    /// tela principal já usava antes da modularização.</summary>
    UIElement Conteudo { get; }

    /// <summary>Contagem mostrada na sidebar. Sem sentido (ignorado) quando <see cref="ComContador"/> é falso.</summary>
    int Contador { get; }

    /// <summary>Chamado a cada ciclo de atualização (intervalo configurável) e após qualquer ação que mude os dados do módulo.</summary>
    void Atualizar();
}
