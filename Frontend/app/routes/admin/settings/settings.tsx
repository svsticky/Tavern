import ManageMailingListsDatatable from "Mailinglist/ManageMailinglistsDatatable/ManageMailinglistsDatatable";
import { t } from "i18next";
import { PlusIcon, TrashIcon } from "lucide-react";
import { useEffect, useState } from "react";
import type { GroupResponseDto, Role } from "~/api";
import ManageStudiesDatatable from "~/components/Study/ManageStudiesDatatable/ManageStudiesDatatable";
import { NoContentTile } from "~/components/Tiles/NoContentTile";
import Tile from "~/components/Tiles/Tile";
import Button from "~/components/UI/Button";
import Checkbox from "~/components/UI/Checkbox";
import Form from "~/components/UI/Form/Form";
import { FormHeader } from "~/components/UI/Form/FormHeader";
import { FormSection } from "~/components/UI/Form/FormSection";
import Input from "~/components/UI/Input";
import { PageHeader } from "~/components/UI/PageHeader/PageHeader";
import Select from "~/components/UI/Select";
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
 * - **Validation**: Prevents saving if required fields are empty via `hasEmptyFields`.
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

  const hasEmptyFields = Object.entries(settings)
    .filter(([key]) => key !== "MembershipPaymentExpirationTime")
    .some(([_key, value]) => !value.trim());

  const groupOptions = getGroupOptions(availableGroups);
  const roleOptions = getRoleOptions(availableRoles, settings);
  const currentRoleMappings = getCurrentRoleMappings(settings);

  if (loading) return t("loading");

  return (
    <div className="flex flex-col max-w-4xl mx-auto w-full">
      <PageHeader title={t("system_settings")} />

      <div className="space-y-4">
        <div>
          <FormHeader title={t("studies")} />
          <ManageStudiesDatatable />
        </div>

        <div>
          <FormHeader title={t("mail_subscriptions")} />
          <ManageMailingListsDatatable />
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
            />
          </FormSection>

          <FormSection title={t("finances")} columns={2}>
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
            <Tile className="bg-gray-50 border border-gray-100">
              <div className="flex flex-col row-span-2">
                <FormHeader title={t("should_pay_membership")} />
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4 px-5 ">
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
            />
            <div className="space-y-6">
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
              disabled={saving || hasEmptyFields}
              className="w-full h-12 text-lg"
            >
              {saving ? t("saving") : t("save_all_settings")}
            </Button>
          </div>
        </Form>
      </div>
    </div>
  );
}
