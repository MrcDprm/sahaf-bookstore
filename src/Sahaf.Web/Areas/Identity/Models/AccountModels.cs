using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Sahaf.Web.Areas.Identity.Models;

public class LoginVM
{
    [Required(ErrorMessage = "val.required")]
    [EmailAddress(ErrorMessage = "val.email")]
    [Display(Name = "field.email")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "val.required")]
    [DataType(DataType.Password)]
    [Display(Name = "field.password")]
    public string Password { get; set; } = "";

    [Display(Name = "field.rememberMe")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

public class RegisterVM
{
    [Required(ErrorMessage = "val.required")]
    [StringLength(80, ErrorMessage = "val.maxLength")]
    [Display(Name = "field.fullName")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "val.required")]
    [EmailAddress(ErrorMessage = "val.email")]
    [StringLength(256, ErrorMessage = "val.maxLength")]
    [Display(Name = "field.email")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "val.required")]
    [StringLength(100, MinimumLength = 10, ErrorMessage = "val.minLength")]
    [DataType(DataType.Password)]
    [Display(Name = "field.password")]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "val.required")]
    [Compare(nameof(Password), ErrorMessage = "val.compare")]
    [DataType(DataType.Password)]
    [Display(Name = "field.confirmPassword")]
    public string ConfirmPassword { get; set; } = "";

    // Only read when an admin creates the account; ignored for everyone else.
    [Display(Name = "field.role")]
    public string? Role { get; set; }

    [Display(Name = "field.company")]
    public int? CompanyId { get; set; }

    [ValidateNever] public IEnumerable<SelectListItem> RoleList { get; set; } = [];
    [ValidateNever] public IEnumerable<SelectListItem> CompanyList { get; set; } = [];
}

public class EmailVM
{
    [Required(ErrorMessage = "val.required")]
    [EmailAddress(ErrorMessage = "val.email")]
    [Display(Name = "field.email")]
    public string Email { get; set; } = "";
}

public class ResetPasswordVM
{
    [Required(ErrorMessage = "val.required")]
    [EmailAddress(ErrorMessage = "val.email")]
    [Display(Name = "field.email")]
    public string Email { get; set; } = "";

    [Required]
    public string Code { get; set; } = "";

    [Required(ErrorMessage = "val.required")]
    [StringLength(100, MinimumLength = 10, ErrorMessage = "val.minLength")]
    [DataType(DataType.Password)]
    [Display(Name = "field.newPassword")]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "val.required")]
    [Compare(nameof(Password), ErrorMessage = "val.compare")]
    [DataType(DataType.Password)]
    [Display(Name = "field.confirmPassword")]
    public string ConfirmPassword { get; set; } = "";
}

public class ProfileVM
{
    [ValidateNever] public string Email { get; set; } = "";

    [Required(ErrorMessage = "val.required")]
    [StringLength(80, ErrorMessage = "val.maxLength")]
    [Display(Name = "field.fullName")]
    public string Name { get; set; } = "";

    [Phone(ErrorMessage = "val.phone")]
    [StringLength(20, ErrorMessage = "val.maxLength")]
    [Display(Name = "field.phone")]
    public string? PhoneNumber { get; set; }

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
}

public class ChangePasswordVM
{
    [Required(ErrorMessage = "val.required")]
    [DataType(DataType.Password)]
    [Display(Name = "field.currentPassword")]
    public string OldPassword { get; set; } = "";

    [Required(ErrorMessage = "val.required")]
    [StringLength(100, MinimumLength = 10, ErrorMessage = "val.minLength")]
    [DataType(DataType.Password)]
    [Display(Name = "field.newPassword")]
    public string NewPassword { get; set; } = "";

    [Required(ErrorMessage = "val.required")]
    [Compare(nameof(NewPassword), ErrorMessage = "val.compare")]
    [DataType(DataType.Password)]
    [Display(Name = "field.confirmPassword")]
    public string ConfirmPassword { get; set; } = "";
}

public class DeleteAccountVM
{
    [Required(ErrorMessage = "val.required")]
    [DataType(DataType.Password)]
    [Display(Name = "field.password")]
    public string Password { get; set; } = "";
}

public record NoticeVM(string Title, string Text, string? LinkText = null, string? LinkUrl = null);
