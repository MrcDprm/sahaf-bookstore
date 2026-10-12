using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Sahaf.Models;

public class OrderHeader
{
    // A Guid instead of 1, 2, 3: order ids appear in URLs and e-mails.
    public Guid Id { get; set; } = Guid.NewGuid();

    public string ApplicationUserId { get; set; } = "";
    [ValidateNever]
    public ApplicationUser ApplicationUser { get; set; } = null!;

    public DateTime OrderDate { get; set; }
    public DateTime? ShippingDate { get; set; }

    [Column(TypeName = "numeric(12,2)")]
    public decimal OrderTotal { get; set; }

    [StringLength(20)]
    public string OrderStatus { get; set; } = "";
    [StringLength(30)]
    public string PaymentStatus { get; set; } = "";

    [StringLength(40, ErrorMessage = "val.maxLength")]
    [Display(Name = "field.tracking")]
    public string? TrackingNumber { get; set; }

    [StringLength(40, ErrorMessage = "val.maxLength")]
    [Display(Name = "field.carrier")]
    public string? Carrier { get; set; }

    public DateTime? PaymentDate { get; set; }
    public DateOnly? PaymentDueDate { get; set; }

    [StringLength(200)]
    public string? SessionId { get; set; }
    [StringLength(200)]
    public string? PaymentIntentId { get; set; }

    [Required(ErrorMessage = "val.required")]
    [StringLength(80, ErrorMessage = "val.maxLength")]
    [Display(Name = "field.fullName")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "val.required")]
    [Phone(ErrorMessage = "val.phone")]
    [StringLength(20, ErrorMessage = "val.maxLength")]
    [Display(Name = "field.phone")]
    public string PhoneNumber { get; set; } = "";

    [Required(ErrorMessage = "val.required")]
    [StringLength(200, ErrorMessage = "val.maxLength")]
    [Display(Name = "field.street")]
    public string StreetAddress { get; set; } = "";

    [Required(ErrorMessage = "val.required")]
    [StringLength(60, ErrorMessage = "val.maxLength")]
    [Display(Name = "field.city")]
    public string City { get; set; } = "";

    [Required(ErrorMessage = "val.required")]
    [StringLength(60, ErrorMessage = "val.maxLength")]
    [Display(Name = "field.district")]
    public string State { get; set; } = "";

    [Required(ErrorMessage = "val.required")]
    [StringLength(10, ErrorMessage = "val.maxLength")]
    [Display(Name = "field.postalCode")]
    public string PostalCode { get; set; } = "";

    [NotMapped]
    public string Number => Id.ToString("N")[..8].ToUpperInvariant();
}
