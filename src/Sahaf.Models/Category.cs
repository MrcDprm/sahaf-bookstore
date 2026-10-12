using System.ComponentModel.DataAnnotations;

namespace Sahaf.Models;

public class Category
{
    public int Id { get; set; }

    [Required(ErrorMessage = "val.required")]
    [StringLength(40, ErrorMessage = "val.maxLength")]
    [Display(Name = "field.name")]
    public string Name { get; set; } = "";

    [StringLength(40, ErrorMessage = "val.maxLength")]
    [Display(Name = "field.nameEn")]
    public string? NameEn { get; set; }

    [Range(1, 100, ErrorMessage = "val.range")]
    [Display(Name = "field.displayOrder")]
    public int DisplayOrder { get; set; }

    public string LocalName(bool english) =>
        english && !string.IsNullOrWhiteSpace(NameEn) ? NameEn : Name;
}
