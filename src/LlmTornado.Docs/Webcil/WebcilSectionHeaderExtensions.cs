namespace LlmTornado.Docs.Webcil;

internal static class WebcilSectionHeaderExtensions
{
    internal static uint GetCorrectedPointerToRawData(this Webcil.WebcilSectionHeader webcilSectionHeader, int offset)
    {
        return (uint) (webcilSectionHeader.PointerToRawData + offset);
    }
}