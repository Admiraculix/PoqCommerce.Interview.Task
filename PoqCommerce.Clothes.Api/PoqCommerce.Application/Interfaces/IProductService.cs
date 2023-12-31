﻿using PoqCommerce.Application.Models;
using PoqCommerce.Application.Models.DTOs;

namespace PoqCommerce.Application.Interfaces
{
    public interface IProductService
    {
        Task<SeedResultDto> SeedProductsAsync();

        Task<FilteredProductsDto> FilterProductsAsync(FilterObject filter);
    }
}
