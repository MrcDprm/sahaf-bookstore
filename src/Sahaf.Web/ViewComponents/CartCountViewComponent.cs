using Microsoft.AspNetCore.Mvc;
using Sahaf.DataAccess.Repository.IRepository;
using Sahaf.Web.Infrastructure;

namespace Sahaf.Web.ViewComponents;

// Number of different books in the cart, shown on the header cart icon.
public class CartCountViewComponent(IUnitOfWork unitOfWork) : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        if (UserClaimsPrincipal.Identity?.IsAuthenticated != true) return Content("");
        var userId = UserClaimsPrincipal.UserId();
        var count = unitOfWork.ShoppingCart.Query().Count(c => c.ApplicationUserId == userId);
        return count == 0 ? Content("") : View(count);
    }
}
