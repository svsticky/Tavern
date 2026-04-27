import { t } from "i18next";
import type { Dispatch, SetStateAction } from "react";
import toast from "react-hot-toast";
import { patchApiMembersById, type MemberResponseDto } from "~/api";
import i18n from "~/i18n";
import type { ChangeAccountFormData } from "./ChangeAccountForm.types";
import type Keycloak from "keycloak-js";
import { mailSubscriptionMap } from "~/types/MailSubscriptionsMap";

export const handleSubscriptionChange = (flag: number, checked: boolean, setFormData: (formData: SetStateAction<ChangeAccountFormData>) => void) => {
    setFormData(prev => ({
        ...prev,
        mailSubscriptions: checked ? prev.mailSubscriptions | flag : prev.mailSubscriptions & ~flag
    }));
};

export const handleChangePassword = async (keycloak: Keycloak) => {
    if (keycloak) {
        const url = await keycloak.createLoginUrl({
        action: 'UPDATE_PASSWORD',
        redirectUri: window.location.href
        });
        window.location.href = url;
    }
    else{
        window.location.href = "/logout";
    }
}; 

export const handleChangeEmail = async (keycloak: Keycloak) => {
if (keycloak) {
    const url = await keycloak.createLoginUrl({
    action: 'UPDATE_EMAIL',
    redirectUri: window.location.href
    });
    window.location.href = url;
}
else{
    window.location.href = "/logout";
}
}; 

export const handleSaveAccount = async (
        userId: string, 
        formData: ChangeAccountFormData, 
        setSaving: (saving: boolean) => void, 
        setMember: Dispatch<SetStateAction<MemberResponseDto | null>>
    ) => {
    setSaving(true);

    const saveProcess = async () => {
        try {
            await patchApiMembersById({
                path: { id: userId },
                body: [
                { op: "replace", path: "/phoneNumber", value: formData.phoneNumber },
                { op: "replace", path: "/street", value: formData.street },
                { op: "replace", path: "/houseNumber", value: formData.houseNumber },
                { op: "replace", path: "/postalCode", value: formData.postalCode },
                { op: "replace", path: "/city", value: formData.city },
                { op: "replace", path: "/parentPhoneNumber", value: formData.parentPhoneNumber },
                { op: "replace", path: "/preferredLanguage", value: formData.preferredLanguage },
                { op: "replace", path: "/mailSubscriptions", value: formData.mailSubscriptions }
                ]
            });
            i18n.changeLanguage(formData.preferredLanguage === "NL" ? "nl" : "en");

            setMember((prev) => prev ? { ...prev, ...formData, mailSubscriptions: mailSubscriptionMap[formData.mailSubscriptions] } : null);
        } catch (err) {
            console.error("Error saving account:", err);
            throw err;
        } finally {
            setSaving(false);
        }
    };

    toast.promise(saveProcess(), {
        loading: t("saving"),
        success: t("save_successful"),
        error: t("save_failed")
    });
};