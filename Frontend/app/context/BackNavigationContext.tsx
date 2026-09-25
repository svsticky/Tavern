import { createContext, useContext, useEffect, useRef, useState } from "react";
import { matchPath, useLocation, useNavigationType } from "react-router";

/** Create/edit forms and confirm-mail: never a back target. Mirrors `app/routes.ts`. */
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

/** Exported for tests; use `useBackNavigationTarget()`. */
export const BackNavigationContext = createContext<string | undefined>(
  undefined,
);

/** Tracks in-app navigation so `PageHeader` can do a real POP (which restores scroll) instead of a PUSH. */
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

/** The page a real back would land on, or `undefined` if there is none or it's an excluded page. */
export function useBackNavigationTarget() {
  return useContext(BackNavigationContext);
}
