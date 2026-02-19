using Microsoft.Extensions.Options;

namespace AuthServer.Options;

public sealed class EmailOptionsValidator : IValidateOptions<EmailOptions>
{
    public ValidateOptionsResult Validate(string? name, EmailOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.FromEmail))
        {
            return ValidateOptionsResult.Fail("Email:FromEmail is required.");
        }

        var provider = options.GetProviderOrDefault();

        if (provider.Equals("Smtp", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(options.Smtp.Host) || options.Smtp.Port <= 0)
            {
                return ValidateOptionsResult.Fail("Email:Smtp:Host and Email:Smtp:Port are required for SMTP provider.");
            }

            return ValidateOptionsResult.Success;
        }

        if (provider.Equals("SendGrid", StringComparison.OrdinalIgnoreCase))
        {
            return string.IsNullOrWhiteSpace(options.SendGrid.ApiKey)
                ? ValidateOptionsResult.Fail("Email:SendGrid:ApiKey is required for SendGrid provider.")
                : ValidateOptionsResult.Success;
        }

        if (provider.Equals("Mailgun", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(options.Mailgun.ApiKey)
                || string.IsNullOrWhiteSpace(options.Mailgun.Domain))
            {
                return ValidateOptionsResult.Fail("Email:Mailgun:ApiKey and Email:Mailgun:Domain are required for Mailgun provider.");
            }

            return ValidateOptionsResult.Success;
        }

        if (provider.Equals("Postmark", StringComparison.OrdinalIgnoreCase))
        {
            return string.IsNullOrWhiteSpace(options.Postmark.ServerToken)
                ? ValidateOptionsResult.Fail("Email:Postmark:ServerToken is required for Postmark provider.")
                : ValidateOptionsResult.Success;
        }

        if (provider.Equals("Graph", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(options.Graph.TenantId)
                || string.IsNullOrWhiteSpace(options.Graph.ClientId)
                || string.IsNullOrWhiteSpace(options.Graph.ClientSecret)
                || string.IsNullOrWhiteSpace(options.Graph.FromUser))
            {
                return ValidateOptionsResult.Fail("Email:Graph:TenantId, ClientId, ClientSecret and FromUser are required for Graph provider.");
            }

            return ValidateOptionsResult.Success;
        }

        return ValidateOptionsResult.Fail("Email:Provider must be one of: Smtp, SendGrid, Mailgun, Postmark, Graph.");
    }
}
