using System.ComponentModel.DataAnnotations;

namespace Sahaf.Models;

public class Company
{
    public int Id { get; set; }

    [Required(ErrorMessage = "val.required")]
    [StringLength(100, ErrorMessage = "val.maxLength")]
    [Display(Name = "field.companyName")]
    public string Name { get; set; } = "";

    [StringLength(200, ErrorMessage = "val.maxLength")]
    [Display(Name = "field.street")]
    public string? StreetAddress { get; set; }

    [StringLength(60, ErrorMessage = "val.maxLength")]
    [Display(Name = "field.city")]
    public string? City { get; set; }

    [StringLength(60, ErrorMessage = "val.maxLength")]
    [Display(Name = "field.district")]
    public string? State { get; set; }

    [StringLength(10, ErrorMessage = "val.maxLength")]
    [Display(Name = "field.postalCode")]
    public string? PostalCode { get; set; }

    [Phone(ErrorMessage = "val.phone")]
    [StringLength(20, ErrorMessage = "val.maxLength")]
    [Display(Name = "field.phone")]
    public string? PhoneNumber { get; set; }
}
