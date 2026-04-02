using FluentValidation;
using OPS.Application.DTOs.Requests;
using OPS.Application.DTOs.Responses;
using OPS.Application.Interfaces;
using OPS.Application.Interfaces.Repositories;
using OPS.Application.Interfaces.Services;
using OPS.Domain.Entities;
using OPS.Domain.Exceptions;

namespace OPS.Application.Services;

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<RegisterRequest> _registerValidator;

    public CustomerService(ICustomerRepository customerRepository, IUnitOfWork unitOfWork, IValidator<RegisterRequest> registerValidator)
    {
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
        _registerValidator = registerValidator;
    }

    public async Task<CustomerResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _registerValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new OPS.Domain.Exceptions.ValidationException(validation.ToDictionary());

        var existing = await _customerRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existing != null)
            throw new OPS.Domain.Exceptions.ValidationException(new Dictionary<string, string[]>
            {
                ["Email"] = [$"Email '{request.Email}' is already registered."]
            });

        var customer = new Customer
        {
            Name = request.Name,
            Email = request.Email,
            Phone = request.Phone,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };

        await _customerRepository.AddAsync(customer, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return MapToResponse(customer);
    }

    public async Task<IReadOnlyList<CustomerResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var customers = await _customerRepository.GetAllAsync(cancellationToken);
        return customers.Select(MapToResponse).ToList();
    }

    public async Task<AddressResponse> AddAddressAsync(Guid customerId, AddAddressRequest request, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId, cancellationToken)
            ?? throw new NotFoundException("Customer", customerId);

        var address = new Address
        {
            CustomerId = customerId,
            Line1 = request.Line1,
            Line2 = request.Line2,
            City = request.City,
            State = request.State,
            Zip = request.Zip,
            Country = request.Country
        };

        await _customerRepository.AddAddressAsync(address, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return new AddressResponse(address.Id, address.Line1, address.Line2, address.City, address.State, address.Zip, address.Country);
    }

    private static CustomerResponse MapToResponse(Customer c) => new(
        c.Id,
        c.Name,
        c.Email,
        c.Phone,
        c.Addresses.Select(a => new AddressResponse(a.Id, a.Line1, a.Line2, a.City, a.State, a.Zip, a.Country)).ToList()
    );
}
