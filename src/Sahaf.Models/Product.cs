using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Sahaf.Models;

public class Product
{
    public int Id { get; set; }

    [Required(ErrorMessage = "val.required")]
    [StringLength(120, ErrorMessage = "val.maxLength")]
    [Display(Name = "field.title")]
    public string Title { get; set; } = "";

    [StringLength(120, ErrorMessage = "val.maxLength")]
    [Display(Name = "field.titleEn")]
    public string? TitleEn { get; set; }

    [Required(ErrorMessage = "val.required")]
    [StringLength(80, ErrorMessage = "val.maxLength")]
    [Display(Name = "field.author")]
    public string Author { get; set; } = "";

    [Required(ErrorMessage = "val.required")]
    [StringLength(2000, ErrorMessage = "val.maxLength")]
    [Display(Name = "field.description")]
    public string Description { get; set; } = "";

    [StringLength(2000, ErrorMessage = "val.maxLength")]
    [Display(Name = "field.descriptionEn")]
    public string? DescriptionEn { get; set; }

    [StringLength(17, ErrorMessage = "val.maxLength")]
    [RegularExpression(@"^[0-9\-]{10,17}$", ErrorMessage = "val.isbn")]
    [Display(Name = "field.isbn")]
    public string? ISBN { get; set; }

    [Range(1, 5000, ErrorMessage = "val.range")]
    [Display(Name = "field.pageCount")]
    public int? PageCount { get; set; }

    [Range(-1000, 2100, ErrorMessage = "val.range")]
    [Display(Name = "field.year")]
    public int? Year { get; set; }

    [Range(1, 100000, ErrorMessage = "val.range")]
    [Display(Name = "field.listPrice")]
    [Column(TypeName = "numeric(10,2)")]
    public decimal ListPrice { get; set; }

    [Range(1, 100000, ErrorMessage = "val.range")]
    [Display(Name = "field.price")]
    [Column(TypeName = "numeric(10,2)")]
    public decimal Price { get; set; }

    [Range(1, 100000, ErrorMessage = "val.range")]
    [Display(Name = "field.price50")]
    [Column(TypeName = "numeric(10,2)")]
    public decimal Price50 { get; set; }

    [Range(1, 100000, ErrorMessage = "val.range")]
    [Display(Name = "field.price100")]
    [Column(TypeName = "numeric(10,2)")]
    public decimal Price100 { get; set; }

    [Display(Name = "field.featured")]
    public bool IsFeatured { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Display(Name = "field.category")]
    public int CategoryId { get; set; }

    [ValidateNever]
    public Category Category { get; set; } = null!;

    [ValidateNever]
    public List<ProductImage> Images { get; set; } = [];

    // First uploaded image, kept here so lists can show it without loading
    // image bytes. Null means the generated cover is used.
    public Guid? CoverImageId { get; set; }

    public string LocalTitle(bool english) =>
        english && !string.IsNullOrWhiteSpace(TitleEn) ? TitleEn : Title;

    public string LocalDescription(bool english) =>
        english && !string.IsNullOrWhiteSpace(DescriptionEn) ? DescriptionEn : Description;

    public int DiscountPercent =>
        ListPrice > Price ? (int)Math.Round((ListPrice - Price) / ListPrice * 100) : 0;
}
