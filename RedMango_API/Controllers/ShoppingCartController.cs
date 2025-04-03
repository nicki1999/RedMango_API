using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RedMango_API.Data;
using RedMango_API.Model;
using System.Net;

namespace RedMango_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShoppingCartController : ControllerBase
    {
        protected ApiResponse _response;
        private readonly ApplicationDbContext _db;

        public ShoppingCartController(ApplicationDbContext db)
        {
            _db = db;
            _response = new();
        }
        [HttpGet]
        public async Task<ActionResult<ApiResponse>> GetShoppingCart(string userId)
        {
            try
            {
                if(string.IsNullOrEmpty(userId))
                {
                    _response.statusCode = HttpStatusCode.BadRequest;
                    _response.isSuccess = false;
                    return BadRequest(_response);
                }

                ShoppingCart shoppingCart = _db.ShoppingCarts.Include(u => u.CartItems)
                    .ThenInclude(u => u.MenuItem)
                    .FirstOrDefault(u => u.UserId == userId);
                _response.statusCode = HttpStatusCode.OK;
                _response.isSuccess = true;
                return Ok(_response);
            }
            catch(Exception ex)
            {
                _response.isSuccess = false;
                _response.ErrorMessages = new List<string>() { ex.ToString() };
                _response.statusCode = HttpStatusCode.BadRequest;
            }
            return _response;
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse>> AddOrUpdateItemInCart(string userId, int menuItemId, int updateQuantity)
        {
            ShoppingCart shoppingCart = _db.ShoppingCarts.Include(u => u.CartItems).FirstOrDefault(u => u.UserId == userId);
            MenuItem menuItem = _db.MenuItems.FirstOrDefault(u => u.Id == menuItemId);
            if (menuItem == null)
            {
                _response.statusCode = HttpStatusCode.BadRequest;
                _response.isSuccess = false;
                return BadRequest(_response);
            }
            if (shoppingCart == null && updateQuantity > 0)
            {
                //create a shopping cart & add cart item

                ShoppingCart newCart = new() { UserId = userId };
                _db.ShoppingCarts.Add(newCart);
                _db.SaveChanges();

                CartItem newCartItem = new()
                {
                    MenuItemId = menuItemId,
                    Quantity = updateQuantity,
                    ShoppingCartId = newCart.Id,
                    MenuItem = null
                };
                _db.CartItems.Add(newCartItem);
                _db.SaveChanges();
            }
            else
            {
                //shopping cart exists
                CartItem cartIteminCart = shoppingCart.CartItems.FirstOrDefault(u => u.MenuItemId == menuItemId);
                if(cartIteminCart == null)
                {
                    //item does not exist in current cart
                    CartItem newCartItem = new()
                    {
                        MenuItemId = menuItemId,
                        Quantity = updateQuantity,
                        ShoppingCartId = shoppingCart.Id,
                        MenuItem = null
                    };
                    _db.CartItems.Add(newCartItem);
                    _db.SaveChanges();

                }
                else
                {
                    //item exists in cart and we have to update the quantity
                    int newQuantity = cartIteminCart.Quantity + updateQuantity;
                    if(updateQuantity == 0 || newQuantity <= 0)
                    {
                        _db.CartItems.Remove(cartIteminCart);
                        if(shoppingCart.CartItems.Count == 1)
                        {
                            _db.ShoppingCarts.Remove(shoppingCart);
                        }
                        _db.SaveChanges();
                    }
                    else
                    {
                        cartIteminCart.Quantity = newQuantity;
                        _db.SaveChanges();
                    }
                }
            }
                return _response;
        }

    }
}
