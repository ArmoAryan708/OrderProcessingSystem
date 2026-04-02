using OPS.Application.DTOs.Requests;
using OPS.Application.DTOs.Responses;

namespace OPS.Application.Interfaces.Services;

public interface ICustomerService
{
    Task<CustomerResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<AddressResponse> AddAddressAsync(Guid customerId, AddAddressRequest request, CancellationToken cancellationToken = default);
}
