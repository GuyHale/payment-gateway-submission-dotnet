namespace PaymentGateway.Api.Models.Responses
{
    public record PostBankSimulatorResponse
    {
        public bool Authorized { get; init; }
        public Guid AuthorizationCode { get; init; }
    }
}
