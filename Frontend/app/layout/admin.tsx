import { Outlet } from "react-router";
import StickyLoadingLogo from "~/components/StickyLoadingLogo";
import { requireBoardOrCandidateBoard } from "~/util/loaderAuth.util";

/**
 * Only board and candidate board members can open the admin routes. Redirects to the login page
 * when the user isn't logged in, and shows the forbidden page (403) for everyone else, before any
 * admin page loads.
 */
export async function clientLoader() {
  await requireBoardOrCandidateBoard();
  return null;
}

/**
 * Shown while the `clientLoader` runs on the first page load.
 *
 * @component
 */
export function HydrateFallback() {
  return <StickyLoadingLogo />;
}

/**
 * The layout wrapper for administrative and board-level routes. The access check happens in the
 * route's `clientLoader`, so this only renders the child route.
 *
 * @component
 */
export default function AdminLayout() {
  return <Outlet />;
}
