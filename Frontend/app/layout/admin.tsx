import { useKeycloak } from "@react-keycloak/web";
import { t } from "i18next";
import { useEffect, useState, type ReactNode } from "react";
import { Outlet, useNavigate } from "react-router";
import { useApp } from "~/context/AppContext";
import { isBoardOrCandidateBoard, isInGroupWithId } from "~/util/group.util";

/**
 * A security-first layout wrapper for administrative and board-level routes.
 * 
 * This component acts as a protected route guard. It verifies that the 
 * authenticated user belongs to either the current active board group or the 
 * candidate board group before allowing access to nested admin features.
 * 
 * Logic Flow:
 * 1. Waits for global context IDs (`boardGroupId`, `candidateBoardGroupId`) to be available.
 * 2. Checks Keycloak group memberships against these IDs.
 * 3. Redirects unauthorized users to the home page (`/`).
 * 4. Displays a loading state while membership verification is in progress.
 * 5. Renders child routes via `<Outlet />` only upon successful authorization.
 * 
 * @component
 */
export default function AdminLayout() {
  const { boardGroupId, candidateBoardGroupId } = useApp();
  const { keycloak } = useKeycloak();
  const navigate = useNavigate();
  const [isLoading, setLoading] = useState(true);

  useEffect(() => {
    if (boardGroupId === null || candidateBoardGroupId === null || !keycloak.tokenParsed) {
      return;
    }
    if (!isInGroupWithId(keycloak.tokenParsed, boardGroupId) && !isInGroupWithId(keycloak.tokenParsed, candidateBoardGroupId)) {
      navigate("/");
      return;
    }

    setLoading(false);
  }, [boardGroupId, candidateBoardGroupId, navigate, keycloak]);

  if (isLoading) {
    return `${t("loading")}`;
  }

  return <Outlet />;;
};
