using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Sahaf.Web.Infrastructure;

// <field asp-for="Email" type="email" /> renders label + input + validation
// message, so every form in the site looks and behaves the same.
[HtmlTargetElement("field", TagStructure = TagStructure.WithoutEndTag)]
public class FieldTagHelper(IHtmlGenerator generator) : TagHelper
{
    [HtmlAttributeName("asp-for")] public ModelExpression For { get; set; } = null!;
    [HtmlAttributeName("type")] public string Type { get; set; } = "text";
    [HtmlAttributeName("hint")] public string? Hint { get; set; }
    [HtmlAttributeName("autocomplete")] public string? Autocomplete { get; set; }
    [HtmlAttributeName("items")] public IEnumerable<SelectListItem>? Items { get; set; }
    [HtmlAttributeName("option-label")] public string? OptionLabel { get; set; }
    [HtmlAttributeName("rows")] public int Rows { get; set; } = 5;

    [ViewContext, HtmlAttributeNotBound] public ViewContext ViewContext { get; set; } = null!;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "div";
        output.TagMode = TagMode.StartTagAndEndTag;
        var name = For.Name;
        var explorer = For.ModelExplorer;
        var hasError = ViewContext.ViewData.ModelState.TryGetValue(
            ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldName(name), out var entry) && entry.Errors.Count > 0;
        output.Attributes.SetAttribute("class", "field" + (Type == "checkbox" ? " field--check" : "") + (hasError ? " field--invalid" : ""));

        var attrs = new Dictionary<string, object> { ["class"] = "input" };
        if (Autocomplete != null) attrs["autocomplete"] = Autocomplete;
        if (Hint != null) attrs["aria-describedby"] = $"{name.Replace('.', '_')}-hint";

        TagBuilder input = Type switch
        {
            "checkbox" => generator.GenerateCheckBox(ViewContext, explorer, name, null, attrs),
            "password" => generator.GeneratePassword(ViewContext, explorer, name, null, attrs),
            "textarea" => generator.GenerateTextArea(ViewContext, explorer, name, Rows, 0, attrs),
            "select" => generator.GenerateSelect(ViewContext, explorer, OptionLabel, name, Items ?? [], false, attrs),
            _ => generator.GenerateTextBox(ViewContext, explorer, name, explorer.Model, null, WithType(attrs, Type)),
        };
        var label = generator.GenerateLabel(ViewContext, explorer, name, null, new { @class = "field__label" });
        var error = generator.GenerateValidationMessage(ViewContext, explorer, name, null, "span", new { @class = "field__error" });

        if (Type == "checkbox")
        {
            output.Content.AppendHtml(input);
            // Unchecked boxes post nothing; the hidden "false" keeps binding correct.
            output.Content.AppendHtml(generator.GenerateHiddenForCheckbox(ViewContext, explorer, name));
            output.Content.AppendHtml(label);
        }
        else
        {
            output.Content.AppendHtml(label);
            output.Content.AppendHtml(input);
        }
        if (Hint != null)
            output.Content.AppendHtml($"<small class=\"field__hint\" id=\"{name.Replace('.', '_')}-hint\">")
                .Append(Hint).AppendHtml("</small>");
        output.Content.AppendHtml(error);
    }

    private static Dictionary<string, object> WithType(Dictionary<string, object> attrs, string type)
    {
        // type="number" rejects "215,00", the Turkish way of writing decimals,
        // so money fields are text inputs that open the numeric keyboard.
        if (type == "decimal")
        {
            attrs["type"] = "text";
            attrs["inputmode"] = "decimal";
        }
        else
        {
            attrs["type"] = type;
        }
        return attrs;
    }
}
