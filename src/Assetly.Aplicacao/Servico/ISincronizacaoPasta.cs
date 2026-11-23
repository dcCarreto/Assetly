namespace Assetly.Aplicacao.Servico;

/// <summary>Informações da cópia gravada na pasta sincronizada.</summary>
public sealed record MetadadosSincronizacao(string Impressao, string Dispositivo, DateTimeOffset GravadoEm, bool Criptografado);

/// <summary>Leitura e gravação da cópia dos dados na pasta sincronizada (OneDrive, Google Drive...).</summary>
public interface ISincronizacaoPasta
{
    MetadadosSincronizacao? LerMetadados(string pasta);

    void Enviar(string pasta, string arquivoBanco, string diretorioDocumentos, string? senha, MetadadosSincronizacao metadados);

    /// <summary>Arquivo da cópia na pasta (para restaurar).</summary>
    string CaminhoCopia(string pasta);
}
