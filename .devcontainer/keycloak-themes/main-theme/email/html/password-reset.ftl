<#import "template.ftl" as layout>
<@layout.emailLayout>
    <h2 style="margin-top: 0; color: #333;">${msg("passwordResetTitle")}</h2>
    <p>${msg("emailHello")} <strong>${user.firstName!user.username},</strong></p>
    <p>${msg("passwordResetBody", linkExpiration)}</p>
</@layout.emailLayout>