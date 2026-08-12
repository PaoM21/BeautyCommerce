using AutoMapper;
using BeautyCommerce.Application.Features.Products.DTOs;
using BeautyCommerce.Domain.Entities;

namespace BeautyCommerce.Application.Mappings;

public class ProductProfile : Profile
{
    public ProductProfile()
    {
        CreateMap<CreateProductDto, Product>();

        CreateMap<UpdateProductDto, Product>();

        CreateMap<Product, ProductDto>()
            .ForMember(
                d => d.Images,
                opt => opt.MapFrom(s => s.Images))
            .ForMember(
                d => d.Variants,
                opt => opt.MapFrom(s => s.Variants))
            .ForMember(
                d => d.FromPrice,
                opt => opt.MapFrom(s =>
                    s.Variants.Any()
                        ? s.Variants.Min(v => v.Price)
                        : (decimal?)null));

        CreateMap<Product, ProductDetailDto>();

        CreateMap<ProductImage, ProductImageDto>();

        CreateMap<ProductVariant, ProductVariantDto>();
    }
}