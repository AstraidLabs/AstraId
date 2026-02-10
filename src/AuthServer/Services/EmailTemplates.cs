using AuthServer.Localization;

namespace AuthServer.Services;

public static class EmailTemplates
{
    public static (string Subject, string HtmlBody, string TextBody) BuildActivationEmail(string link)
        => BuildActivationEmail(link, SupportedCultures.Default);

    public static (string Subject, string HtmlBody, string TextBody) BuildActivationEmail(string link, string? cultureTag)
    {
        var c = LanguageTagNormalizer.Normalize(cultureTag);
        var subject = EmailText(c, "Email.Activation.Subject");
        var intro = EmailText(c, "Email.Activation.Intro");
        var cta = EmailText(c, "Email.Activation.Cta");
        var outro = EmailText(c, "Email.Activation.Outro");
        var textBody = $"{EmailText(c, "Email.Activation.TextPrefix")} {link}";
        var htmlBody = $"""
                        <p>{intro}</p>
                        <p><a href="{link}">{cta}</a></p>
                        <p>{outro}</p>
                        """;

        return (subject, htmlBody, textBody);
    }

    public static (string Subject, string HtmlBody, string TextBody) BuildResetPasswordEmail(string link)
        => BuildResetPasswordEmail(link, SupportedCultures.Default);

    public static (string Subject, string HtmlBody, string TextBody) BuildResetPasswordEmail(string link, string? cultureTag)
    {
        var c = LanguageTagNormalizer.Normalize(cultureTag);
        var subject = EmailText(c, "Email.Reset.Subject");
        var intro = EmailText(c, "Email.Reset.Intro");
        var cta = EmailText(c, "Email.Reset.Cta");
        var outro = EmailText(c, "Email.Reset.Outro");
        var textBody = $"{EmailText(c, "Email.Reset.TextPrefix")} {link}";
        var htmlBody = $"""
                        <p>{intro}</p>
                        <p><a href="{link}">{cta}</a></p>
                        <p>{outro}</p>
                        """;

        return (subject, htmlBody, textBody);
    }

    public static (string Subject, string HtmlBody, string TextBody) BuildReactivationEmail(string link)
    {
        var subject = "Reaktivace účtu";
        var htmlBody = $"""
                        <p>Pro reaktivaci účtu klikněte na odkaz:</p>
                        <p><a href="{link}">Reaktivovat účet</a></p>
                        """;
        var textBody = $"Pro reaktivaci účtu otevřete: {link}";

        return (subject, htmlBody, textBody);
    }

    public static (string Subject, string HtmlBody, string TextBody) BuildChangeEmailEmail(string link)
    {
        var subject = "Confirm your new email";
        var htmlBody = $"""
                        <p>We received a request to change the email address on your account.</p>
                        <p>To confirm this change, click the link below:</p>
                        <p><a href="{link}">Confirm new email</a></p>
                        <p>If you did not request this, you can safely ignore this email.</p>
                        """;
        var textBody = $"Confirm your new email: {link}";

        return (subject, htmlBody, textBody);
    }

    private static string EmailText(string cultureTag, string key)
    {
        var dictionary = key switch
        {
            "Email.Activation.Subject" => new Dictionary<string, string>
            {
                ["en-US"] = "Confirm your account",
                ["cs-CZ"] = "Potvrzení účtu",
                ["sk-SK"] = "Potvrdenie účtu",
                ["pl-PL"] = "Potwierdzenie konta",
                ["de-DE"] = "Konto bestätigen"
            },
            "Email.Activation.Intro" => new Dictionary<string, string>
            {
                ["en-US"] = "Thank you for registering. Click the link below to verify your account:",
                ["cs-CZ"] = "Děkujeme za registraci. Pro potvrzení účtu klikněte na odkaz:",
                ["sk-SK"] = "Ďakujeme za registráciu. Na potvrdenie účtu kliknite na odkaz:",
                ["pl-PL"] = "Dziękujemy za rejestrację. Kliknij link, aby potwierdzić konto:",
                ["de-DE"] = "Vielen Dank für Ihre Registrierung. Klicken Sie auf den Link, um Ihr Konto zu bestätigen:"
            },
            "Email.Activation.Cta" => new Dictionary<string, string>
            {
                ["en-US"] = "Confirm account",
                ["cs-CZ"] = "Potvrdit účet",
                ["sk-SK"] = "Potvrdiť účet",
                ["pl-PL"] = "Potwierdź konto",
                ["de-DE"] = "Konto bestätigen"
            },
            "Email.Activation.Outro" => new Dictionary<string, string>
            {
                ["en-US"] = "If you did not request this, you can ignore this message.",
                ["cs-CZ"] = "Pokud jste o registraci nežádali, tento e-mail můžete ignorovat.",
                ["sk-SK"] = "Ak ste o registráciu nežiadali, tento e-mail môžete ignorovať.",
                ["pl-PL"] = "Jeśli to nie Ty, zignoruj tę wiadomość.",
                ["de-DE"] = "Wenn Sie dies nicht angefordert haben, können Sie diese Nachricht ignorieren."
            },
            "Email.Activation.TextPrefix" => new Dictionary<string, string>
            {
                ["en-US"] = "To confirm your account open:",
                ["cs-CZ"] = "Pro potvrzení účtu otevřete:",
                ["sk-SK"] = "Na potvrdenie účtu otvorte:",
                ["pl-PL"] = "Aby potwierdzić konto, otwórz:",
                ["de-DE"] = "Zum Bestätigen Ihres Kontos öffnen Sie:"
            },
            "Email.Reset.Subject" => new Dictionary<string, string>
            {
                ["en-US"] = "Password reset",
                ["cs-CZ"] = "Obnovení hesla",
                ["sk-SK"] = "Obnovenie hesla",
                ["pl-PL"] = "Reset hasła",
                ["de-DE"] = "Passwort zurücksetzen"
            },
            "Email.Reset.Intro" => new Dictionary<string, string>
            {
                ["en-US"] = "We received a request to reset your password. Click the link below:",
                ["cs-CZ"] = "Požádali jste o obnovení hesla. Pro nastavení nového hesla klikněte na odkaz:",
                ["sk-SK"] = "Požiadali ste o obnovenie hesla. Kliknite na odkaz nižšie:",
                ["pl-PL"] = "Otrzymaliśmy prośbę o reset hasła. Kliknij poniższy link:",
                ["de-DE"] = "Wir haben eine Anfrage zum Zurücksetzen Ihres Passworts erhalten. Klicken Sie auf den Link:"
            },
            "Email.Reset.Cta" => new Dictionary<string, string>
            {
                ["en-US"] = "Reset password",
                ["cs-CZ"] = "Obnovit heslo",
                ["sk-SK"] = "Obnoviť heslo",
                ["pl-PL"] = "Zresetuj hasło",
                ["de-DE"] = "Passwort zurücksetzen"
            },
            "Email.Reset.Outro" => new Dictionary<string, string>
            {
                ["en-US"] = "If you did not request this, you can ignore this message.",
                ["cs-CZ"] = "Pokud jste o obnovu nežádali, tento e-mail můžete ignorovat.",
                ["sk-SK"] = "Ak ste o obnovu nežiadali, tento e-mail môžete ignorovať.",
                ["pl-PL"] = "Jeśli nie wysyłałeś takiej prośby, zignoruj tę wiadomość.",
                ["de-DE"] = "Wenn Sie dies nicht angefordert haben, können Sie diese Nachricht ignorieren."
            },
            "Email.Reset.TextPrefix" => new Dictionary<string, string>
            {
                ["en-US"] = "To reset your password open:",
                ["cs-CZ"] = "Pro obnovu hesla otevřete:",
                ["sk-SK"] = "Na obnovenie hesla otvorte:",
                ["pl-PL"] = "Aby zresetować hasło, otwórz:",
                ["de-DE"] = "Zum Zurücksetzen Ihres Passworts öffnen Sie:"
            },
            _ => new Dictionary<string, string> { ["en-US"] = key }
        };

        return dictionary.TryGetValue(cultureTag, out var value)
            ? value
            : dictionary[SupportedCultures.Default];
    }
}
