using FluentValidation;
using Transfer.API.Controllers;

namespace Transfer.API.Validators;

public class TransferRequestValidator : AbstractValidator<TransferRequest>
{
    private static readonly string[] ValidCurrencies = { "TRY", "USD", "EUR", "GBP" };
    
    public TransferRequestValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than zero")
            .LessThanOrEqualTo(1000000).WithMessage("Maximum transfer amount is 1,000,000");
        
        RuleFor(x => x.FromAccountId)
            .NotEmpty().WithMessage("Source account is required")
            .NotEqual(x => x.ToAccountId).WithMessage("Cannot transfer to the same account");
        
        RuleFor(x => x.ToAccountId)
            .NotEmpty().WithMessage("Destination account is required");
        
        RuleFor(x => x.Currency)
            .Must(BeValidCurrency).WithMessage($"Currency must be one of: {string.Join(", ", ValidCurrencies)}")
            .When(x => !string.IsNullOrEmpty(x.Currency));
    }
    
    private bool BeValidCurrency(string currency) 
        => ValidCurrencies.Contains(currency?.ToUpper());
}
