using System.Text.RegularExpressions;

namespace BeautyCommerce.Application.Features.Media.Commands.SyncImagesFromDrive;

/// <summary>
/// Convención de nombres en la carpeta de Drive: el nombre del archivo
/// (sin extensión) es el SKU del producto. Para varias fotos del mismo
/// producto se agrega un sufijo "-N": "SKU12345.jpg" o
/// "SKU12345-1.jpg" es la foto principal, "SKU12345-2.jpg" la
/// siguiente, etc.
/// </summary>
public static class DriveFileNameParser
{
    private static readonly Regex SuffixPattern =
        new(@"^(?<sku>.+)-(?<order>\d+)$", RegexOptions.Compiled);

    /// <summary>
    /// Devuelve los candidatos de (sku, displayOrder) a probar, en
    /// orden de prioridad: primero el nombre completo tal cual
    /// (por si el SKU real contiene guiones), y si el nombre tiene un
    /// sufijo numérico, también la variante con el sufijo interpretado
    /// como número de orden.
    /// </summary>
    public static IEnumerable<(string Sku, int Order)> GetCandidates(
        string fileNameWithoutExtension)
    {
        var trimmed = fileNameWithoutExtension.Trim();

        yield return (trimmed, 1);

        var match = SuffixPattern.Match(trimmed);

        if (match.Success)
        {
            var sku = match.Groups["sku"].Value;
            var order = int.Parse(match.Groups["order"].Value);

            yield return (sku, order);
        }
    }
}
