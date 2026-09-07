import { t } from "i18next";
import { useEffect, useState } from "react";
import toast from "react-hot-toast";
import { deleteMembersById, getMembersByIdMailinglists } from "~/api";
import type { MemberMailinglistDto, MemberResponseDto } from "~/api/types.gen";
import Tile from "~/components/Tiles/Tile";
import AdminModeToggle from "~/components/UI/AdminModeToggle";
import Button from "~/components/UI/Button";
import Checkbox from "~/components/UI/Checkbox";
import { useConfirm } from "~/components/UI/ConfirmModal/useConfirm";
import Form from "~/components/UI/Form/Form";
import { FormHeader } from "~/components/UI/Form/FormHeader";
import { FormSection } from "~/components/UI/Form/FormSection";
import Input from "~/components/UI/Input";
import ThemeToggle from "~/components/UI/ThemeToggle";
import { useAdminMode } from "~/context/AdminModeContext";
import { useApp } from "~/context/AppContext";
import { useAuth } from "~/context/AuthContext";
import { getEnv } from "~/util/config.utils";
import { appendErrorMessage } from "~/util/error.util";
import {
  handleChangeEmail,
  handleChangePassword,
  handleConfigure2FA,
  handleSaveAccount,
  handleSubscriptionToggle,
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
  const authService = useAuth();
  const { setMember } = useApp();
  const { isAdminUser } = useAdminMode();
  const [saving, setSaving] = useState(false);
  const [loadingMailingLists, setLoadingMailingLists] = useState(false);
  const [mailingListsUnavailable, setMailingListsUnavailable] = useState(false);
  const [confirmModal, confirm] = useConfirm();

  const [mailingLists, setMailingLists] = useState<MemberMailinglistDto[]>([]);
  const [subscribedIds, setSubscribedIds] = useState<Set<string>>(new Set());
  const [formData, setFormData] = useState<ChangeAccountFormData>({
    phoneNumber: "",
    street: "",
    houseNumber: "",
    postalCode: "",
    city: "",
    parentPhoneNumber: "",
    preferredLanguage: "NL",
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
    });
  }, [member]);

  useEffect(() => {
    const fetchMailingLists = async () => {
      if (!member.id) return;

      setLoadingMailingLists(true);
      try {
        const response = await getMembersByIdMailinglists({
          path: { id: member.id },
        });

        if (response.error || !response.data) {
          throw response.error ?? new Error("Failed to fetch mailing lists");
        }

        setMailingLists(response.data);
        setSubscribedIds(
          new Set(
            response.data
              .filter((list) => list.subscribed)
              .map((list) => list.id!),
          ),
        );
        setMailingListsUnavailable(false);
      } catch (error) {
        console.error("Error fetching mailing lists:", error);
        toast.error(appendErrorMessage(t("fetch_mailinglists_failed"), error));
        setMailingLists([]);
        setMailingListsUnavailable(true);
      } finally {
        setLoadingMailingLists(false);
      }
    };

    fetchMailingLists();
  }, [member.id]);

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
              onClick={() => handleChangeEmail(authService)}
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

      {(mailingLists.length > 0 || loadingMailingLists) && (
        <section>
          <FormHeader title={t("mail_subscriptions")} />
          <Tile className="grid grid-cols-1 md:grid-cols-2 gap-4 p-5 bg-gray-50 border border-gray-100">
            {!loadingMailingLists ? (
              mailingLists.map((list) => (
                <Checkbox
                  key={list.id}
                  label={list.name}
                  checked={subscribedIds.has(list.id!)}
                  onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
                    handleSubscriptionToggle(
                      list.id!,
                      e.target.checked,
                      setSubscribedIds,
                    )
                  }
                />
              ))
            ) : (
              <p className="text-gray-500 italic">{t("loading")}</p>
            )}
          </Tile>
        </section>
      )}
      {mailingListsUnavailable && (
        <p className="text-sm text-gray-500 italic">
          {t("mailinglists_unavailable")}
        </p>
      )}

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
          <FormHeader title={t("theme")} border={false} />
          <ThemeToggle variant="segmented" />
        </div>

        {isAdminUser && (
          <div className="md:col-span-2">
            <FormHeader title={t("view_mode")} border={false} />
            <AdminModeToggle variant="segmented" />
          </div>
        )}
      </FormSection>

      <FormSection columns={1}>
        <div>
          <FormHeader title={t("security")} border={false} />
          <div className="flex flex-col gap-2">
            <div className="flex gap-2">
              <Button
                type="button"
                variant="secondary"
                className="flex-1"
                onClick={() => handleChangePassword(authService)}
              >
                {t("change_password")}
              </Button>
              <Button
                type="button"
                variant="secondary"
                className="flex-1"
                onClick={() => handleConfigure2FA(authService)}
              >
                {t("setup_2fa")}
              </Button>
            </div>

            <a
              href={`${getEnv("KeycloakUrl")}/realms/${getEnv("KeycloakRealm")}/account/#/account-security/signing-in`}
              target="_blank"
              rel="noopener noreferrer"
              className="text-xs text-center text-gray-500 hover:text-gray-700 underline pt-1"
            >
              {t("manage_or_remove_2fa_devices")}
            </a>
          </div>
        </div>
      </FormSection>

      <Button
        onClick={() =>
          member.id &&
          handleSaveAccount(
            member.id,
            formData,
            Array.from(subscribedIds),
            setSaving,
            setMember,
          )
        }
        disabled={saving || !isFormValid}
        className="w-full"
      >
        {saving ? t("saving") : t("save")}
      </Button>
      <Button
        variant="danger"
        className="w-full"
        type="button"
        onClick={async () => {
          if (!(await confirm(t("delete_account_confirmation")))) {
            return;
          }

          try {
            if (!member.id) throw new Error("Failed to fetch member id");
            const response = await deleteMembersById({
              path: { id: member.id },
            });

            if (response.error || response.status >= 400) {
              throw response.error ?? new Error("Failed to delete account");
            }

            toast.success(t("account_deleted_successfully"));
            authService.logout(`${window.location.origin}/login`);
          } catch (err) {
            console.error("Error deleting account:", err);
            toast.error(appendErrorMessage(t("delete_account_error"), err));
          }
        }}
      >
        {t("delete_account")}
      </Button>
      {confirmModal}
    </Form>
  );
}
