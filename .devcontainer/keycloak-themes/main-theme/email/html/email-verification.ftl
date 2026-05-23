<#import "template.ftl" as layout>
<@layout.emailLayout>
    <h2 style="margin-top: 0; color: #333;">${msg("emailVerificationTitle")}</h2>
    <p>${msg("emailHello")} <strong>${user.firstName!user.username},</strong></p>
    
    <p>${msg("emailVerificationBody")}</p>
    
    <p style="font-size: 12px; color: #666;">${msg("emailVerificationWarning", linkExpiration)}</p>
</@layout.emailLayout>