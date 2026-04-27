import { useState, useEffect } from "react";
import { t } from "i18next";
import { type Role, type RoleAlias } from "~/api";
import Input from "~/components/UI/Input";
import Select from "../../UI/Select";
import Form from "../../UI/Form/Form";
import Button from "../../UI/Button";
import { fetchRoles, handleCreateRoleSubmit } from "./CreateRoleOverlay.handlers";

export default function CreateRoleOverlay({ onRoleAliasCreated, onRoleCreated }: { onRoleAliasCreated: (roleAlias: RoleAlias) => void; onRoleCreated: (role: Role) => void; }) {
  const [roles, setRoles] = useState<RoleAlias[]>([]);
  const [loadingRoles, setLoadingRoles] = useState(true);
  const [selectedType, setSelectedType] = useState("RoleAlias");
  const [name, setName] = useState("");
  const [selectedRoleId, setSelectedRoleId] = useState("");
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    const timer = setTimeout(() => fetchRoles(setLoadingRoles, setRoles), 300);
    return () => clearTimeout(timer);
  }, []);

  return (
    <Form onSubmit={(e) => {
        handleCreateRoleSubmit({
          e,
          selectedType,
          name,
          selectedRoleId,
          setLoading,
          onRoleCreated,
          onRoleAliasCreated
        });
      }}>
        <Select
            label={t("type")}
            defaultValue="RoleAlias"
            onChange={(e: React.ChangeEvent<HTMLSelectElement>) => setSelectedType(e.target.value)}
            options={[
                { value: "RoleAlias", label: t("role_alias") },
                { value: "ParentRole", label: t("parent_role") },
            ]}
            disabled={loadingRoles || loading}
        />

        {selectedType === "RoleAlias" && (
            <Select
                label={t("parent_role")}
                defaultValue=""
                onChange={(e: React.ChangeEvent<HTMLSelectElement>) => setSelectedRoleId(e.target.value)}
                options={[
                    { value: "", label: t("select_role") },
                    ...roles.map(role => ({ value: role.id!, label: role.name }))
                ]}
                disabled={loadingRoles || loading}
            />
        )}
        
        <Input
            placeholder={t("name")}
            value={name}
            onChange={(e: React.ChangeEvent<HTMLInputElement>) => setName(e.target.value)}
            disabled={loadingRoles || loading}
        />

        <Button type="submit" disabled={loading || loadingRoles || (selectedRoleId === "" && selectedType === "RoleAlias") || name.trim() === ""}>
            {t("create")}
        </Button>

    </Form>
  );
}
