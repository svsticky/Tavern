import { useKeycloak } from "@react-keycloak/web";
import { t } from "i18next";
import { useEffect, useState } from "react";
import toast from "react-hot-toast";
import { getMailinglists } from "~/api";
import type { Mailinglist, MemberResponseDto } from "~/api/types.gen";
import Tile from "~/components/Tiles/Tile";
import Button from "~/components/UI/Button";
import Checkbox from "~/components/UI/Checkbox";
import Form from "~/components/UI/Form/Form";
import { FormHeader } from "~/components/UI/Form/FormHeader";
import { FormSection } from "~/components/UI/Form/FormSection";
import Input from "~/components/UI/Input";
import { useApp } from "~/context/AppContext";
import {
  handleChangeEmail,
  handleChangePassword,
  handleSaveAccount,
  handleSubscriptionChange,
} from "./ChangeAccountForm.handlers";
import type { ChangeAccountFormData } from "./ChangeAccountForm.types";

/**
 * Renders a form for changing account details.
 * @param {Object} props - The component props.
 * @param {MemberResponseDto} props.member - The member whose account is being changed.
 * @returns {JSX.Element} - The rendered form.
 */
export default function ChangeAccountForm({
  member,
}: {
  member: MemberResponseDto;
}) {
  const { keycloak } = useKeycloak();
  const { setMember } = useApp();
  const [saving, setSaving] = useState(false);
  const [loadingMailingLists, setLoadingMailingLists] = useState(false);

  const [mailingLists, setMailinglists] = useState<Mailinglist[]>([]);
  const [formData, setFormData] = useState<ChangeAccountFormData>({
    phoneNumber: "",
    street: "",
    houseNumber: "",
    postalCode: "",
    city: "",
    parentPhoneNumber: "",
    preferredLanguage: "NL",
    mailSubscriptions: 0,
  });

  const [isFormValid, setIsFormValid] = useState(false);

  useEffect(() => {
    const validateForm = () => {
      const {
        phoneNumber,
        street,
        houseNumber,
        postalCode,
        city,
        parentPhoneNumber,
        preferredLanguage,
      } = formData;
      const basicFieldsFilled = !!(
        phoneNumber &&
        street &&
        houseNumber &&
        postalCode &&
        city &&
        preferredLanguage !== undefined
      );

      let parentPhoneValid = true;
      if (member?.dateOfBirth) {
        const age =
          new Date().getFullYear() - new Date(member.dateOfBirth).getFullYear();
        if (age < 18) parentPhoneValid = !!parentPhoneNumber?.trim();
      }
      setIsFormValid(basicFieldsFilled && parentPhoneValid);
    };
    validateForm();
  }, [formData, member]);

  useEffect(() => {
    setFormData({
      phoneNumber: member.phoneNumber || "",
      street: member.street || "",
      houseNumber: member.houseNumber || "",
      postalCode: member.postalCode || "",
      city: member.city || "",
      parentPhoneNumber: member.parentPhoneNumber || "",
      preferredLanguage: member.preferredLanguage ?? "EN",
      mailSubscriptions: member.mailSubscriptions || 0,
    });
  }, [member]);

  useEffect(() => {
    const fetchMailingLists = async () => {
      setLoadingMailingLists(true);
      try {
        const response = await getMailinglists();

        if (response.error || !response.data)
          throw new Error("Failed to fetch mailing lists");

        setMailinglists(response.data);
      } catch (error) {
        console.error("Error fetching mailing lists:", error);
        toast.error(t("fetch_mailinglists_failed"));
      } finally {
        setLoadingMailingLists(false);
      }
    };

    fetchMailingLists();
  }, []);

  return (
    <Form className="w-full">
      <FormSection title={t("contact_details")}>
        <Input
          label={t("phone_number")}
          required
          value={formData.phoneNumber}
          onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
            setFormData({ ...formData, phoneNumber: e.target.value })
          }
        />
        <Input
          label={t("parent_phone_number")}
          required={
            member?.dateOfBirth
              ? new Date().getFullYear() -
                  new Date(member.dateOfBirth).getFullYear() <
                18
              : false
          }
          value={formData.parentPhoneNumber}
          onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
            setFormData({ ...formData, parentPhoneNumber: e.target.value })
          }
        />
        <div className="flex items-end gap-4 w-full">
          <div className="flex-1">
            <Input
              label={t("email")}
              type="email"
              value={member.email!}
              disabled
              className="border-transparent bg-transparent p-0 cursor-default text-gray-900 disabled:text-gray-900"
            />
          </div>

          <div className="pb-1">
            <a
              className="text-(--board-primary) hover:text-(--board-primary-light) underline decoration-transparent hover:decoration-current hover:cursor-pointer"
              onClick={() => handleChangeEmail(keycloak)}
            >
              {t("change_email")}
            </a>
          </div>
        </div>
      </FormSection>

      <FormSection title={t("address")} columns={3}>
        <div className="md:col-span-2">
          <Input
            label={t("street")}
            required
            value={formData.street}
            onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
              setFormData({ ...formData, street: e.target.value })
            }
          />
        </div>
        <Input
          label={t("house_number")}
          required
          value={formData.houseNumber}
          onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
            setFormData({ ...formData, houseNumber: e.target.value })
          }
        />
        <Input
          label={t("postal_code")}
          required
          value={formData.postalCode}
          onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
            setFormData({ ...formData, postalCode: e.target.value })
          }
        />
        <div className="md:col-span-2">
          <Input
            label={t("city")}
            required
            value={formData.city}
            onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
              setFormData({ ...formData, city: e.target.value })
            }
          />
        </div>
      </FormSection>

      <section>
        <FormHeader title={t("mail_subscriptions")} />
        <Tile className="grid grid-cols-1 md:grid-cols-2 gap-4 p-5 bg-gray-50 border border-gray-100">
          {!loadingMailingLists ? (
            mailingLists.map((list) => (
              <Checkbox
                key={list.id}
                label={list.name}
                checked={(formData.mailSubscriptions & list.bitValue!) !== 0}
                onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
                  handleSubscriptionChange(
                    list.bitValue!,
                    e.target.checked,
                    setFormData,
                  )
                }
              />
            ))
          ) : (
            <p className="text-gray-500 italic">{t("loading")}</p>
          )}
        </Tile>
      </section>

      <FormSection columns={2}>
        <div>
          <FormHeader title={t("preferred_language")} border={false} />
          <div className="flex gap-2">
            <Button
              type="button"
              variant={
                formData.preferredLanguage === "NL" ? "primary" : "secondary"
              }
              className="flex-1"
              onClick={() =>
                setFormData({ ...formData, preferredLanguage: "NL" })
              }
            >
              {t("dutch")}
            </Button>
            <Button
              variant={
                formData.preferredLanguage === "EN" ? "primary" : "secondary"
              }
              type="button"
              className="flex-1"
              onClick={() =>
                setFormData({ ...formData, preferredLanguage: "EN" })
              }
            >
              {t("english")}
            </Button>
          </div>
        </div>
        <div>
          <FormHeader title={t("security")} border={false} />
          <Button
            variant="secondary"
            className="w-full"
            onClick={() => handleChangePassword(keycloak)}
          >
            {t("change_password")}
          </Button>
        </div>
      </FormSection>

      <Button
        onClick={() =>
          member.id &&
          handleSaveAccount(member.id, formData, setSaving, setMember)
        }
        disabled={saving || !isFormValid}
        className="w-full"
      >
        {saving ? t("saving") : t("save")}
      </Button>
    </Form>
  );
}
