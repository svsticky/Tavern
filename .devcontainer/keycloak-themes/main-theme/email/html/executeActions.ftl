<#import "template.ftl" as layout>
<@layout.emailLayout buttonText=msg("executeActionsButton") closingText=msg("executeActionsClosing")>
    <p>${msg("emailHello")} <strong>${user.firstName!user.username},</strong></p>

    <#if requiredActions?seq_contains("VERIFY_EMAIL")>
        <h2 style="margin-top: 0; color: #333;">${msg("welcomeTitle")}</h2>
        <p>${msg("welcomeIntro")}</p>
        <p>${msg("welcomeAbout")}</p>
        <p>${msg("welcomeCommunity")}</p>

        <h2 style="color: #333;">${msg("welcomeEducationTitle")}</h2>
        <p>${msg("welcomeEducationBody")}</p>

        <h2 style="color: #333;">${msg("welcomeCareerTitle")}</h2>
        <p>${msg("welcomeCareerBody")}</p>

        <h2 style="color: #333;">${msg("welcomeSocialTitle")}</h2>
        <p>${msg("welcomeSocialBody")}</p>

        <h2 style="color: #333;">${msg("welcomeNextTitle")}</h2>
        <p>${msg("welcomeNextBody")}</p>
    <#else>
        <h2 style="margin-top: 0; color: #333;">${msg("executeActionsTitle")}</h2>
        <p>${msg("executeActionsBody")}</p>
    </#if>

    <ul style="color: #333; padding-left: 20px;">
        <#list requiredActions as action>
            <li><strong>${msg("requiredAction.${action}")}</strong></li>
        </#list>
    </ul>

    <p>${msg("executeActionsWarning", linkExpiration)}</p>
</@layout.emailLayout>
