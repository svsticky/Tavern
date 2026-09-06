import { t } from "i18next";
import { PlusIcon, TrashIcon } from "lucide-react";
import { useEffect, useState } from "react";
import toast from "react-hot-toast";
import type { GroupResponseDto, Role } from "~/api";
import { postGroupsPromoteBoard } from "~/api/sdk.gen";
import ManageExternalLinksDatatable from "~/components/Admin/ManageExternalLinksDatatable/ManageExternalLinksDatatable";
import ManageMailinglistsDatatable from "~/components/Mailinglist/ManageMailinglistsDatatable/ManageMailinglistsDatatable";
import ManageRegisterReasonsDatatable from "~/components/Register/ManageRegisterReasonsDatatable/ManageRegisterReasonsDatatable";
import ManageRegisterSlidesDatatable from "~/components/Register/ManageRegisterSlidesDatatable/ManageRegisterSlidesDatatable";
import ManageRegistrationDocumentsDatatable from "~/components/Register/ManageRegistrationDocumentsDatatable/ManageRegistrationDocumentsDatatable";
import ManageStudiesDatatable from "~/components/Study/ManageStudiesDatatable/ManageStudiesDatatable";
import { NoContentTile } from "~/components/Tiles/NoContentTile";
import Tile from "~/components/Tiles/Tile";
import Button from "~/components/UI/Button";
import Checkbox from "~/components/UI/Checkbox";
import { useConfirm } from "~/components/UI/ConfirmModal/useConfirm";
import Form from "~/components/UI/Form/Form";
import { FormHeader } from "~/components/UI/Form/FormHeader";
import { FormSection } from "~/components/UI/Form/FormSection";
import Input from "~/components/UI/Input";
import { PageHeader } from "~/components/UI/PageHeader";
import Select from "~/components/UI/Select";
import { getEnv } from "~/util/config.utils";
import { appendErrorMessage } from "~/util/error.util";
import {
  getCurrentRoleMappings,
  getGroupOptions,
  getRoleOptions,
  handleAddRoleMapping,
  handleRemoveRoleMapping,
  handleSaveSettings,
  handleSettingsChange,
  loadSettingsPageData,
} from "./settings.handlers";

/**
 * The primary configuration dashboard for the application's global settings.
 *
 * This page serves as a centralized administrative hub for:
 * - **Study Management**: Viewing and editing educational programs.
 * - **Mailing Lists**: Managing association-wide subscription lists.
 * - **Internal Identifiers**: Configuring critical system IDs like the Board and Candidate Board groups.
 * - **Financial Parameters**: Setting transaction fees, VAT codes, and membership prices.
 * - **Accounting Integration**: Mapping GL accounts and relation codes for financial exports.
 * - **Role-to-Email Mapping**: Configuring specific email recipients for system roles.
 *
 * State Management Features:
 * - **Change Tracking**: Uses `newSettings` and `deletedSettings` sets to identify which records
 *   require POST, PATCH, or DELETE operations upon saving.
 * - **Validation**: Prevents saving if required fields are empty via `requiredFieldMissing`.
 *
 * @page
 * @component
 */
export default function SettingsPage() {
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [settings, setSettings] = useState<Record<string, string>>({});

  const [newSettings, setNewSettings] = useState<Set<string>>(new Set());
  const [deletedSettings, setDeletedSettings] = useState<Set<string>>(
    new Set(),
  );

  const [availableGroups, setAvailableGroups] = useState<GroupResponseDto[]>(
    [],
  );
  const [availableRoles, setAvailableRoles] = useState<Role[]>([]);
  const [selectedRoleId, setSelectedRoleId] = useState("");

  useEffect(() => {
    loadSettingsPageData({
      setSettings,
      setAvailableGroups,
      setAvailableRoles,
      setLoading,
    });
  }, []);

  const [isPromotingBoard, setIsPromotingBoard] = useState(false);
  const [confirmModal, confirm] = useConfirm();

  const requiredFieldMissing =
    !settings.BoardGroupId ||
    !settings.CandidateBoardGroupId ||
    !settings.PaymentServiceFee ||
    !settings.MembershipPrice ||
    !settings.BegunstigerPrice ||
    !settings.FinancialEmailSender ||
    !settings.MainBoardMail ||
    !settings.ActivityUpdateEmailSender ||
    !settings.FinancialYearStartDate ||
    !settings.CommitteeCreationDate ||
    !settings.YearlyMailSendDate;

  const groupOptions = getGroupOptions(availableGroups);
  const roleOptions = getRoleOptions(availableRoles, settings);
  const currentRoleMappings = getCurrentRoleMappings(settings);

  const handlePromoteBoard = async () => {
    if (!(await confirm(t("are_you_sure_promote_board")))) {
      return;
    }
    try {
      setIsPromotingBoard(true);
      await postGroupsPromoteBoard({ throwOnError: true });
      toast.success(t("promote_board_success"));
    } catch (err) {
      console.error("Failed to promote board:", err);
      toast.error(appendErrorMessage(t("promote_board_error"), err));
    } finally {
      setIsPromotingBoard(false);
    }
  };

  if (loading) return t("loading");

  return (
    <div className="flex flex-col max-w-4xl mx-auto w-full">
      <PageHeader title={t("system_settings")} />

      <div className="space-y-4">
        <div>
          <FormHeader title={t("board_rotation")} />
          <Tile className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4 p-6">
            <div>
              <h4 className="font-semibold text-slate-800">
                {t("board_rotation_title")}
              </h4>
              <p className="text-sm text-slate-500 max-w-xl">
                {t("board_rotation_desc")}
              </p>
            </div>
            <Button
              type="button"
              variant="primary"
              onClick={handlePromoteBoard}
              disabled={isPromotingBoard}
              className="whitespace-nowrap shrink-0"
            >
              {isPromotingBoard ? t("loading") : t("run_board_rotation")}
            </Button>
          </Tile>
        </div>

        <div>
          <FormHeader title={t("studies")} />
          <ManageStudiesDatatable />
        </div>

        <div>
          <FormHeader title={t("registration_documents")} />
          <ManageRegistrationDocumentsDatatable />
        </div>

        <div>
          <FormHeader title={t("registration_reasons")} />
          <ManageRegisterReasonsDatatable />
        </div>

        <div>
          <FormHeader title={t("registration_slideshow")} />
          <ManageRegisterSlidesDatatable />
        </div>

        <div>
          <FormHeader title={t("external_links")} />
          <ManageExternalLinksDatatable />
        </div>

        <Form>
          <FormSection title={t("internal_identifiers")} columns={2}>
            <Select
              label={t("board_group")}
              value={settings.BoardGroupId || ""}
              onChange={(e) =>
                handleSettingsChange(
                  "BoardGroupId",
                  e.target.value,
                  setSettings,
                )
              }
              options={groupOptions}
              required
            />
            <Select
              label={t("candidate_board_group")}
              value={settings.CandidateBoardGroupId || ""}
              onChange={(e) =>
                handleSettingsChange(
                  "CandidateBoardGroupId",
                  e.target.value,
                  setSettings,
                )
              }
              options={groupOptions}
              required
            />
          </FormSection>

          <FormSection title={t("branding")} columns={3}>
            <Input
              label={t("board_primary_light")}
              type="color"
              value={settings.BoardPrimaryLight || ""}
              onChange={(e) =>
                handleSettingsChange(
                  "BoardPrimaryLight",
                  e.target.value,
                  setSettings,
                )
              }
              className="h-12 cursor-pointer p-1"
            />
            <Input
              label={t("board_primary")}
              type="color"
              value={settings.BoardPrimary || ""}
              onChange={(e) =>
                handleSettingsChange(
                  "BoardPrimary",
                  e.target.value,
                  setSettings,
                )
              }
              className="h-12 cursor-pointer p-1"
            />
            <Input
              label={t("board_primary_dark")}
              type="color"
              value={settings.BoardPrimaryDark || ""}
              onChange={(e) =>
                handleSettingsChange(
                  "BoardPrimaryDark",
                  e.target.value,
                  setSettings,
                )
              }
              className="h-12 cursor-pointer p-1"
            />
          </FormSection>

          <FormSection
            title={t("membership_and_association", "Lidmaatschap & Vereniging")}
            columns={2}
          >
            <Input
              label={t("membership_price")}
              type="number"
              step="0.01"
              value={settings.MembershipPrice || ""}
              onChange={(e) =>
                handleSettingsChange(
                  "MembershipPrice",
                  e.target.value,
                  setSettings,
                )
              }
              required
            />
            <Input
              label={t("membership_payment_expiration_time")}
              type="number"
              step="1"
              value={settings.MembershipPaymentExpirationTime || ""}
              onChange={(e) =>
                handleSettingsChange(
                  "MembershipPaymentExpirationTime",
                  e.target.value.trim(),
                  setSettings,
                )
              }
            />
            <Input
              label={t("financial_year_start_date")}
              placeholder="MM-DD"
              value={settings.FinancialYearStartDate || ""}
              onChange={(e) =>
                handleSettingsChange(
                  "FinancialYearStartDate",
                  e.target.value.trim(),
                  setSettings,
                )
              }
              required
            />
            <Input
              label={t("committee_creation_date")}
              placeholder="MM-DD"
              value={settings.CommitteeCreationDate || ""}
              onChange={(e) =>
                handleSettingsChange(
                  "CommitteeCreationDate",
                  e.target.value.trim(),
                  setSettings,
                )
              }
              required
            />
            <Input
              label={t("study_start_dates")}
              placeholder="MM-DD, MM-DD"
              value={settings.StudyStartDates || ""}
              onChange={(e) =>
                handleSettingsChange(
                  "StudyStartDates",
                  e.target.value.trim(),
                  setSettings,
                )
              }
              required
            />
            <Input
              label={t("membership_vat_code")}
              type="number"
              step="1"
              value={settings.MembershipVATCode || ""}
              onChange={(e) =>
                handleSettingsChange(
                  "MembershipVATCode",
                  e.target.value,
                  setSettings,
                )
              }
            />
            <Input
              label={t("begunstiger_price")}
              type="number"
              step="0.01"
              value={settings.BegunstigerPrice || ""}
              onChange={(e) =>
                handleSettingsChange(
                  "BegunstigerPrice",
                  e.target.value,
                  setSettings,
                )
              }
              required
            />
            <Tile className="bg-gray-50 border border-gray-100 col-span-1 md:col-span-2">
              <div className="flex flex-col">
                <FormHeader title={t("should_pay_membership")} />
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 px-5 pb-3">
                  <Checkbox
                    label={t("masters")}
                    checked={settings.MastersShouldPayMembership !== "0"}
                    onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
                      handleSettingsChange(
                        "MastersShouldPayMembership",
                        e.target.checked ? "1" : "0",
                        setSettings,
                      )
                    }
                  />
                  <Checkbox
                    label={t("gratie")}
                    checked={settings.GratieShouldPayMembership !== "0"}
                    onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
                      handleSettingsChange(
                        "GratieShouldPayMembership",
                        e.target.checked ? "1" : "0",
                        setSettings,
                      )
                    }
                  />
                  <Checkbox
                    label={t("ere_lid")}
                    checked={settings.ErelidShouldPayMembership !== "0"}
                    onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
                      handleSettingsChange(
                        "ErelidShouldPayMembership",
                        e.target.checked ? "1" : "0",
                        setSettings,
                      )
                    }
                  />
                  <Checkbox
                    label={t("lid_van_verdienste")}
                    checked={
                      settings.LidVanVerdiensteShouldPayMembership !== "0"
                    }
                    onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
                      handleSettingsChange(
                        "LidVanVerdiensteShouldPayMembership",
                        e.target.checked ? "1" : "0",
                        setSettings,
                      )
                    }
                  />
                </div>
              </div>
            </Tile>
          </FormSection>

          <FormSection
            title={t("financial_and_payments", "Financiën & Betalingen")}
            columns={2}
          >
            <Input
              label={t("payment_service_fee")}
              type="number"
              step="0.01"
              value={settings.PaymentServiceFee || ""}
              onChange={(e) =>
                handleSettingsChange(
                  "PaymentServiceFee",
                  e.target.value,
                  setSettings,
                )
              }
              required
            />
            <Input
              label={t("payment_service_fee_vat_code")}
              type="number"
              step="1"
              value={settings.PaymentServiceFeeVATCode || ""}
              onChange={(e) =>
                handleSettingsChange(
                  "PaymentServiceFeeVATCode",
                  e.target.value,
                  setSettings,
                )
              }
            />
          </FormSection>

          <FormSection title={t("accounting")} columns={2}>
            <Input
              label={t("membership_gl_account")}
              value={settings.MembershipGLAccount || ""}
              onChange={(e) =>
                handleSettingsChange(
                  "MembershipGLAccount",
                  e.target.value,
                  setSettings,
                )
              }
            />
            <Input
              label={t("membership_cost_center")}
              value={settings.MembershipCostCenter || ""}
              onChange={(e) =>
                handleSettingsChange(
                  "MembershipCostCenter",
                  e.target.value,
                  setSettings,
                )
              }
            />
            <Input
              label={t("membership_cost_unit")}
              value={settings.MembershipCostUnit || ""}
              onChange={(e) =>
                handleSettingsChange(
                  "MembershipCostUnit",
                  e.target.value,
                  setSettings,
                )
              }
            />
            <Input
              label={t("activity_gl_account")}
              value={settings.ActivityGLAccount || ""}
              onChange={(e) =>
                handleSettingsChange(
                  "ActivityGLAccount",
                  e.target.value,
                  setSettings,
                )
              }
            />
            <Input
              label={t("payment_service_fee_gl_account")}
              value={settings.PaymentServiceFeeGLAccount || ""}
              onChange={(e) =>
                handleSettingsChange(
                  "PaymentServiceFeeGLAccount",
                  e.target.value,
                  setSettings,
                )
              }
            />
            <Input
              label={t("payment_service_fee_cost_center")}
              value={settings.PaymentServiceFeeCostCenter || ""}
              onChange={(e) =>
                handleSettingsChange(
                  "PaymentServiceFeeCostCenter",
                  e.target.value,
                  setSettings,
                )
              }
            />
            <Input
              label={t("payment_service_fee_cost_unit")}
              value={settings.PaymentServiceFeeCostUnit || ""}
              onChange={(e) =>
                handleSettingsChange(
                  "PaymentServiceFeeCostUnit",
                  e.target.value,
                  setSettings,
                )
              }
            />
            <Input
              label={t("begunstiger_gl_account")}
              value={settings.BegunstigerGLAccount || ""}
              onChange={(e) =>
                handleSettingsChange(
                  "BegunstigerGLAccount",
                  e.target.value,
                  setSettings,
                )
              }
            />
            <Input
              label={t("begunstiger_cost_center")}
              value={settings.BegunstigerCostCenter || ""}
              onChange={(e) =>
                handleSettingsChange(
                  "BegunstigerCostCenter",
                  e.target.value,
                  setSettings,
                )
              }
            />
            <Input
              label={t("begunstiger_cost_unit")}
              value={settings.BegunstigerCostUnit || ""}
              onChange={(e) =>
                handleSettingsChange(
                  "BegunstigerCostUnit",
                  e.target.value,
                  setSettings,
                )
              }
            />
            <Input
              label={t("begunstiger_vat_code")}
              type="number"
              step="1"
              value={settings.BegunstigerVATCode || ""}
              onChange={(e) =>
                handleSettingsChange(
                  "BegunstigerVATCode",
                  e.target.value,
                  setSettings,
                )
              }
            />
            <Input
              label={t("payment_service_relation_code")}
              value={settings.PaymentServiceRelationCode || ""}
              onChange={(e) =>
                handleSettingsChange(
                  "PaymentServiceRelationCode",
                  e.target.value,
                  setSettings,
                )
              }
            />
            <Input
              label={t("payment_service_payments_condition")}
              value={settings.PaymentServicePaymentsCondition || ""}
              onChange={(e) =>
                handleSettingsChange(
                  "PaymentServicePaymentsCondition",
                  e.target.value,
                  setSettings,
                )
              }
            />
          </FormSection>

          <FormSection
            title={t("external_services")}
            columns={2}
          >
            <div className="col-span-1 md:col-span-2">
              <Checkbox
                label={t("enable_outline")}
                checked={settings.OutlineEnabled === "true"}
                onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
                  handleSettingsChange(
                    "OutlineEnabled",
                    e.target.checked ? "true" : "false",
                    setSettings,
                  )
                }
              />
            </div>
            {settings.OutlineEnabled === "true" && (
              <>
                <Input
                  label={t("outline_api_url")}
                  placeholder="https://wiki.svsticky.nl"
                  value={settings.OutlineApiUrl || ""}
                  onChange={(e) =>
                    handleSettingsChange(
                      "OutlineApiUrl",
                      e.target.value.trim(),
                      setSettings,
                    )
                  }
                  required
                />
                <Input
                  label={t("outline_api_key")}
                  type="password"
                  value={settings.OutlineApiKey || ""}
                  onChange={(e) =>
                    handleSettingsChange(
                      "OutlineApiKey",
                      e.target.value.trim(),
                      setSettings,
                    )
                  }
                  required
                />
              </>
            )}

            <Select
              label={t("payment_provider")}
              value={settings.PaymentProvider || "MOLLIE"}
              onChange={(e) =>
                handleSettingsChange(
                  "PaymentProvider",
                  e.target.value,
                  setSettings,
                )
              }
              options={[{ value: "MOLLIE", label: "Mollie" }]}
            />
            {(settings.PaymentProvider || "").toUpperCase() === "MOLLIE" ? (
              <Input
                label={t("mollie_api_key")}
                type="password"
                value={settings.MollieApiKey || ""}
                onChange={(e) =>
                  handleSettingsChange(
                    "MollieApiKey",
                    e.target.value,
                    setSettings,
                  )
                }
              />
            ) : (
              <div />
            )}

            <Select
              label={t("mail_subscription_service")}
              value={settings.MailSubscriptionService || ""}
              onChange={(e) =>
                handleSettingsChange(
                  "MailSubscriptionService",
                  e.target.value,
                  setSettings,
                )
              }
              options={[
                { value: "", label: t("none") },
                { value: "MAILCHIMP", label: "Mailchimp" },
              ]}
            />
            {(settings.MailSubscriptionService || "").toUpperCase() ===
            "MAILCHIMP" ? (
              <Input
                label={t("mailchimp_list_key")}
                value={settings.MailchimpListKey || ""}
                onChange={(e) =>
                  handleSettingsChange(
                    "MailchimpListKey",
                    e.target.value,
                    setSettings,
                  )
                }
              />
            ) : (
              <div />
            )}
            {(settings.MailSubscriptionService || "").toUpperCase() ===
              "MAILCHIMP" && (
              <Input
                label={t("mailchimp_api_key")}
                type="password"
                value={settings.MailchimpApiKey || ""}
                onChange={(e) =>
                  handleSettingsChange(
                    "MailchimpApiKey",
                    e.target.value,
                    setSettings,
                  )
                }
              />
            )}

            {getEnv("ACCOUNTING_ENABLED")?.toLowerCase() === "true" && (
              <>
                <Select
                  label={t("accounting_service")}
                  value={settings.AccountingService || ""}
                  onChange={(e) =>
                    handleSettingsChange(
                      "AccountingService",
                      e.target.value,
                      setSettings,
                    )
                  }
                  options={[
                    { value: "", label: t("none") },
                    { value: "EXACT", label: "Exact Online" },
                  ]}
                />
                <div />

                {(settings.AccountingService || "").toUpperCase() ===
                  "EXACT" && (
                  <>
                    <Input
                      label={t("exact_division")}
                      value={settings.ExactDivision || ""}
                      onChange={(e) =>
                        handleSettingsChange(
                          "ExactDivision",
                          e.target.value,
                          setSettings,
                        )
                      }
                    />
                    <Input
                      label={t("exact_access_token")}
                      type="password"
                      value={settings.ExactAccessToken || ""}
                      onChange={(e) =>
                        handleSettingsChange(
                          "ExactAccessToken",
                          e.target.value,
                          setSettings,
                        )
                      }
                    />
                  </>
                )}
              </>
            )}
          </FormSection>

          <FormSection title={t("mail_settings")} columns={2}>
            <Input
              label={t("yearly_mail_send_date")}
              placeholder="MM-DD"
              value={settings.YearlyMailSendDate || ""}
              onChange={(e) =>
                handleSettingsChange(
                  "YearlyMailSendDate",
                  e.target.value.trim(),
                  setSettings,
                )
              }
              required
            />
            <div />
            <Select
              label={t("mail_service")}
              value={settings.MailService || "SMTP"}
              onChange={(e) =>
                handleSettingsChange("MailService", e.target.value, setSettings)
              }
              options={[
                { value: "SMTP", label: "SMTP" },
                { value: "MAILGUN", label: "Mailgun" },
              ]}
            />
            <div />

            {(settings.MailService || "SMTP").toUpperCase() === "MAILGUN" && (
              <>
                <Input
                  label={t("mailgun_token")}
                  type="password"
                  value={settings.MailgunToken || ""}
                  onChange={(e) =>
                    handleSettingsChange(
                      "MailgunToken",
                      e.target.value,
                      setSettings,
                    )
                  }
                />
                <Input
                  label={t("mailgun_api_base_url")}
                  value={settings.MailgunApiBaseUrl || ""}
                  onChange={(e) =>
                    handleSettingsChange(
                      "MailgunApiBaseUrl",
                      e.target.value,
                      setSettings,
                    )
                  }
                />
              </>
            )}

            {(settings.MailService || "SMTP").toUpperCase() === "SMTP" && (
              <>
                <Input
                  label={t("smtp_host")}
                  value={settings.SmtpHost || ""}
                  onChange={(e) =>
                    handleSettingsChange(
                      "SmtpHost",
                      e.target.value,
                      setSettings,
                    )
                  }
                />
                <Input
                  label={t("smtp_port")}
                  type="number"
                  value={settings.SmtpPort || ""}
                  onChange={(e) =>
                    handleSettingsChange(
                      "SmtpPort",
                      e.target.value,
                      setSettings,
                    )
                  }
                />
                <Input
                  label={t("smtp_user")}
                  value={settings.SmtpUser || ""}
                  onChange={(e) =>
                    handleSettingsChange(
                      "SmtpUser",
                      e.target.value,
                      setSettings,
                    )
                  }
                />
                <Input
                  label={t("smtp_pass")}
                  type="password"
                  value={settings.SmtpPass || ""}
                  onChange={(e) =>
                    handleSettingsChange(
                      "SmtpPass",
                      e.target.value,
                      setSettings,
                    )
                  }
                />
                <Checkbox
                  label={t("smtp_starttls")}
                  checked={settings.SmtpStartTls === "true"}
                  onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
                    handleSettingsChange(
                      "SmtpStartTls",
                      e.target.checked ? "true" : "false",
                      setSettings,
                    )
                  }
                />
                <Input
                  label={t("smtp_max_batch_size")}
                  type="number"
                  value={settings.SmtpMaxBatchSize || ""}
                  onChange={(e) =>
                    handleSettingsChange(
                      "SmtpMaxBatchSize",
                      e.target.value,
                      setSettings,
                    )
                  }
                />
              </>
            )}
          </FormSection>

          <div>
            <FormHeader title={t("manage_mailing_lists")} />
            <ManageMailinglistsDatatable />
          </div>

          <FormSection title={t("role_email_mapping")} columns={1}>
            <Input
              label={t("financial_email_sender")}
              value={settings.FinancialEmailSender || ""}
              onChange={(e) =>
                handleSettingsChange(
                  "FinancialEmailSender",
                  e.target.value,
                  setSettings,
                )
              }
              required
            />
            <Input
              label={t("main_board_email")}
              value={settings.MainBoardMail || ""}
              onChange={(e) =>
                handleSettingsChange(
                  "MainBoardMail",
                  e.target.value,
                  setSettings,
                )
              }
              required
            />
            <Input
              label={t("activity_update_email_sender")}
              value={settings.ActivityUpdateEmailSender || ""}
              onChange={(e) =>
                handleSettingsChange(
                  "ActivityUpdateEmailSender",
                  e.target.value,
                  setSettings,
                )
              }
              required
            />
            <div className="space-y-6 pt-2">
              <div className="flex flex-col sm:flex-row items-end gap-4 w-full">
                <div className="flex-1 w-full">
                  <Select
                    label={t("add_new_role_email")}
                    value={selectedRoleId}
                    onChange={(e) => setSelectedRoleId(e.target.value)}
                    options={roleOptions}
                  />
                </div>
                <Button
                  type="button"
                  onClick={() =>
                    handleAddRoleMapping({
                      selectedRoleId,
                      settings,
                      setSettings,
                      setNewSettings,
                      setDeletedSettings,
                      setSelectedRoleId,
                    })
                  }
                  disabled={!selectedRoleId}
                  variant="secondary"
                  className="whitespace-nowrap w-full sm:w-auto sm:mb-1"
                >
                  <PlusIcon className="w-4 h-4 mr-2" />
                  {t("add_mapping")}
                </Button>
              </div>

              <div className="grid grid-cols-1 gap-4">
                {currentRoleMappings.length > 0 ? (
                  currentRoleMappings.map(([key, value]) => {
                    const roleId = key.replace("ROLEMAILMAP_", "");
                    const role = availableRoles.find(
                      (r) => r.id?.toString() === roleId,
                    );

                    return (
                      <div key={key} className="flex items-start gap-4 w-full">
                        <div className="flex-1 min-w-0">
                          <Input
                            label={`${t("email_address_for")} ${role?.name || roleId}`}
                            type="email"
                            placeholder={t("enter_email_for_role")}
                            value={value}
                            onChange={(e) =>
                              handleSettingsChange(
                                key,
                                e.target.value,
                                setSettings,
                              )
                            }
                            className="w-full"
                            required
                          />
                        </div>

                        <button
                          type="button"
                          onClick={() =>
                            handleRemoveRoleMapping({
                              name: key,
                              newSettings,
                              setSettings,
                              setNewSettings,
                              setDeletedSettings,
                            })
                          }
                          className="hover:cursor-pointer mt-7 p-2 flex-shrink-0 text-slate-400 hover:text-red-600 hover:bg-red-50 rounded-lg transition-all"
                          title={t("remove")}
                        >
                          <TrashIcon className="w-5 h-5" />
                        </button>
                      </div>
                    );
                  })
                ) : (
                  <NoContentTile text={t("no_role_email_mappings")} />
                )}
              </div>
            </div>
          </FormSection>

          <div className="pt-6 border-t border-slate-200">
            <Button
              onClick={() =>
                handleSaveSettings({
                  deletedSettings,
                  settings,
                  newSettings,
                  setSaving,
                  clearTracking: () => {
                    setNewSettings(new Set());
                    setDeletedSettings(new Set());
                  },
                })
              }
              disabled={saving || requiredFieldMissing}
              className="w-full h-12 text-lg"
            >
              {saving ? t("saving") : t("save_all_settings")}
            </Button>
          </div>
        </Form>
      </div>
      {confirmModal}
    </div>
  );
}
