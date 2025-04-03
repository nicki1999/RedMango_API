using Microsoft.AspNetCore.Identity;
namespace RedMango_API.Model
{
    public class ApplicationUser: IdentityUser
    {
        public string Name { get; set; }
    }
}
