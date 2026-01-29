using Application.DTOs;
using Application.Queries.RequestParameters;
using Application.Services.Interfaces;
using ETicaret.Helpers;
using ETicaret.Models;
using Microsoft.AspNetCore.Mvc;

namespace ETicaret.Controllers
{
    public class ProductsController : Controller
    {
        private readonly IProductService _productService;
        private readonly IUserReviewService _userReviewService;
        private readonly IFileService _fileService;

        public ProductsController(IProductService productService, IUserReviewService userReviewService, IFileService fileService)
        {
            _productService = productService;
            _userReviewService = userReviewService;
            _fileService = fileService;
        }

        public async Task<IActionResult> Index([FromQuery] ProductRequestParameters p)
        {
            var result = await _productService.GetAllProductsAsync(p);
            var favIds = CookieHelper.GetFavouriteProductIds(Request);
            ViewBag.FavouriteIds = favIds;

            var filterParams = new ProductFilterParameters
            {
                PageSize = p.PageSize,
                Page = p.Page,
                SearchTerm = p.SearchTerm,
                MinPrice = p.MinPrice,
                MaxPrice = p.MaxPrice,
                Brand = p.Brand,
                IsShowCase = p.IsShowCase,
                IsDiscount = p.IsDiscount,
                SortBy = p.SortBy,
                CategoryId = p.CategoryId,
            };

            var model = new ProductListViewModel
            {
                Products = result.products,
                TotalCount = result.count,
                FilterParams = filterParams
            };

            return View(model);
        }

        [HttpGet("products/list")]
        public async Task<IActionResult> ProductsListPartial([FromQuery] ProductRequestParameters p)
        {
            var result = await _productService.GetAllProductsAsync(p);

            var filterParams = new ProductFilterParameters
            {
                PageSize = p.PageSize,
                Page = p.Page,
                SearchTerm = p.SearchTerm,
                MinPrice = p.MinPrice,
                MaxPrice = p.MaxPrice,
                Brand = p.Brand,
                IsShowCase = p.IsShowCase,
                IsDiscount = p.IsDiscount,
                SortBy = p.SortBy,
                CategoryId = p.CategoryId,
                CursorId = p.CursorId,
                CursorPrice = p.CursorPrice,
                CursorRating = p.CursorRating,
                CursorReviewCount = p.CursorReviewCount,
            };

            var model = new ProductListViewModel
            {
                Products = result.products,
                TotalCount = result.count,
                FilterParams = filterParams
            };

            return PartialView("_ProductsListPartial", model);
        }

        [HttpGet("products/cards")]
        public async Task<IActionResult> ProductCardsPartial([FromQuery] ProductRequestParameters p)
        {
            IEnumerable<ProductDto> products;
            bool hasMore;

            if (p.Page > 1 && p.CursorId == null)
            {
                p.PageSize = p.PageSize + 1; // hasMore kontrol� i�in

                var result = await _productService.GetAllProductsAsync(p);

                hasMore = result.products.Count() > (p.PageSize - 1);
                products = result.products.Take(p.PageSize - 1).ToList();

                Response.Headers["X-Has-More"] = hasMore.ToString().ToLower();
                Response.Headers["X-Current-Page"] = p.Page.ToString();

                return PartialView("_ProductCardsPartial", products);
            }

            if (p.CursorId.HasValue)
            {
                p.PageSize = p.PageSize + 1; // hasMore kontrol� i�in

                var result = await _productService.GetAllProductsAsync(p);

                hasMore = result.products.Count() > (p.PageSize - 1);
                products = result.products.Take(p.PageSize - 1).ToList();

                Response.Headers["X-Has-More"] = hasMore.ToString().ToLower();

                return PartialView("_ProductCardsPartial", products);
            }

            p.PageSize = p.PageSize + 1;
            var firstPageResult = await _productService.GetAllProductsAsync(p);

            hasMore = firstPageResult.products.Count() > (p.PageSize - 1);
            products = firstPageResult.products.Take(p.PageSize - 1).ToList();

            Response.Headers["X-Has-More"] = hasMore.ToString().ToLower();

            return PartialView("_ProductCardsPartial", products);
        }

        [HttpGet("products/{slug}")]
        public async Task<IActionResult> Get([FromRoute(Name = "slug")] string slug)
        {
            var product = await _productService.GetOneProductBySlugAsync(slug);
            var recommendedProducts = await _productService.GetRecommendedProductsAsync();

            var favIds = CookieHelper.GetFavouriteProductIds(Request);
            ViewBag.FavouriteIds = favIds;

            var model = new ProductDetailViewModel
            {
                Product = product,
                RelatedProducts = recommendedProducts
            };

            return View(model);
        }

        [HttpGet("product-reviews/{productId}")]
        public IActionResult GetProductReviews([FromRoute(Name = "productId")] int productId)
        {
            return ViewComponent("ProductReviews", new { productId = productId });
        }

        public IActionResult SearchProduct([FromForm] String? searchQuery)
        {
            return RedirectToAction("Index", new { SearchTerm = searchQuery });
        }
    }
}