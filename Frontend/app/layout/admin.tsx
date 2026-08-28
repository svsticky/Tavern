import { t } from "i18next";
import { useEffect, useState } from "react";
import { Outlet, useNavigate } from "react-router";
import { useApp } from "~/context/AppContext";
import { useAuth } from "~/context/AuthContext";
import type { TokenParsed } from "~/types/TokenParsed";
import { hasPermission, isBoardOrCandidateBoard } from "~/util/group.util";

/**
 * Permissions that unlock at least one page under `/admin/*`. Individual pages narrow
 * further from here - this is only the outer "can this user see the admin section at all" gate.
 */
const ADMIN_PERMISSIONS = [
  "ViewMembers",
  "ManageMembers",
  "ManageGroups",
  "ManageRoles",
  "ManageGroupPermissions",
  "ManageRolePermissions",
  "ViewFinances",
  "ManageFinances",
  "EditAllActivities",
  "EditActivityForGroup",
  "EditAnnouncements",
  "ViewPastActivities",
] as const;

const hasAnyAdminPermission = (tokenParsed: TokenParsed | null): boolean =>
  ADMIN_PERMISSIONS.some((permission) =>
    hasPermission(tokenParsed, permission),
  );

/**
 * A security-first layout wrapper for administrative and board-level routes.
 *
 * This component acts as a protected route guard. It verifies that the
 * authenticated user belongs to either the current active board group or the
 * candidate board group before allowing access to nested admin features.
 *
 * Logic Flow:
 * 1. Waits for global context IDs (`boardGroupId`, `candidateBoardGroupId`) to be available.
 * 2. Checks group memberships against these IDs.
 * 3. Redirects unauthorized users to the home page (`/`).
 * 4. Displays a loading state while membership verification is in progress.
 * 5. Renders child routes via `<Outlet />` only upon successful authorization.
 *
 * @component
 */
export default function AdminLayout() {
  const { boardGroupId, candidateBoardGroupId } = useApp();
  const authService = useAuth();
  const [tokenParsed, setTokenParsed] = useState<TokenParsed | null>(null);
  const navigate = useNavigate();
  const [isLoading, setLoading] = useState(true);

  useEffect(() => {
    const loadToken = async () => {
      const token = await authService.getTokenParsed();
      setTokenParsed(token);
    };
    loadToken();
  }, [authService]);

  useEffect(() => {
    if (!tokenParsed) return;
    if (
      boardGroupId === null ||
      candidateBoardGroupId === null ||
      !tokenParsed
    ) {
      return;
    }
    if (
      !isBoardOrCandidateBoard(tokenParsed) &&
      !hasAnyAdminPermission(tokenParsed)
    ) {
      navigate("/");
      return;
    }

    setLoading(false);
  }, [boardGroupId, candidateBoardGroupId, navigate, tokenParsed]);

  if (isLoading) {
    return `${t("loading")}`;
  }

  return <Outlet />;
}
