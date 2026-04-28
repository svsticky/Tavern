import { useEffect, useState } from "react";
import { t } from "i18next";
import { 
  type GroupResponseDto, 
  type Role 
} from "~/api";
import { PageHeader } from "~/components/UI/PageHeader/PageHeader";
import { FormSection } from "~/components/UI/Form/FormSection";
import Input from "~/components/UI/Input";
import Button from "~/components/UI/Button";
import Form from "~/components/UI/Form/Form";
import Select from "~/components/UI/Select";
import { PlusIcon, TrashIcon } from "lucide-react";
import { NoContentTile } from "~/components/Tiles/NoContentTile";
import { FormHeader } from "~/components/UI/Form/FormHeader";
import {
  getCurrentRoleMappings,
  getGroupOptions,
  getRoleOptions,
  handleAddRoleMapping,
  handleRemoveRoleMapping,
  handleSaveSettings,
  handleSettingsChange,
  loadSettingsPageData
} from "./settings.handlers";
import ManageStudiesDatatable from "~/components/Study/ManageStudiesDatatable/ManageStudiesDatatable";
import ManageMailingListsDatatable from "Mailinglist/ManageMailinglistsDatatable/ManageMailinglistsDatatable";

export default function SettingsPage() {
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [settings, setSettings] = useState<Record<string, string>>({});
  
  const [newSettings, setNewSettings] = useState<Set<string>>(new Set());
  const [deletedSettings, setDeletedSettings] = useState<Set<string>>(new Set());
  
  const [availableGroups, setAvailableGroups] = useState<GroupResponseDto[]>([]);
  const [availableRoles, setAvailableRoles] = useState<Role[]>([]);
  const [selectedRoleId, setSelectedRoleId] = useState("");

  useEffect(() => {
    loadSettingsPageData({ setSettings, setAvailableGroups, setAvailableRoles, setLoading });
  }, []);

  const hasEmptyFields = Object.entries(settings).some(
    ([key, value]) => !value.trim()
  );

  if (loading) return t("loading");

  const groupOptions = getGroupOptions(availableGroups);
  const roleOptions = getRoleOptions(availableRoles, settings);
  const currentRoleMappings = getCurrentRoleMappings(settings);

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
              value={settings["BoardGroupId"] || ""} 
              onChange={(e) => handleSettingsChange("BoardGroupId", e.target.value, setSettings)}
              options={groupOptions}
            />
            <Select 
              label={t("candidate_board_group")} 
              value={settings["CandidateBoardGroupId"] || ""} 
              onChange={(e) => handleSettingsChange("CandidateBoardGroupId", e.target.value, setSettings)}
              options={groupOptions}
            />
          </FormSection>
          
          <FormSection title={t("finances")} columns={2}>
            <Input 
              label={t("mollie_fee")} 
              type="number" step="0.01"
              value={settings["MollieFee"] || ""} 
              onChange={(e) => handleSettingsChange("MollieFee", e.target.value, setSettings)} 
            />
            <Input 
              label={t("mollie_fee_vat_code")} 
              type="number" step="1"
              value={settings["MollieFeeVATCode"] || ""} 
              onChange={(e) => handleSettingsChange("MollieFeeVATCode", e.target.value, setSettings)} 
            />
            <Input 
              label={t("membership_price")} 
              type="number" step="0.01"
              value={settings["MembershipPrice"] || ""} 
              onChange={(e) => handleSettingsChange("MembershipPrice", e.target.value, setSettings)} 
            />
            <Input 
              label={t("membership_vat_code")} 
              type="number" step="1"
              value={settings["MembershipVATCode"] || ""} 
              onChange={(e) => handleSettingsChange("MembershipVATCode", e.target.value, setSettings)} 
            />
          </FormSection>

          <FormSection title={t("accounting")} columns={2}>
            <Input 
              label={t("membership_gl_account")} 
              value={settings["MembershipGLAccount"] || ""} 
              onChange={(e) => handleSettingsChange("MembershipGLAccount", e.target.value, setSettings)} 
            />
            <Input 
              label={t("activity_gl_account")} 
              value={settings["ActivityGLAccount"] || ""} 
              onChange={(e) => handleSettingsChange("ActivityGLAccount", e.target.value, setSettings)} 
            />
            <Input 
              label={t("mollie_fee_gl_account")} 
              value={settings["MollieFeeGLAccount"] || ""} 
              onChange={(e) => handleSettingsChange("MollieFeeGLAccount", e.target.value, setSettings)} 
            />
            <Input 
              label={t("mollie_relation_code")} 
              value={settings["MollieRelationCode"] || ""} 
              onChange={(e) => handleSettingsChange("MollieRelationCode", e.target.value, setSettings)} 
            />
            <Input 
              label={t("mollie_payments_condition")} 
              value={settings["MolliePaymentsCondition"] || ""} 
              onChange={(e) => handleSettingsChange("MolliePaymentsCondition", e.target.value, setSettings)} 
            />
          </FormSection>

          <FormSection title={t("role_email_mapping")} columns={1}>
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
                        setSelectedRoleId
                      })
                    } 
                    disabled={!selectedRoleId}
                    variant="secondary"
                    className="whitespace-nowrap sm:mb-1"
                  >
                    <PlusIcon className="w-4 h-4 mr-2" />
                    {t("add_mapping")}
                  </Button>
                </div>

              <div className="grid grid-cols-1 gap-4">
                {currentRoleMappings.length > 0 ? (
                  currentRoleMappings.map(([key, value]) => {
                    const roleId = key.replace("ROLEMAILMAP_", "");
                    const role = availableRoles.find(r => r.id?.toString() === roleId);
                    
                    return (
                      <div key={key} className="flex items-start gap-4 w-full">
                        <div className="flex-1 min-w-0">
                            <Input 
                            label={`${t("email_address_for")} ${role?.name || roleId}`}
                            type="email"
                            placeholder={t("enter_email_for_role")}
                            value={value} 
                            onChange={(e) => handleSettingsChange(key, e.target.value, setSettings)} 
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
                                setDeletedSettings
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
                    }
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
