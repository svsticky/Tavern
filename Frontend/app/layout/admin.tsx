import { t } from "i18next";
import { useEffect } from "react";
import { Outlet, useNavigate } from "react-router";
import { useApp } from "~/context/AppContext";
import { useTokenParsed } from "~/context/AuthContext";
import { isBoardOrCandidateBoard } from "~/util/group.util";

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
 * 3. Redirects unauthorized users to the 403 page (`/403`).
 * 4. Displays a loading state while membership verification is in progress.
 * 5. Renders child routes via `<Outlet />` only upon successful authorization.
 *
 * @component
 */
export default function AdminLayout() {
  const { boardGroupId, candidateBoardGroupId } = useApp();
  // The token is known synchronously, so scroll restoration finds a full-height page on the first commit.
  const tokenParsed = useTokenParsed();
  const navigate = useNavigate();

  const isReady =
    tokenParsed !== null &&
    boardGroupId !== null &&
    candidateBoardGroupId !== null;
  const isAuthorized = isReady && isBoardOrCandidateBoard(tokenParsed);

  useEffect(() => {
    if (isReady && !isAuthorized) {
      navigate("/403");
    }
  }, [isReady, isAuthorized, navigate]);

  if (!isAuthorized) {
    return isReady ? null : `${t("loading")}`;
  }

  return <Outlet />;
}
