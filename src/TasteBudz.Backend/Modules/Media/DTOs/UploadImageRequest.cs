using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace TasteBudz.Backend.Modules.Media;

public sealed class UploadImageRequest
{
    [Required]
    public IFormFile? File { get; init; }
}
