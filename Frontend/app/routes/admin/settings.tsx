import { useEffect, useState } from "react";
import { t } from "i18next";
import { 
  getApiGroups, 
  getApiSettings, 
  getApiRoles,
  patchApiSettingsById, 
  postApiSettings,
  deleteApiSettingsById,
  type Setting, 
  type GroupResponseDto, 
  type Role 
} from "~/api";
import { PageHeader } from "~/components/UI/PageHeader";
import { FormSection } from "~/components/UI/Form/FormSection";
import Input from "~/components/UI/Input";
import Button from "~/components/UI/Button";
import Form from "~/components/UI/Form/Form";
import Select from "~/components/UI/Select";
import toast from "react-hot-toast";
import { PlusIcon, TrashIcon } from "lucide-react";
import { NoContentTile } from "~/components/Tiles/NoContentTile";
import ManageStudiesDatatable from "~/components/Study/ManageStudiesDatatable";
import { FormHeader } from "~/components/UI/Form/FormHeader";

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
    async function loadData() {
      try {
        const [settingsRes, groupsRes, rolesRes] = await Promise.all([
          getApiSettings(),
          getApiGroups({ query: { IncludeInactive: true } }),
          getApiRoles()
        ]);

        if (settingsRes.data) {
          const settingsObj = settingsRes.data.reduce((acc: Record<string, string>, s: Setting) => {
            if (s.name) acc[s.name] = s.value || "";
            return acc;
          }, {});
          setSettings(settingsObj);
        }
        if (groupsRes.data) setAvailableGroups(groupsRes.data);
        if (rolesRes.data) setAvailableRoles(rolesRes.data);
      } catch (err) {
        toast.error(t("failed_to_load_settings"));
      } finally {
        setLoading(false);
      }
    }
    loadData();
  }, []);

  const handleChange = (name: string, value: string) => {
    setSettings((prev) => ({ ...prev, [name]: value }));
  };

  const handleAddRoleMapping = () => {
    if (!selectedRoleId) return;
    const settingName = `ROLEMAILMAP_${selectedRoleId}`;
    
    if (settings[settingName] !== undefined) {
      toast.error(t("role_already_added"));
      return;
    }

    setSettings(prev => ({ ...prev, [settingName]: "" }));
    setNewSettings(prev => new Set(prev).add(settingName));
    setDeletedSettings(prev => {
      const next = new Set(prev);
      next.delete(settingName);
      return next;
    });
    setSelectedRoleId(""); 
  };

  const handleRemoveRoleMapping = (name: string) => {
    setSettings(prev => {
      const next = { ...prev };
      delete next[name];
      return next;
    });
    
    if (newSettings.has(name)) {
      setNewSettings(prev => {
        const next = new Set(prev);
        next.delete(name);
        return next;
      });
    } else {
      setDeletedSettings(prev => new Set(prev).add(name));
    }
  };

  const handleSave = async () => {
    setSaving(true);
    
    try {
      const promises = [];

      for (const id of deletedSettings) {
        promises.push(deleteApiSettingsById({ path: { id }, query: { id } }));
      }

      for (const [name, value] of Object.entries(settings)) {
        if (newSettings.has(name)) {
          promises.push(postApiSettings({ query: { id: name, value } }));
        } else {
          promises.push(patchApiSettingsById({
            path: { id: name },
            query: { id: name },
            body: [{ op: "replace", path: "/Value", value: value }] as any,
          }));
        }
      }

      await Promise.all(promises);
      
      setNewSettings(new Set());
      setDeletedSettings(new Set());
      toast.success(t("settings_saved"));
    } catch (err) {
      toast.error(t("failed_to_save_settings"));
    } finally {
      setSaving(false);
    }
  };

  const hasEmptyFields = Object.entries(settings).some(
    ([key, value]) => !value.trim()
  );

  if (loading) return `${t("loading")}...`;

  const groupOptions = [
    { value: "", label: t("select_a_group") },
    ...availableGroups.map(g => ({ value: g.id.toString(), label: g.name }))
  ];

  const roleOptions = [
    { value: "", label: t("select_a_role_to_add") },
    ...availableRoles
      .filter(r => !settings[`ROLEMAILMAP_${r.id}`]) 
      .map(r => ({ value: r.id?.toString() || "", label: r.name }))
  ];

  const currentRoleMappings = Object.entries(settings).filter(([key]) => 
    key.startsWith("ROLEMAILMAP_")
  );

  return (
    <div className="flex flex-col max-w-4xl mx-auto w-full">
      <PageHeader title={t("system_settings")} />

      <div className="space-y-4">
        <div>
          <FormHeader title={t("studies")} />
          <ManageStudiesDatatable />
        </div>

        <Form>
          <FormSection title={t("internal_identifiers")} columns={2}>
            <Select 
              label={t("board_group")} 
              value={settings["BoardGroupId"] || ""} 
              onChange={(e) => handleChange("BoardGroupId", e.target.value)}
              options={groupOptions}
            />
            <Select 
              label={t("candidate_board_group")} 
              value={settings["CandidateBoardGroupId"] || ""} 
              onChange={(e) => handleChange("CandidateBoardGroupId", e.target.value)}
              options={groupOptions}
            />
          </FormSection>
          
          <FormSection title={t("finances")} columns={2}>
            <Input 
              label={t("mollie_fee")} 
              type="number" step="0.01"
              value={settings["MollieFee"] || ""} 
              onChange={(e) => handleChange("MollieFee", e.target.value)} 
            />
            <Input 
              label={t("mollie_fee_vat_code")} 
              type="number" step="1"
              value={settings["MollieFeeVATCode"] || ""} 
              onChange={(e) => handleChange("MollieFeeVATCode", e.target.value)} 
            />
            <Input 
              label={t("membership_price")} 
              type="number" step="0.01"
              value={settings["MembershipPrice"] || ""} 
              onChange={(e) => handleChange("MembershipPrice", e.target.value)} 
            />
            <Input 
              label={t("membership_vat_code")} 
              type="number" step="1"
              value={settings["MembershipVATCode"] || ""} 
              onChange={(e) => handleChange("MembershipVATCode", e.target.value)} 
            />
          </FormSection>

          <FormSection title={t("accounting")} columns={2}>
            <Input 
              label={t("membership_gl_account")} 
              value={settings["MembershipGLAccount"] || ""} 
              onChange={(e) => handleChange("MembershipGLAccount", e.target.value)} 
            />
            <Input 
              label={t("activity_gl_account")} 
              value={settings["ActivityGLAccount"] || ""} 
              onChange={(e) => handleChange("ActivityGLAccount", e.target.value)} 
            />
            <Input 
              label={t("mollie_fee_gl_account")} 
              value={settings["MollieFeeGLAccount"] || ""} 
              onChange={(e) => handleChange("MollieFeeGLAccount", e.target.value)} 
            />
            <Input 
              label={t("mollie_relation_code")} 
              value={settings["MollieRelationCode"] || ""} 
              onChange={(e) => handleChange("MollieRelationCode", e.target.value)} 
            />
            <Input 
              label={t("mollie_payments_condition")} 
              value={settings["MolliePaymentsCondition"] || ""} 
              onChange={(e) => handleChange("MolliePaymentsCondition", e.target.value)} 
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
                    onClick={handleAddRoleMapping} 
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
                            onChange={(e) => handleChange(key, e.target.value)} 
                            className="w-full"
                            />
                        </div>
                        
                        <button 
                            type="button"
                            onClick={() => handleRemoveRoleMapping(key)}
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
                onClick={handleSave} 
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