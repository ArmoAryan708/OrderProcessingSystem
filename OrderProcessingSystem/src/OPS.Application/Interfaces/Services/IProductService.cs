using OPS.Application.DTOs.Responses;

namespace OPS.Application.Interfaces.Services;

public interface IProductService
{
    Task<IReadOnlyList<ProductResponse>> GetAllAsync(CancellationToken cancellationToken = default);
}
