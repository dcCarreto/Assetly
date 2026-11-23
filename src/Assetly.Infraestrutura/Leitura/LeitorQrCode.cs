using ZXing;
using ZXing.Common;

namespace Assetly.Infraestrutura.Leitura;

/// <summary>Lê QR Codes de uma imagem (pixels BGRA de 32 bits). A decodificação da imagem em si (JPG,
/// PNG...) fica com quem chama, para esta classe não depender de biblioteca gráfica.</summary>
public static class LeitorQrCode
{
    public static string? Ler(byte[] pixelsBgra, int largura, int altura)
    {
        ArgumentNullException.ThrowIfNull(pixelsBgra);
        var leitor = new BarcodeReaderGeneric
        {
            AutoRotate = true,
            Options = new DecodingOptions
            {
                TryHarder = true,
                TryInverted = true,
                PossibleFormats = [BarcodeFormat.QR_CODE]
            }
        };

        var fonte = new RGBLuminanceSource(pixelsBgra, largura, altura, RGBLuminanceSource.BitmapFormat.BGRA32);
        return leitor.Decode(fonte)?.Text;
    }
}
