﻿using Microsoft.EntityFrameworkCore;
using SWP391_CareSkin_BE.DTOS.Requests;
using SWP391_CareSkin_BE.DTOS.Responses;
using SWP391_CareSkin_BE.Mappers;
using SWP391_CareSkin_BE.Models;
using SWP391_CareSkin_BE.Repositories.Interfaces;
using SWP391_CareSkin_BE.Services.Interfaces;
using SWP391_CareSkin_BE.Extensions;

namespace SWP391_CareSkin_BE.Services.Implementations
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly IFirebaseService _firebaseService;

        public ProductService(IProductRepository productRepository, IFirebaseService firebaseService)
        {
            _productRepository = productRepository;
            _firebaseService = firebaseService;
        }

        public async Task<List<ProductDTO>> GetAllProductsAsync()
        {
            var products = await _productRepository.GetAllProductsAsync();
            return products.Select(ProductMapper.ToDTO).ToList();
        }

        public async Task<ProductDTO> GetProductByIdAsync(int productId)
        {
            var product = await _productRepository.GetProductByIdAsync(productId);
            return ProductMapper.ToDTO(product);
        }

        public async Task<ProductDTO> CreateProductAsync(ProductCreateRequestDTO request, string pictureUrl)
        {
            var productEntity = ProductMapper.ToEntity(request, pictureUrl);
            await _productRepository.AddProductAsync(productEntity);
            var createdProduct = await _productRepository.GetProductByIdAsync(productEntity.ProductId);
            return ProductMapper.ToDTO(createdProduct);
        }

        public async Task<ProductDTO> UpdateProductAsync(int productId, ProductUpdateRequestDTO request, string pictureUrl)
        {
            var existingProduct = await _productRepository.GetProductByIdAsync(productId);
            if (existingProduct == null) 
                return null;

            // Only update fields if they are different from existing values
            if (request.ProductName != existingProduct.ProductName)
                existingProduct.ProductName = request.ProductName;

            if (request.BrandId != existingProduct.BrandId)
                existingProduct.BrandId = request.BrandId;

            if (request.Category != existingProduct.Category)
                existingProduct.Category = request.Category;

            if (request.Description != existingProduct.Description)
                existingProduct.Description = request.Description;

            // Update picture URL only if a new picture is provided
            if (!string.IsNullOrEmpty(pictureUrl))
            {
                // Delete old image if exists
                if (!string.IsNullOrEmpty(existingProduct.PictureUrl))
                {
                    var oldFileName = existingProduct.PictureUrl.Split('/').Last();
                    await _firebaseService.DeleteImageAsync(oldFileName);
                }
                existingProduct.PictureUrl = pictureUrl;
            }

            // Update variations if provided
            if (request.Variations != null && request.Variations.Any())
            {
                existingProduct.ProductVariations.Clear();
                foreach (var variation in request.Variations)
                {
                    existingProduct.ProductVariations.Add(new ProductVariation
                    {
                        Ml = variation.Ml,
                        Price = variation.Price
                    });
                }
            }

            // Update main ingredients if provided
            if (request.MainIngredients != null && request.MainIngredients.Any())
            {
                existingProduct.ProductMainIngredients.Clear();
                foreach (var ingredient in request.MainIngredients)
                {
                    existingProduct.ProductMainIngredients.Add(new ProductMainIngredient
                    {
                        IngredientName = ingredient.IngredientName,
                        Description = ingredient.Description
                    });
                }
            }

            // Update detail ingredients if provided
            if (request.DetailIngredients != null && request.DetailIngredients.Any())
            {
                existingProduct.ProductDetailIngredients.Clear();
                foreach (var ingredient in request.DetailIngredients)
                {
                    existingProduct.ProductDetailIngredients.Add(new ProductDetailIngredient
                    {
                        IngredientName = ingredient.IngredientName
                    });
                }
            }

            // Update usages if provided
            if (request.Usages != null && request.Usages.Any())
            {
                existingProduct.ProductUsages.Clear();
                foreach (var usage in request.Usages)
                {
                    existingProduct.ProductUsages.Add(new ProductUsage
                    {
                        Step = usage.Step,
                        Instruction = usage.Instruction
                    });
                }
            }

            await _productRepository.UpdateProductAsync(existingProduct);
            var updatedProduct = await _productRepository.GetProductByIdAsync(productId);
            return ProductMapper.ToDTO(updatedProduct);
        }

        public async Task<(List<ProductDTO> Products, int TotalCount)> SearchProductsAsync(ProductSearchRequestDTO request)
        {
            var query = _productRepository.GetQueryable(); // Chỉ lấy sản phẩm đang hoạt động

            // Áp dụng các bộ lọc từ extension class
            query = query
                .ApplyKeywordFilter(request.Keyword)
                .ApplyCategoryFilter(request.Category)
                .ApplyBrandFilter(request.BrandId)
                .ApplyPriceFilter(request.MinPrice, request.MaxPrice)
                .ApplyMlFilter(request.MinMl, request.MaxMl)
                .ApplySorting(request.SortBy);

            // Lấy tổng số sản phẩm trước khi phân trang
            var totalCount = await query.CountAsync();

            // Áp dụng phân trang (nếu không có giá trị thì mặc định)
            var pageNumber = request.PageNumber ?? 1;
            var pageSize = request.PageSize ?? 10;

            var products = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Include(p => p.Brand)
                .Include(p => p.ProductVariations)
                .Include(p => p.ProductMainIngredients)
                .ToListAsync();

            return (products.Select(ProductMapper.ToDTO).ToList(), totalCount);
        }

        public async Task<bool> DeleteProductAsync(int productId)
        {
            var product = await _productRepository.GetProductByIdAsync(productId);
            if (product == null)
                return false;

            // Delete image from Firebase if exists
            if (!string.IsNullOrEmpty(product.PictureUrl))
            {
                var fileName = product.PictureUrl.Split('/').Last();
                await _firebaseService.DeleteImageAsync(fileName);
            }

            await _productRepository.DeleteProductAsync(productId);
            return true;
        }
    }
}
