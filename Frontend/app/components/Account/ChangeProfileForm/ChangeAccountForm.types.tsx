import type { Language } from "~/api";

/**
 * Represents the form data for changing account details.
 */
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
