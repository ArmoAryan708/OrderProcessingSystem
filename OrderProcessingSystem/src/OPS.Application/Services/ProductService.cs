using OPS.Application.DTOs.Responses;
using OPS.Application.Interfaces.Repositories;
using OPS.Application.Interfaces.Services;

namespace OPS.Application.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;

    public ProductService(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<IReadOnlyList<ProductResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var products = await _productRepository.GetAllAsync(cancellationToken);
        return products.Select(p => new ProductResponse(p.Id, p.Name, p.Description, p.Price, p.Stock)).ToList();
    }
}
