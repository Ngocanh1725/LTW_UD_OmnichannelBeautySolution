using System.Collections.Generic;
using Omnichannel.Domain.Entities;

namespace Omnichannel.Web.Models
{
    public class HomeSectionsViewModel
    {
        public List<HomeSectionItemViewModel> Sections { get; set; } = new();
    }

    public class HomeSectionItemViewModel
    {
        public UiSection Section { get; set; } = null!;
        public List<Banner> Banners { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
        public List<Product> FlashSaleProducts { get; set; } = new();
        public List<Product> BestSellers { get; set; } = new();
        public List<Banner> MidBanners { get; set; } = new();
        public List<Post> BlogPosts { get; set; } = new();
    }
}
