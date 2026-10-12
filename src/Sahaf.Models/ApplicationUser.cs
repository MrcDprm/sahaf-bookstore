using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace Sahaf.Models;

public class ApplicationUser : IdentityUser
{
    [Required, StringLength(80)]
    public string Name { get; set; } = "";

    [StringLength(200)]
    public string? StreetAddress { get; set; }

    [StringLength(60)]
    public string? City { get; set; }

    [StringLength(60)]
    public string? State { get; set; }

    [StringLength(10)]
    public string? PostalCode { get; set; }

    public int? CompanyId { get; set; }
    public Company? Company { get; set; }

    [NotMapped]
    public string Role { get; set; } = "";
}
