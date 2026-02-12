using Application.DTOs;
using AutoMapper;
using Domain.Entities;
using System.Text.Json;

namespace Application.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Product, ProductDto>()
                .ForMember(dest => dest.DefaultVariantId, opt => opt.MapFrom(src => src.Variants.FirstOrDefault()!.ProductVariantId))
                .ForMember(dest => dest.Images, opt => opt.MapFrom(src => src.Variants.FirstOrDefault()!.Images ?? new List<ProductImage>()))
                .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.Variants.FirstOrDefault()!.Price))
                .ForMember(dest => dest.DiscountPrice, opt => opt.MapFrom(src => src.Variants.FirstOrDefault()!.DiscountPrice))
                .ForMember(dest => dest.TotalStock, opt => opt.MapFrom(src => src.Variants.Sum(v => v.Stock)));
            CreateMap<Product, ProductWithDetailsDto>()
                .ForMember(dest => dest.Specifications, opt => opt.MapFrom(src =>
                    string.IsNullOrEmpty(src.SpecificationsJson)
                        ? new List<ProductSpecificationDto>()
                        : JsonSerializer.Deserialize<List<ProductSpecificationDto>>(src.SpecificationsJson, (JsonSerializerOptions?)null) ?? new List<ProductSpecificationDto>())
                )
                .ReverseMap()
                .ForMember(dest => dest.SpecificationsJson, opt => opt.MapFrom(src =>
                   JsonSerializer.Serialize(src.Specifications, (JsonSerializerOptions?)null)));
            CreateMap<ProductDtoForUpdate, Product>()
               .ForMember(dest => dest.SpecificationsJson, opt => opt.MapFrom(src =>
                    JsonSerializer.Serialize(src.Specifications, (JsonSerializerOptions?)null)));
            CreateMap<ProductDtoForCreation, Product>()
               .ForMember(dest => dest.SpecificationsJson, opt => opt.MapFrom(src =>
                    JsonSerializer.Serialize(src.Specifications, (JsonSerializerOptions?)null)));
            CreateMap<ProductImage, ProductImageDto>();
            CreateMap<ProductImageDtoForCreation, ProductImage>();
            CreateMap<User, UserDto>();
            CreateMap<UserDtoForCreation, User>();
            CreateMap<UserDtoForUpdate, User>();
            CreateMap<UserDtoForUpdateAdmin, User>();
            CreateMap<Category, CategoryDto>();
            CreateMap<Category, CategoryWithDetailsDto>();
            CreateMap<CategoryDtoForCreation, Category>();
            CreateMap<CategoryDtoForUpdate, Category>();
            CreateMap<UserReview, UserReviewDto>();
            CreateMap<UserReviewDtoForCreation, UserReview>();
            CreateMap<UserReviewDtoForUpdate, UserReview>();
            CreateMap<UserReviewDto, UserReviewDtoForUpdate>();
            CreateMap<CartDto, Cart>().ReverseMap();
            CreateMap<CartDto, Cart>().ReverseMap();

            CreateMap<CartLine, CartLineDto>()
                .ForMember(dest => dest.VariantSpecifications, opt => opt.MapFrom(src =>
                    string.IsNullOrEmpty(src.SpecificationsJson)
                        ? new List<ProductSpecificationDto>()
                        : JsonSerializer.Deserialize<List<ProductSpecificationDto>>(src.SpecificationsJson, (JsonSerializerOptions?)null) ?? new List<ProductSpecificationDto>())
                );
            CreateMap<CartLineDto, CartLine>()
               .ForMember(dest => dest.SpecificationsJson, opt => opt.MapFrom(src =>
                    JsonSerializer.Serialize(src.VariantSpecifications, (JsonSerializerOptions?)null)));
            CreateMap<Notification, NotificationDto>();
            CreateMap<NotificationDtoForCreation, Notification>();
            CreateMap<NotificationDtoForBulkCreation, Notification>();
            CreateMap<Address, AddressDto>();
            CreateMap<AddressDtoForCreation, Address>();
            CreateMap<AddressDtoForUpdate, Address>();
            CreateMap<Order, OrderDto>();
            CreateMap<Order, OrderWithDetailsDto>();
            CreateMap<OrderDtoForCreation, Order>();
            CreateMap<OrderDtoForUpdate, Order>();
            CreateMap<OrderLine, OrderLineDto>()
                .ForMember(dest => dest.VariantSpecifications, opt => opt.MapFrom(src =>
                    string.IsNullOrEmpty(src.SpecificationsJson)
                        ? new List<ProductSpecificationDto>()
                        : JsonSerializer.Deserialize<List<ProductSpecificationDto>>(src.SpecificationsJson, (JsonSerializerOptions?)null) ?? new List<ProductSpecificationDto>()))
                .ReverseMap();
            CreateMap<OrderHistory, OrderHistoryDto>();
            CreateMap<Campaign, CampaignDto>();
            CreateMap<CampaignDtoForCreation, Campaign>();
            CreateMap<CampaignDtoForUpdate, Campaign>();
            CreateMap<Coupon, CouponDto>();
            CreateMap<CouponDtoForCreation, Coupon>();
            CreateMap<CouponDtoForUpdate, Coupon>();
            CreateMap<OrderCampaign, OrderCampaignDto>();
            CreateMap<ProductVariant, ProductVariantDto>()
                .ForMember(dest => dest.VariantSpecifications, opt => opt.MapFrom(src =>
                    string.IsNullOrEmpty(src.VariantSpecificationsJson)
                        ? new List<ProductSpecificationDto>()
                        : JsonSerializer.Deserialize<List<ProductSpecificationDto>>(src.VariantSpecificationsJson, (JsonSerializerOptions?)null) ?? new List<ProductSpecificationDto>())
                );
            CreateMap<ProductVariantDtoForCreation, ProductVariant>()
                .ForMember(dest => dest.VariantSpecificationsJson, opt =>
                    opt.MapFrom(src =>
                        JsonSerializer.Serialize(src.VariantSpecifications, (JsonSerializerOptions?)null)
                    )
                );
            CreateMap<Activity, ActivityDto>();
            CreateMap<CategoryVariantAttribute, CategoryVariantAttributeDto>();
        }
    }
}
