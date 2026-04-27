import type { Language } from "~/api";

export type ChangeAccountFormData = {
    phoneNumber: string;
    street: string;
    houseNumber: string;
    postalCode: string;
    city: string;
    parentPhoneNumber: string;
    preferredLanguage: Language;
    mailSubscriptions: number;
};