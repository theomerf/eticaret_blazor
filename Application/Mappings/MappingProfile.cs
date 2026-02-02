using Application.DTOs;
using AutoMapper;
using Domain.Entities;

namespace Application.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Product, ProductDto>();
            CreateMap<Product, ProductWithDetailsDto>().ReverseMap();
            CreateMap<ProductDtoForUpdate, Product>();
            CreateMap<ProductDtoForCreation, Product>();
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
            CreateMap<CartLineDto, CartLine>().ReverseMap();
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
            CreateMap<OrderLine, OrderLineDto>();
            CreateMap<OrderHistory, OrderHistoryDto>();
            CreateMap<Campaign, CampaignDto>();
            CreateMap<CampaignDtoForCreation, Campaign>();
            CreateMap<CampaignDtoForUpdate, Campaign>();
            CreateMap<Coupon, CouponDto>();
            CreateMap<CouponDtoForCreation, Coupon>();
            CreateMap<CouponDtoForUpdate, Coupon>();
            CreateMap<OrderCampaign, OrderCampaignDto>();
        }
    }
}
