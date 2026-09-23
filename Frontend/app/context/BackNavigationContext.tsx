import { createContext, useContext, useEffect, useRef, useState } from "react";
import { useLocation, useNavigationType } from "react-router";

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
    setTarget(stack.length > 1 ? stack[stack.length - 2] : undefined);
  }, [location.pathname, navigationType]);

  return (
    <BackNavigationContext.Provider value={target}>
      {children}
    </BackNavigationContext.Provider>
  );
}

/**
 * The pathname a real browser back button would currently land on, or
 * `undefined` if there's no in-app history to go back to (e.g. the page was
 * opened directly via a link).
 */
export function useBackNavigationTarget() {
  return useContext(BackNavigationContext);
}
