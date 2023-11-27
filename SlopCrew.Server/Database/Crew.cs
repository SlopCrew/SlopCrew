using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace SlopCrew.Server.Database;

[PrimaryKey("Id")]
[Index(nameof(Tag), IsUnique = true)]
public class Crew {
    // ReSharper disable once EntityFramework.ModelValidation.UnlimitedStringLength
    public required string Id { get; set; }

    [MinLength(3)] [MaxLength(32)] public required string Name { get; set; }
    [MinLength(3)] [MaxLength(32)] public required string Tag { get; set; }

    public required List<User> Owners { get; set; }
    public required List<User> Members { get; set; }

    public string[] InviteCodes { get; set; } = [];
}
