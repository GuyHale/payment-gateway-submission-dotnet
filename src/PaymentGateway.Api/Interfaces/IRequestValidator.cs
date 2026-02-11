using System.Diagnostics.CodeAnalysis;

namespace PaymentGateway.Api.Interfaces;

public interface IRequestValidator<T>
    where T: class
{
    bool TryValidate(T instance, [NotNullWhen(false)] out string? errors);
}
