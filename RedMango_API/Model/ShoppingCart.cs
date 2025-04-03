using RedMango_API.Model;
using System.ComponentModel.DataAnnotations.Schema;

namespace RedMango_API.Controllers
{
    public class ShoppingCart
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        [NotMapped]
        public string StripePaymentIntentId { get; set; }
        [NotMapped]
        public string ClientSecret { get; set; }

        public ICollection<CartItem> CartItems { get; set; }
        
        // With this no new tables will be added
        [NotMapped]
        public double CartTotal { get; set; }

    }
}
