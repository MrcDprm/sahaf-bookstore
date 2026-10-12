using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Sahaf.DataAccess.Repository.IRepository;
using Sahaf.Models;
using Sahaf.Models.ViewModels;
using Sahaf.Utility;
using Sahaf.Web.Localization;

namespace Sahaf.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = SD.Role_Admin)]
public class ProductController(IUnitOfWork unitOfWork, IStringLocalizer<SharedResource> t) : Controller
{
    private const int PageSize = 20;
    public const int MaxImages = 6;
    public const int MaxFilesPerUpload = 3;

    public IActionResult Index(string? q, int? categoryId, int page = 1)
    {
        var query = unitOfWork.Product.Query("Category");
        q = q?.Trim();
        if (!string.IsNullOrEmpty(q))
        {
            if (q.Length > 80) q = q[..80];
            var pattern = "%" + q.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_") + "%";
            query = query.Where(p => EF.Functions.ILike(p.Title, pattern) || EF.Functions.ILike(p.Author, pattern));
        }
        if (categoryId.HasValue) query = query.Where(p => p.CategoryId == categoryId);

        var total = query.Count();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
        page = Math.Clamp(page, 1, totalPages);
        ViewBag.Categories = CategoryList();
        return View(new AdminListVM<Product>
        {
            Items = query.OrderBy(p => p.Title).Skip((page - 1) * PageSize).Take(PageSize).ToList(),
            Query = q,
            CategoryId = categoryId,
            Page = page,
            TotalPages = totalPages,
            TotalCount = total,
        });
    }

    public IActionResult Upsert(int? id)
    {
        if (id is null or 0) return View(Vm(new Product()));
        var product = unitOfWork.Product.Get(p => p.Id == id);
        return product == null ? NotFound() : View(Vm(product));
    }

    [HttpPost]
    [RequestSizeLimit(MaxFilesPerUpload * ImageValidator.MaxBytes + 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxFilesPerUpload * ImageValidator.MaxBytes + 1024 * 1024)]
    public async Task<IActionResult> Upsert(ProductVM vm, List<IFormFile>? files)
    {
        var input = vm.Product;
        files ??= [];
        if (unitOfWork.Category.Get(c => c.Id == input.CategoryId) == null)
            ModelState.AddModelError("Product.CategoryId", t["admin.chooseCategory"]);
        if (input.Price > input.ListPrice)
            ModelState.AddModelError("Product.Price", t["admin.priceAboveList"]);
        if (input.Price50 > input.Price || input.Price100 > input.Price50)
            ModelState.AddModelError("Product.Price100", t["admin.tiersOrder"]);

        var existing = input.Id == 0 ? 0 : unitOfWork.ProductImage.Query().Count(i => i.ProductId == input.Id);
        var images = await ReadImagesAsync(files, existing);
        if (!ModelState.IsValid)
        {
            vm.CategoryList = CategoryList();
            vm.ImageIds = ImageIds(input.Id);
            return View(vm);
        }

        Product product;
        if (input.Id == 0)
        {
            product = new Product();
            product.CopyEditableFrom(input);
            unitOfWork.Product.Add(product);
            unitOfWork.Save();
            TempData["success"] = t["admin.created"].Value;
        }
        else
        {
            var stored = unitOfWork.Product.Get(p => p.Id == input.Id, tracked: true);
            if (stored == null) return NotFound();
            product = stored;
            product.CopyEditableFrom(input);
            TempData["success"] = t["admin.updated"].Value;
        }

        var order = existing;
        foreach (var (data, type) in images)
        {
            var image = new ProductImage { ProductId = product.Id, Data = data, ContentType = type, SortOrder = order++ };
            unitOfWork.ProductImage.Add(image);
            product.CoverImageId ??= image.Id;
        }
        unitOfWork.Save();
        return RedirectToAction(nameof(Upsert), new { id = product.Id });
    }

    [HttpPost]
    public IActionResult DeleteImage(Guid id)
    {
        var image = unitOfWork.ProductImage.Get(i => i.Id == id, tracked: true);
        if (image == null) return NotFound();
        var product = unitOfWork.Product.Get(p => p.Id == image.ProductId, tracked: true)!;
        unitOfWork.ProductImage.Remove(image);
        if (product.CoverImageId == id)
        {
            product.CoverImageId = unitOfWork.ProductImage.Query()
                .Where(i => i.ProductId == product.Id && i.Id != id)
                .OrderBy(i => i.SortOrder).Select(i => (Guid?)i.Id).FirstOrDefault();
        }
        unitOfWork.Save();
        TempData["success"] = t["admin.imageDeleted"].Value;
        return RedirectToAction(nameof(Upsert), new { id = product.Id });
    }

    [HttpPost]
    public IActionResult SetCover(Guid id)
    {
        var image = unitOfWork.ProductImage.Query().Where(i => i.Id == id).Select(i => new { i.Id, i.ProductId }).FirstOrDefault();
        if (image == null) return NotFound();
        var product = unitOfWork.Product.Get(p => p.Id == image.ProductId, tracked: true)!;
        product.CoverImageId = image.Id;
        unitOfWork.Save();
        return RedirectToAction(nameof(Upsert), new { id = product.Id });
    }

    [HttpPost]
    public IActionResult UseGeneratedCover(int id)
    {
        var product = unitOfWork.Product.Get(p => p.Id == id, tracked: true);
        if (product == null) return NotFound();
        product.CoverImageId = null;
        unitOfWork.Save();
        return RedirectToAction(nameof(Upsert), new { id });
    }

    [HttpPost]
    public IActionResult Delete(int id)
    {
        var product = unitOfWork.Product.Get(p => p.Id == id, tracked: true);
        if (product == null) return NotFound();
        // Ordered books stay: past orders must keep pointing at them.
        if (unitOfWork.OrderDetail.Query().Any(d => d.ProductId == id))
        {
            TempData["error"] = t["admin.productInUse"].Value;
            return RedirectToAction(nameof(Index));
        }
        unitOfWork.Product.Remove(product);
        unitOfWork.Save();
        TempData["success"] = t["admin.deleted"].Value;
        return RedirectToAction(nameof(Index));
    }

    // Each file is checked by size and by its first bytes; the file name and
    // the browser's Content-Type are ignored because both can be faked.
    private async Task<List<(byte[] Data, string Type)>> ReadImagesAsync(List<IFormFile> files, int existing)
    {
        var result = new List<(byte[], string)>();
        files = files.Where(f => f.Length > 0).ToList();
        if (files.Count > MaxFilesPerUpload || existing + files.Count > MaxImages)
        {
            ModelState.AddModelError("files", t["admin.tooManyImages", MaxImages]);
            return result;
        }
        foreach (var file in files)
        {
            if (file.Length > ImageValidator.MaxBytes)
            {
                ModelState.AddModelError("files", t["admin.imageTooLarge", file.FileName]);
                continue;
            }
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var data = ms.ToArray();
            var type = ImageValidator.DetectContentType(data);
            if (type == null)
            {
                ModelState.AddModelError("files", t["admin.imageInvalid", file.FileName]);
                continue;
            }
            result.Add((data, type));
        }
        return result;
    }

    private ProductVM Vm(Product product) => new()
    {
        Product = product,
        CategoryList = CategoryList(),
        ImageIds = ImageIds(product.Id),
    };

    private IReadOnlyList<Guid> ImageIds(int productId) => productId == 0 ? [] :
        unitOfWork.ProductImage.Query().Where(i => i.ProductId == productId)
            .OrderBy(i => i.SortOrder).Select(i => i.Id).ToList();

    private List<SelectListItem> CategoryList()
    {
        var english = Culture.IsEnglish;
        return unitOfWork.Category.Query().OrderBy(c => c.DisplayOrder).ToList()
            .Select(c => new SelectListItem(c.LocalName(english), c.Id.ToString())).ToList();
    }
}
