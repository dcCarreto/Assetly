namespace Assetly.Dominio.Preferencias;

/// <summary>Preferências de aparência, layout e comportamento do usuário — uma por ambiente, mesma
/// filosofia de <see cref="Backup.ConfiguracaoBackup"/>. "Configurações" (o módulo que edita isto) é
/// fixo na barra lateral e nunca aparece em <see cref="OrdemModulos"/>/<see cref="ModulosOcultos"/>.</summary>
public sealed class ConfiguracaoPreferencias
{
    private readonly List<string> _ordemModulos;
    private readonly HashSet<string> _modulosOcultos;

    public TemaVisual Tema { get; private set; }
    public CorDestaque CorDestaque { get; private set; }
    public DensidadeTabela DensidadeTabela { get; private set; }
    public IntervaloAtualizacao IntervaloAtualizacao { get; private set; }
    public IReadOnlyList<string> OrdemModulos => _ordemModulos;
    public IReadOnlyCollection<string> ModulosOcultos => _modulosOcultos;
    public string? ModuloInicial { get; private set; }

    /// <summary>Procurar, baixar e oferecer versões novas do programa automaticamente (padrão: sim).</summary>
    public bool AtualizarAutomaticamente { get; private set; } = true;

    private ConfiguracaoPreferencias(TemaVisual tema, CorDestaque corDestaque, DensidadeTabela densidadeTabela,
        IntervaloAtualizacao intervaloAtualizacao, IEnumerable<string> ordemModulos, IEnumerable<string> modulosOcultos, string? moduloInicial)
    {
        Tema = tema;
        CorDestaque = corDestaque;
        DensidadeTabela = densidadeTabela;
        IntervaloAtualizacao = intervaloAtualizacao;
        _ordemModulos = ordemModulos.ToList();
        _modulosOcultos = modulosOcultos.ToHashSet();
        ModuloInicial = moduloInicial;
    }

    public static ConfiguracaoPreferencias Padrao() =>
        new(TemaVisual.Escuro, CorDestaque.Ciano, DensidadeTabela.Confortavel, IntervaloAtualizacao.Segundos30, [], [], null);

    public static ConfiguracaoPreferencias Restaurar(TemaVisual tema, CorDestaque corDestaque, DensidadeTabela densidadeTabela,
        IntervaloAtualizacao intervaloAtualizacao, IEnumerable<string> ordemModulos, IEnumerable<string> modulosOcultos, string? moduloInicial,
        bool atualizarAutomaticamente = true) =>
        new(tema, corDestaque, densidadeTabela, intervaloAtualizacao, ordemModulos, modulosOcultos, moduloInicial)
        {
            AtualizarAutomaticamente = atualizarAutomaticamente
        };

    public void DefinirTema(TemaVisual tema) => Tema = tema;

    public void DefinirAtualizacaoAutomatica(bool ativa) => AtualizarAutomaticamente = ativa;

    public void DefinirCorDestaque(CorDestaque corDestaque) => CorDestaque = corDestaque;

    public void DefinirDensidadeTabela(DensidadeTabela densidadeTabela) => DensidadeTabela = densidadeTabela;

    public void DefinirIntervaloAtualizacao(IntervaloAtualizacao intervaloAtualizacao) => IntervaloAtualizacao = intervaloAtualizacao;

    public TimeSpan IntervaloComoTimeSpan() => IntervaloAtualizacao switch
    {
        IntervaloAtualizacao.Segundos10 => TimeSpan.FromSeconds(10),
        IntervaloAtualizacao.Segundos30 => TimeSpan.FromSeconds(30),
        IntervaloAtualizacao.Segundos60 => TimeSpan.FromSeconds(60),
        _ => TimeSpan.FromSeconds(5)
    };

    /// <summary>Ponto único de segurança de versionamento: títulos que não existem mais (módulo
    /// renomeado/removido) são descartados; títulos novos (módulo futuro) são anexados ao final,
    /// visíveis por padrão. Chamado uma vez por lançamento, antes de qualquer leitura pela UI —
    /// nenhum outro código precisa reimplementar esse fallback.</summary>
    public void SincronizarModulos(IReadOnlyList<string> titulosAtuais)
    {
        var validos = titulosAtuais.ToHashSet();
        _ordemModulos.RemoveAll(titulo => !validos.Contains(titulo));
        foreach (var titulo in titulosAtuais)
        {
            if (!_ordemModulos.Contains(titulo))
            {
                _ordemModulos.Add(titulo);
            }
        }

        _modulosOcultos.RemoveWhere(titulo => !validos.Contains(titulo));
        if (ModuloInicial is not null && !validos.Contains(ModuloInicial))
        {
            ModuloInicial = null;
        }
    }

    public void MoverModuloParaCima(string titulo)
    {
        var indice = ExigirIndiceModulo(titulo);
        if (indice > 0)
        {
            (_ordemModulos[indice - 1], _ordemModulos[indice]) = (_ordemModulos[indice], _ordemModulos[indice - 1]);
        }
    }

    public void MoverModuloParaBaixo(string titulo)
    {
        var indice = ExigirIndiceModulo(titulo);
        if (indice < _ordemModulos.Count - 1)
        {
            (_ordemModulos[indice + 1], _ordemModulos[indice]) = (_ordemModulos[indice], _ordemModulos[indice + 1]);
        }
    }

    public void OcultarModulo(string titulo)
    {
        ExigirIndiceModulo(titulo);
        if (!_modulosOcultos.Contains(titulo) && _ordemModulos.Count - _modulosOcultos.Count <= 1)
        {
            throw new InvalidOperationException("pelo menos um módulo deve permanecer visível");
        }

        _modulosOcultos.Add(titulo);
        if (ModuloInicial == titulo)
        {
            ModuloInicial = null;
        }
    }

    public void MostrarModulo(string titulo)
    {
        ExigirIndiceModulo(titulo);
        _modulosOcultos.Remove(titulo);
    }

    public void DefinirModuloInicial(string? titulo)
    {
        if (titulo is not null && (!_ordemModulos.Contains(titulo) || _modulosOcultos.Contains(titulo)))
        {
            throw new ArgumentException("módulo inicial precisa ser um módulo visível", nameof(titulo));
        }

        ModuloInicial = titulo;
    }

    /// <summary>Módulo inicial salvo, se ainda visível; senão o primeiro módulo visível na ordem atual.</summary>
    public string? ResolverModuloInicial() =>
        ModuloInicial is not null && _ordemModulos.Contains(ModuloInicial) && !_modulosOcultos.Contains(ModuloInicial)
            ? ModuloInicial
            : _ordemModulos.FirstOrDefault(titulo => !_modulosOcultos.Contains(titulo));

    private int ExigirIndiceModulo(string titulo)
    {
        var indice = _ordemModulos.IndexOf(titulo);
        if (indice < 0)
        {
            throw new ArgumentException("módulo desconhecido", nameof(titulo));
        }

        return indice;
    }
}
