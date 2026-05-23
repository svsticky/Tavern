<#import "template.ftl" as layout>
<@layout.emailLayout>
    <h2 style="margin-top: 0; color: #333;">${msg("executeActionsTitle")}</h2>
    <p>${msg("emailHello")} <strong>${user.firstName!user.username},</strong></p>
    
    <p>${msg("executeActionsBody")}</p>
    
    <ul style="color: #333; padding-left: 20px;">
        <#list requiredActions as action>
            <li><strong>${msg("requiredAction.${action}")}</strong></li>
        </#list>
    </ul>

    <p>${msg("executeActionsWarning", linkExpiration)}</p>
</@layout.emailLayout>