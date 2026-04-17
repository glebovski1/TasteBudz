namespace TasteBudz.Backend.Modules.Media;

internal sealed record ValidatedImageFile(
    byte[] Content,
    string ContentType,
    string FileName);
