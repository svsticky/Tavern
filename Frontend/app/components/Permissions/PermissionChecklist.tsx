import { t } from "i18next";
import { XIcon } from "lucide-react";
import { useEffect, useState } from "react";
import toast from "react-hot-toast";
import { ALL_PERMISSIONS, type Permission } from "~/types/Permission";
import { appendErrorMessage } from "~/util/error.util";
import Button from "../UI/Button";
import Checkbox from "../UI/Checkbox";
import Input from "../UI/Input";

const KNOWN_PERMISSIONS = new Set<string>(ALL_PERMISSIONS);

/** Mirrors Backend/Validators/PermissionValidator.cs - keep these two in sync. */
export const MAX_CUSTOM_PERMISSION_LENGTH = 100;
export const MAX_CUSTOM_PERMISSION_COUNT = 20;

/** Turns e.g. "EditActivityForGroup" into "Edit Activity For Group" for display. */
const humanize = (permission: Permission) =>
  permission.replace(/([a-z])([A-Z])/g, "$1 $2");

/**
 * A checklist of the 12 known `Permission` values, plus a free-form section for custom
 * permission strings - for other applications sharing this Keycloak instance to interpret via
 * the group_memberships claim. Tavern's own backend never evaluates the custom strings. Backed
 * by a load/save pair of async callbacks operating on the full raw string[] of granted keys -
 * used for both the group-permission editor and the role-permission editor, the only difference
 * being which endpoints `onLoad`/`onSave` call and which note is shown for the known-permission half.
 *
 * When `allKnownPermissionsGranted` is set (the (candidate) board group always has every
 * permission unconditionally, regardless of what's stored), the 12 known checkboxes render as
 * checked and locked - editing them would be misleading since board status already grants them
 * independently of any stored grant. Custom permissions stay fully editable, since other
 * applications don't know about Tavern's board concept.
 */
export default function PermissionChecklist({
  onLoad,
  onSave,
  note,
  allKnownPermissionsGranted = false,
}: {
  onLoad: () => Promise<string[]>;
  onSave: (permissions: string[]) => Promise<void>;
  note?: string;
  allKnownPermissionsGranted?: boolean;
}) {
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [selected, setSelected] = useState<Set<Permission>>(new Set());
  const [customPermissions, setCustomPermissions] = useState<string[]>([]);
  const [newCustomPermission, setNewCustomPermission] = useState("");

  useEffect(() => {
    let cancelled = false;
    onLoad()
      .then((permissionKeys) => {
        if (cancelled) return;
        setSelected(
          new Set(
            permissionKeys.filter((p): p is Permission =>
              KNOWN_PERMISSIONS.has(p),
            ),
          ),
        );
        setCustomPermissions(
          permissionKeys.filter((p) => !KNOWN_PERMISSIONS.has(p)),
        );
      })
      .catch((error) => {
        console.error("Error loading permissions:", error);
        toast.error(appendErrorMessage(t("loading_failed"), error));
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [onLoad]);

  const toggle = (permission: Permission) => {
    setSelected((prev) => {
      const next = new Set(prev);
      if (next.has(permission)) next.delete(permission);
      else next.add(permission);
      return next;
    });
  };

  const trimmedCustomInput = newCustomPermission.trim();
  const canAddCustomPermission =
    trimmedCustomInput.length > 0 &&
    trimmedCustomInput.length <= MAX_CUSTOM_PERMISSION_LENGTH &&
    !KNOWN_PERMISSIONS.has(trimmedCustomInput) &&
    !customPermissions.includes(trimmedCustomInput) &&
    customPermissions.length < MAX_CUSTOM_PERMISSION_COUNT;

  const addCustomPermission = () => {
    if (!canAddCustomPermission) return;
    setCustomPermissions((prev) => [...prev, trimmedCustomInput]);
    setNewCustomPermission("");
  };

  const removeCustomPermission = (permission: string) => {
    setCustomPermissions((prev) => prev.filter((p) => p !== permission));
  };

  const save = async () => {
    setSaving(true);
    try {
      const knownToSave = allKnownPermissionsGranted
        ? []
        : Array.from(selected);
      await onSave([...knownToSave, ...customPermissions]);
      toast.success(t("save_permissions"));
    } catch (error) {
      console.error("Error saving permissions:", error);
      toast.error(appendErrorMessage(t("save_failed"), error));
    } finally {
      setSaving(false);
    }
  };

  if (loading) return t("loading");

  return (
    <div className="flex flex-col gap-3">
      {note && <p className="text-xs text-gray-500 italic">{note}</p>}
      {allKnownPermissionsGranted && (
        <p className="text-xs text-gray-500 italic">
          {t("all_permissions_granted_note")}
        </p>
      )}
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-2">
        {ALL_PERMISSIONS.map((permission) => (
          <Checkbox
            key={permission}
            label={humanize(permission)}
            checked={allKnownPermissionsGranted || selected.has(permission)}
            disabled={allKnownPermissionsGranted}
            onChange={() => toggle(permission)}
          />
        ))}
      </div>

      <div className="flex flex-col gap-2 border-t border-gray-100 pt-3">
        <p className="text-xs font-semibold uppercase tracking-wider text-gray-400">
          {t("custom_permissions")}
        </p>
        <p className="text-xs text-gray-500 italic">
          {t("custom_permissions_note")}
        </p>

        {customPermissions.length > 0 && (
          <ul className="flex flex-col gap-1">
            {customPermissions.map((permission) => (
              <li
                key={permission}
                className="flex items-center justify-between gap-2 rounded-md bg-gray-50 px-2 py-1 text-sm"
              >
                <span className="break-all">{permission}</span>
                <button
                  type="button"
                  onClick={() => removeCustomPermission(permission)}
                  className="shrink-0 text-gray-400 hover:text-red-600 hover:cursor-pointer"
                  aria-label={t("remove")}
                >
                  <XIcon size={14} />
                </button>
              </li>
            ))}
          </ul>
        )}

        <div className="flex items-end gap-2">
          <div className="flex-1">
            <Input
              label={null}
              placeholder={t("custom_permission_placeholder")}
              value={newCustomPermission}
              maxLength={MAX_CUSTOM_PERMISSION_LENGTH}
              onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
                setNewCustomPermission(e.target.value)
              }
              onKeyDown={(e: React.KeyboardEvent<HTMLInputElement>) => {
                if (e.key === "Enter") {
                  e.preventDefault();
                  addCustomPermission();
                }
              }}
            />
          </div>
          <Button
            type="button"
            variant="secondary"
            onClick={addCustomPermission}
            disabled={!canAddCustomPermission}
          >
            {t("add")}
          </Button>
        </div>
        {customPermissions.length >= MAX_CUSTOM_PERMISSION_COUNT && (
          <p className="text-xs text-red-500">
            {t("custom_permissions_limit_reached")}
          </p>
        )}
      </div>

      <Button
        variant="secondary"
        className="self-start"
        onClick={save}
        disabled={saving}
      >
        {saving ? t("saving") : t("save_permissions")}
      </Button>
    </div>
  );
}
