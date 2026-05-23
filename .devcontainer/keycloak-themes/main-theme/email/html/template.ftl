<#macro emailLayout>
<!DOCTYPE html>
<html>
<head>
    <meta name="viewport" content="width=device-width">
    <meta http-equiv="Content-Type" content="text/html; charset=UTF-8">
    <style>
        @media only screen and (max-width: 640px) {
            .container { padding: 0 !important; width: 100% !important; }
            .content { padding: 0 !important; }
            .content-wrap { padding: 10px !important; }
        }
    </style>
</head>
<body style="font-family: 'Helvetica Neue', Helvetica, Arial, sans-serif; background-color: #f6f6f6; margin: 0;">
<table bgcolor="#f6f6f6" style="width: 100%; background-color: #f6f6f6; margin: 0;">
    <tr>
        <td></td>
        <td class="container" width="600" style="display: block; max-width: 600px; margin: 0 auto; vertical-align: top;">
            <div class="content" style="padding: 20px;">
                <table class="main" width="100%" cellpadding="0" cellspacing="0" style="border-radius: 3px; background-color: #fff; border: 1px solid #e9e9e9; border-collapse: separate;">
                    <tr>
                        <td align="center" style="color: #fff; font-weight: 500; text-align: center; border-radius: 3px 3px 0 0; background-color: #fa6b20; padding: 20px;">
                            Studievereniging Sticky
                        </td>
                    </tr>
                    <tr>
                        <td class="content-wrap" style="padding: 30px; vertical-align: top;">
                            <table width="100%" cellpadding="0" cellspacing="0">
                                <tr>
                                    <td style="font-size: 14px; vertical-align: top; padding: 0 0 20px;">
                                        <#nested>
                                    </td>
                                </tr>
                                <#if link??>
                                <tr>
                                    <td align="center" style="padding: 10px 0 20px;">
                                        <a href="${link}" style="color: #FFF; text-decoration: none; font-weight: bold; display: inline-block; border-radius: 5px; background-color: #fa6b20; border-style: solid; border-color: #fa6b20; border-width: 10px 25px;">
                                            ${msg("linkText")}
                                        </a>
                                    </td>
                                </tr>
                                </#if>
                                <tr>
                                    <td style="font-size: 14px; padding-top: 10px; border-top: 1px solid #eee; color: #333;">
                                        ${msg("emailClosing")}<br>
                                        <strong>${msg("emailSignature")}</strong>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                </table>
                <div style="width: 100%; color: #999; padding: 20px 0 0 0; text-align: center; font-size: 12px;">
                    ${msg("emailFooterText")} <a href="https://svsticky.nl" style="color: #999;">svsticky.nl</a>
                </div>
            </div>
        </td>
        <td></td>
    </tr>
</table>
</body>
</html>
</#macro>