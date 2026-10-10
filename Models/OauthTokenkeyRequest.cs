using System.ComponentModel.DataAnnotations;

namespace thaibev.Models;

public sealed class OauthTokenkeyRequest
{
    [Required]
    public string VendorID { get; set; } = string.Empty;

    [Required]
    public string AuthKey { get; set; } = string.Empty;
}
