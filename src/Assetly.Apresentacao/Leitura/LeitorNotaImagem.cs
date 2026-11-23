using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using Assetly.Aplicacao.Leitura;
using Assetly.Infraestrutura.Leitura;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Assetly.Apresentacao.Leitura;

/// <summary>Resultado da leitura de uma foto/imagem de nota fiscal.</summary>
internal sealed record LeituraImagemNota(DadosNotaFiscal Dados, bool QrCodeEncontrado, bool OcrDisponivel, string TextoReconhecido);

/// <summary>
/// Lê uma foto ou imagem escaneada de nota fiscal: primeiro procura o QR Code da NFC-e (chave de acesso, link
/// de consulta e, quando a nota traz, data e valor); depois reconhece o texto impresso com o OCR do próprio
/// Windows (sem internet, sem enviar a imagem a ninguém) para completar data, valor total e CNPJ.
/// </summary>
internal static class LeitorNotaImagem
{
    internal static readonly string[] Extensoes = [".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tif", ".tiff", ".heic", ".webp"];

    internal static bool EhImagem(string caminho) => Extensoes.Contains(Path.GetExtension(caminho).ToLowerInvariant());

    /// <summary>Frase curta para o formulário: o que foi encontrado na imagem.</summary>
    internal static string Descrever(LeituraImagemNota leitura)
    {
        var dados = leitura.Dados;
        var partes = new List<string>();
        if (dados.Chave is { } chave)
        {
            partes.Add($"{chave.Modelo} nº {chave.Numero} ({chave.Uf})");
        }

        if (dados.Emissao is { } emissao)
        {
            partes.Add("data " + emissao.ToString("dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture));
        }

        if (dados.ValorTotal is { } total)
        {
            partes.Add("valor " + total.ToString("C2", System.Globalization.CultureInfo.GetCultureInfo("pt-BR")));
        }

        if (dados.CnpjEmitente is { } cnpj)
        {
            partes.Add("CNPJ " + cnpj);
        }

        var origem = leitura.QrCodeEncontrado ? "QR Code lido" : "QR Code não encontrado";
        var ocr = leitura.OcrDisponivel ? string.Empty : " (OCR indisponível neste Windows)";
        return partes.Count == 0
            ? $"{origem}{ocr}; nenhum dado reconhecido — preencha à mão."
            : $"{origem}{ocr}: {string.Join(", ", partes)}. Confira os campos.";
    }

    internal static LeituraImagemNota Ler(string caminho, DateOnly hoje) =>
        Task.Run(() => LerAsync(caminho, hoje)).GetAwaiter().GetResult();

    private static async Task<LeituraImagemNota> LerAsync(string caminho, DateOnly hoje)
    {
        var arquivo = await StorageFile.GetFileFromPathAsync(Path.GetFullPath(caminho));
        using IRandomAccessStream fluxo = await arquivo.OpenAsync(FileAccessMode.Read);
        BitmapDecoder decodificador;
        try
        {
            decodificador = await BitmapDecoder.CreateAsync(fluxo);
        }
        catch (Exception excecao) when (excecao is not OutOfMemoryException)
        {
            throw new InvalidOperationException("não foi possível abrir a imagem. Use uma foto ou imagem escaneada (JPG, PNG, BMP, TIFF...).", excecao);
        }

        var pixels = await decodificador.GetPixelDataAsync(
            BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied, new BitmapTransform(),
            ExifOrientationMode.RespectExifOrientation, ColorManagementMode.DoNotColorManage);
        var bytes = pixels.DetachPixelData();
        var largura = (int)decodificador.OrientedPixelWidth;
        var altura = (int)decodificador.OrientedPixelHeight;

        var dados = DadosNotaFiscal.Vazio;
        var conteudoQr = LeitorQrCode.Ler(bytes, largura, altura);
        if (conteudoQr is not null)
        {
            dados = LeituraNotaFiscal.InterpretarQrCode(conteudoQr, hoje);
        }

        var motor = OcrEngine.TryCreateFromUserProfileLanguages()
            ?? OcrEngine.AvailableRecognizerLanguages.Select(OcrEngine.TryCreateFromLanguage).FirstOrDefault(m => m is not null);
        if (motor is null)
        {
            return new LeituraImagemNota(dados, conteudoQr is not null, false, string.Empty);
        }

        using var bitmap = SoftwareBitmap.CreateCopyFromBuffer(bytes.AsBuffer(), BitmapPixelFormat.Bgra8, largura, altura, BitmapAlphaMode.Premultiplied);
        using var reduzido = await ReduzirSeNecessario(bitmap);
        var resultado = await motor.RecognizeAsync(reduzido ?? bitmap);
        var texto = string.Join('\n', resultado.Lines.Select(l => l.Text));
        dados = dados.Completar(LeituraNotaFiscal.InterpretarTexto(texto, hoje));
        return new LeituraImagemNota(dados, conteudoQr is not null, true, texto);
    }

    /// <summary>O OCR do Windows recusa imagens maiores que <see cref="OcrEngine.MaxImageDimension"/>.</summary>
    private static async Task<SoftwareBitmap?> ReduzirSeNecessario(SoftwareBitmap bitmap)
    {
        var maximo = OcrEngine.MaxImageDimension;
        if (bitmap.PixelWidth <= maximo && bitmap.PixelHeight <= maximo)
        {
            return null;
        }

        var escala = Math.Min((double)maximo / bitmap.PixelWidth, (double)maximo / bitmap.PixelHeight);
        using var memoria = new InMemoryRandomAccessStream();
        var codificador = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, memoria);
        codificador.SetSoftwareBitmap(bitmap);
        codificador.BitmapTransform.ScaledWidth = (uint)(bitmap.PixelWidth * escala);
        codificador.BitmapTransform.ScaledHeight = (uint)(bitmap.PixelHeight * escala);
        await codificador.FlushAsync();
        var decodificador = await BitmapDecoder.CreateAsync(memoria);
        return await decodificador.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
    }
}
