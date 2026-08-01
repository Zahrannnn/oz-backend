namespace Oz.Api.Helpers;

// ponytail: one branded RTL frame for all customer emails; callers pass eyebrow/title/body/CTA slots.
public static class EmailTemplates
{
    public static string Wrap(string title, string eyebrow, string body, string ctaText, string ctaUrl) => $$"""
        <!DOCTYPE html>
        <html lang="ar" dir="rtl">
        <head>
        <meta charset="utf-8">
        <meta name="viewport" content="width=device-width,initial-scale=1">
        <title>{{title}}</title>
        <link rel="preconnect" href="https://fonts.googleapis.com">
        <link href="https://fonts.googleapis.com/css2?family=Plus+Jakarta+Sans:wght@700;800&family=Work+Sans:wght@400;600&display=swap" rel="stylesheet">
        </head>
        <body style="margin:0;padding:24px 16px;background-color:#fff8f0;font-family:'Work Sans',sans-serif;direction:rtl;text-align:right;">
        <table role="presentation" cellpadding="0" cellspacing="0" border="0" style="max-width:560px;width:100%;margin:0 auto;background-color:#ffffff;border:3px solid #1f1b10;border-radius:16px;box-shadow:6px 6px 0 #1f1b10;">
        <tr>
        <td style="padding:32px 24px 0;">
        <p style="font-family:'Plus Jakarta Sans',sans-serif;font-size:12px;font-weight:700;color:#725c00;margin:0 0 8px;">{{eyebrow}}</p>
        <h1 style="font-family:'Plus Jakarta Sans',sans-serif;font-size:32px;font-weight:800;line-height:40px;letter-spacing:-0.01em;color:#1f1b10;margin:0 0 16px;">{{title}}</h1>
        {{body}}
        </td>
        </tr>
        <tr>
        <td style="padding:0 24px 32px;text-align:center;">
        <table role="presentation" cellpadding="0" cellspacing="0" border="0" style="width:100%;">
        <tr>
        <td style="border-radius:8px;border:3px solid #1f1b10;box-shadow:6px 6px 0 #1f1b10;background-color:#ffd200;text-align:center;padding:0;">
        <a href="{{ctaUrl}}" style="display:block;padding:14px 24px;font-family:'Plus Jakarta Sans',sans-serif;font-size:16px;font-weight:700;line-height:24px;color:#1f1b10;text-decoration:none;">{{ctaText}}</a>
        </td>
        </tr>
        </table>
        </td>
        </tr>
        <tr>
        <td style="padding:0 24px 32px;text-align:center;">
        <p style="font-size:11px;line-height:16px;color:#7f765f;margin:0;">Oz School Uniforms</p>
        </td>
        </tr>
        </table>
        </body>
        </html>
        """;
}
