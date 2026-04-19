import { useState, useEffect } from "react";
import { t } from "i18next";
import { getApiMembers, getApiRoles, postApiEnrollments, postApiRolealiases, postApiRoles, type Member, type MemberSummaryDto, type RoleAlias } from "~/api";
import Input from "~/components/UI/Input";
import Select from "../UI/Select";
import Form from "../UI/Form/Form";
import Button from "../UI/Button";
import toast from "react-hot-toast";

export default function CreateRoleOverlay({ onRoleAliasCreated }: { onRoleAliasCreated: (roleAlias: RoleAlias) => void }) {
  const [roles, setRoles] = useState<RoleAlias[]>([]);
  const [loadingRoles, setLoadingRoles] = useState(true);
  const [selectedType, setSelectedType] = useState("RoleAlias");
  const [name, setName] = useState("");
  const [selectedRoleId, setSelectedRoleId] = useState("");
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    const searchMembers = async () => {
      setLoadingRoles(true);
      try {
        const res = await getApiRoles();
        if (res.data) {
          setRoles(res.data);
        }
      } catch (error) {
        console.error("Search error:", error);
      } finally {
        setLoadingRoles(false);
      }
    };

    const timer = setTimeout(searchMembers, 300); 
    return () => clearTimeout(timer);
  }, []);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();

    const postRole = async () => {
        try{
            setLoading(true);
            if (selectedType === "ParentRole") {
                const response = postApiRoles({
                    body: {
                        name,
                    }
                });
            } else if (selectedType === "RoleAlias") {
                const response = await postApiRolealiases({
                    body: {
                        name,
                        roleId: Number(selectedRoleId)
                    }
                });
                
                if(response.data){
                    onRoleAliasCreated({id: (response.data as any).id, name: name, roleId: Number(selectedRoleId)});
                }
            }
        } catch (error) {
            console.error("Error creating role:", error);
        } finally {
            setLoading(false);
        }
    }

    toast.promise(postRole(), {
        loading: t("creating_role"),
        success: t("role_created"),
        error: t("creating_role_failed")
    });
  };

  return (
    <Form onSubmit={(e) => {
        e.preventDefault();
        handleSubmit(e);
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

        <Button type="submit" disabled={loading || loadingRoles || selectedRoleId === "" || name.trim() === ""}>
            {t("create")}
        </Button>

    </Form>
  );
}