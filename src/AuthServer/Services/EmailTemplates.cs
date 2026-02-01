namespace AuthServer.Services;

public static class EmailTemplates
{
    public static (string Subject, string HtmlBody, string TextBody) BuildActivationEmail(string link)
    {
        var subject = "Potvrzení účtu";
        var htmlBody = $"""
                        <p>Děkujeme za registraci. Pro potvrzení účtu klikněte na odkaz:</p>
                        <p><a href="{link}">Potvrdit účet</a></p>
                        <p>Pokud jste o registraci nežádali, tento e-mail můžete ignorovat.</p>
                        """;
        var textBody = $"Pro potvrzení účtu otevřete: {link}";

        return (subject, htmlBody, textBody);
    }

    public static (string Subject, string HtmlBody, string TextBody) BuildResetPasswordEmail(string link)
    {
        var subject = "Obnovení hesla";
        var htmlBody = $"""
                        <p>Požádali jste o obnovení hesla. Pro nastavení nového hesla klikněte na odkaz:</p>
                        <p><a href="{link}">Obnovit heslo</a></p>
                        <p>Pokud jste o obnovu nežádali, tento e-mail můžete ignorovat.</p>
                        """;
        var textBody = $"Pro obnovu hesla otevřete: {link}";

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
}
