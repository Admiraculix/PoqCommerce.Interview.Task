﻿using PoqCommerce.Application.Interfaces;
using PoqCommerce.Application.Models;
using PoqCommerce.Application.Models.DTOs;
using PoqCommerce.Domain;
using System.Text.RegularExpressions;

namespace PoqCommerce.Application
{
    public class ProductService : IProductService
    {
        private readonly  IMockyHttpClient _httpClient;
        private List<Product> _products = new List<Product>();

        public ProductService(IMockyHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<FilteredProductsDto> FilterProducts(FilterObject filter)
        {
            var response = await _httpClient.GetAllProductsAsync();
            _products = response.Products.ToList();

            var filteredProducts = _products;

            if (filter.MinPrice.HasValue)
                filteredProducts = filteredProducts.Where(p => p.Price >= filter.MinPrice.Value).ToList();

            if (filter.MaxPrice.HasValue)
                filteredProducts = filteredProducts.Where(p => p.Price <= filter.MaxPrice.Value).ToList();

            if (!string.IsNullOrWhiteSpace(filter.Size))
                filteredProducts = filteredProducts.Where(p => p.Sizes.Contains(filter.Size)).ToList();

            if (!string.IsNullOrWhiteSpace(filter.Highlight))
                filteredProducts = ApplyHighlight(filteredProducts, filter.Highlight);

            var filterObject = new FilterObjectDto
            {
                MinPrice = _products.Min(p => p.Price),
                MaxPrice = _products.Max(p => p.Price),
                Sizes = _products.SelectMany(p => p.Sizes).Distinct().ToList(),
                CommonWords = GetCommonWords()
            };

            var result = new FilteredProductsDto
            {
                Products = filteredProducts.Select(p => new Product
                {
                    Title = p.Title,
                    Price = p.Price,
                    Sizes = p.Sizes,
                    Description = p.Description
                }).ToList(),
                Filter = filterObject
            };

            return result;
        }

        private List<Product> ApplyHighlight(List<Product> products, string highlight)
        {
            var highlightedWords = highlight.Split(',').Select(h => h.Trim().ToLower()).ToList();

            foreach (var product in products)
            {
                foreach (var highlightedWord in highlightedWords)
                {
                    product.Description = product.Description.Replace(highlightedWord, $"<em>{highlightedWord}</em>", StringComparison.OrdinalIgnoreCase);
                }
            }

            return products;
        }

        private List<string> GetCommonWords()
        {
            var wordFrequency = new Dictionary<string, int>();

            foreach (var product in _products)
            {
                var descriptionWithoutTags = StripHtmlTags(product.Description.ToLower());
                var words = descriptionWithoutTags.Split(new[] { ' ', '.', ',', ';', ':', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var word in words)
                {
                    if (word.Length > 2) // Exclude words with less than three characters
                    {
                        if (!wordFrequency.ContainsKey(word))
                        {
                            wordFrequency[word] = 1;
                        }
                        else
                        {
                            wordFrequency[word]++;
                        }
                    }
                }
            }

            var mostCommonWords = wordFrequency.OrderByDescending(kv => kv.Value)
                                              .Skip(5) // Exclude the most common five
                                              .Take(10) // Take the next ten
                                              .Select(kv => kv.Key)
                                              .ToList();

            return mostCommonWords;
        }

        private string StripHtmlTags(string input)
        {
            return Regex.Replace(input, "<.*?>", string.Empty);
        }
    }
}