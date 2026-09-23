import { createContext, useContext, useEffect, useRef, useState } from "react";
import { matchPath, useLocation, useNavigationType } from "react-router";

/**
 * Pages you should never land on via a "back" button, even if that's where
 * in-app history actually points: create/edit forms (going "back" into one
 * re-opens something the user just finished with) and the confirm-mail
 * interstitial. Mirrors the create/edit routes in `app/routes.ts`.
 */
const EXCLUDED_BACK_TARGETS = [
  "/activities/create",
  "/activities/edit/:id",
  "/announcements/create",
  "/announcements/edit/:id",
  "/admin/activities/create",
  "/admin/activities/edit/:id",
  "/admin/members/create-member",
  "/admin/members/:id",
  "/admin/groups/:id",
  "/confirm-mail",
];

function isExcludedBackTarget(pathname: string): boolean {
  return EXCLUDED_BACK_TARGETS.some((pattern) => matchPath(pattern, pathname));
}

/** Exported for tests only - components should use `useBackNavigationTarget()`. */
export const BackNavigationContext = createContext<string | undefined>(
  undefined,
);

/**
 * Tracks the app's own in-memory navigation stack so a "back" button (which
 * normally links to a fixed `backTo` path) can tell whether that destination
 * is the same page the browser's real back button would land on. When it is,
 * `PageHeader` acts like a real POP navigation (`navigate(-1)`) instead of a
 * PUSH to a new history entry - PUSH always resets scroll to top by React
 * Router's own design, only POP restores the saved position.
 */
export function BackNavigationProvider({
  children,
}: {
  children: React.ReactNode;
}) {
  const location = useLocation();
  const navigationType = useNavigationType();
  const stackRef = useRef<string[]>([location.pathname]);
  const [target, setTarget] = useState<string | undefined>(undefined);

  useEffect(() => {
    const stack = stackRef.current;
    if (navigationType === "POP") {
      if (stack.length > 1) {
        stack.pop();
      } else {
        stackRef.current = [location.pathname];
      }
    } else if (navigationType === "REPLACE") {
      stack[stack.length - 1] = location.pathname;
    } else if (stack[stack.length - 1] !== location.pathname) {
      stack.push(location.pathname);
    }
    const previous = stack.length > 1 ? stack[stack.length - 2] : undefined;
    setTarget(
      previous != null && !isExcludedBackTarget(previous)
        ? previous
        : undefined,
    );
  }, [location.pathname, navigationType]);

  return (
    <BackNavigationContext.Provider value={target}>
      {children}
    </BackNavigationContext.Provider>
  );
}

/**
 * Whether it's safe to send the user to wherever a real browser back button
 * would currently land: `undefined` when there's no in-app history to go
 * back to (e.g. the page was opened directly via a link) or when that page
 * is a create/edit form or the confirm-mail page - landing back on one of
 * those would be confusing, so callers should fall back to a fixed
 * destination instead. Otherwise, the actual pathname (informational only;
 * callers should navigate with `navigate(-1)`, not push to this path).
 */
export function useBackNavigationTarget() {
  return useContext(BackNavigationContext);
}
