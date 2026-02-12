using Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace ETicaret.TagHelpers
{
    [HtmlTargetElement("div", Attributes = "products")]
    public class LastestProductTagHelper : TagHelper
    {
        private readonly IProductService _productService;

        public LastestProductTagHelper(IProductService productService)
        {
            _productService = productService;
        }

        [HtmlAttributeName("number")]
        public int Number { get; set; }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            TagBuilder div = new TagBuilder("div");
            div.AddCssClass("latest-products-container");

            var products = await _productService.GetLatestAsync(Number);

            foreach (var product in products)
            {
                TagBuilder itemDiv = new TagBuilder("div");
                itemDiv.AddCssClass("latest-product-item d-flex align-items-center p-2 border-bottom");

                // Ürün resmi
                TagBuilder imgDiv = new TagBuilder("div");
                imgDiv.AddCssClass("latest-product-img me-2");

                TagBuilder img = new TagBuilder("img");
                img.Attributes.Add("src", $"/images/{product.Images?.First().ImageUrl}" ?? "/img/no-image.jpg");
                img.Attributes.Add("alt", product.ProductName);
                img.AddCssClass("img-fluid rounded");
                img.Attributes.Add("style", "width: 50px; height: 50px; object-fit: cover;");

                imgDiv.InnerHtml.AppendHtml(img);
                itemDiv.InnerHtml.AppendHtml(imgDiv);

                // Ürün bilgileri
                TagBuilder infoDiv = new TagBuilder("div");
                infoDiv.AddCssClass("latest-product-info flex-grow-1");

                // Ürün adı
                TagBuilder nameDiv = new TagBuilder("div");
                nameDiv.AddCssClass("fw-bold text-truncate");
                nameDiv.Attributes.Add("style", "max-width: 160px;");

                TagBuilder nameLink = new TagBuilder("a");
                nameLink.Attributes.Add("href", $"/product/get/{product.ProductId}");
                nameLink.AddCssClass("text-decoration-none text-dark");
                nameLink.InnerHtml.Append(product.ProductName ?? "");

                nameDiv.InnerHtml.AppendHtml(nameLink);
                infoDiv.InnerHtml.AppendHtml(nameDiv);

                // Ürün fiyatı
                TagBuilder priceDiv = new TagBuilder("div");
                priceDiv.AddCssClass("text-primary fw-bold");
                priceDiv.InnerHtml.Append($"{product.Price.ToString("c")}");

                infoDiv.InnerHtml.AppendHtml(priceDiv);
                itemDiv.InnerHtml.AppendHtml(infoDiv);

                div.InnerHtml.AppendHtml(itemDiv);
            }

            // Son ürün yok ise mesaj göster
            if (!products.Any())
            {
                TagBuilder noProductDiv = new TagBuilder("div");
                noProductDiv.AddCssClass("text-center p-3 text-muted fst-italic");
                noProductDiv.InnerHtml.Append("Henüz ürün eklenmemiş.");
                div.InnerHtml.AppendHtml(noProductDiv);
            }

            // Tümünü gör linki
            TagBuilder viewAllDiv = new TagBuilder("div");
            viewAllDiv.AddCssClass("text-end p-2 mt-2");

            TagBuilder viewAllLink = new TagBuilder("a");
            viewAllLink.Attributes.Add("href", "/product/index?SortOrder=newest");
            viewAllLink.AddCssClass("btn btn-sm btn-outline-primary");
            viewAllLink.InnerHtml.Append("Tümünü Gör");

            viewAllDiv.InnerHtml.AppendHtml(viewAllLink);
            div.InnerHtml.AppendHtml(viewAllDiv);

            output.Content.SetHtmlContent(div);
        }
    }
}