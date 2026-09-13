import type React from "react";
import { createContext, useContext, useEffect, useState } from "react";

export const ADMIN_MODE_STORAGE_KEY = "tavern_admin_mode";
export const ADMIN_MODE_CHANGED_EVENT = "tavern_admin_mode_changed";

export interface AdminModeContextType {
  isAdminUser: boolean;
  setIsAdminUser: (isAdmin: boolean) => void;
  adminMode: boolean;
  setAdminMode: (enabled: boolean) => void;
  toggleAdminMode: () => void;
}

export function isAdminModeActive(): boolean {
  if (typeof window === "undefined" || !window.localStorage) return true;
  return localStorage.getItem(ADMIN_MODE_STORAGE_KEY) !== "false";
}

const AdminModeContext = createContext<AdminModeContextType>({
  isAdminUser: false,
  setIsAdminUser: () => {},
  adminMode: true,
  setAdminMode: () => {},
  toggleAdminMode: () => {},
});

export function AdminModeProvider({
  children,
  initialIsAdmin = false,
}: {
  children: React.ReactNode;
  initialIsAdmin?: boolean;
}) {
  const [isAdminUser, setIsAdminUser] = useState<boolean>(initialIsAdmin);
  const [adminMode, setAdminModeState] = useState<boolean>(() =>
    isAdminModeActive(),
  );

  useEffect(() => {
    const handleStorageChange = (e: Event) => {
      const customEvent = e as CustomEvent<{ adminMode: boolean }>;
      if (
        customEvent.detail &&
        typeof customEvent.detail.adminMode === "boolean"
      ) {
        setAdminModeState(customEvent.detail.adminMode);
      } else {
        setAdminModeState(isAdminModeActive());
      }
    };

    window.addEventListener(ADMIN_MODE_CHANGED_EVENT, handleStorageChange);
    return () => {
      window.removeEventListener(ADMIN_MODE_CHANGED_EVENT, handleStorageChange);
    };
  }, []);

  const setAdminMode = (enabled: boolean) => {
    setAdminModeState(enabled);
    if (typeof window !== "undefined" && window.localStorage) {
      localStorage.setItem(ADMIN_MODE_STORAGE_KEY, enabled ? "true" : "false");
      window.dispatchEvent(
        new CustomEvent(ADMIN_MODE_CHANGED_EVENT, {
          detail: { adminMode: enabled },
        }),
      );
    }
  };

  const toggleAdminMode = () => {
    setAdminMode(!adminMode);
  };

  return (
    <AdminModeContext.Provider
      value={{
        isAdminUser,
        setIsAdminUser,
        adminMode: isAdminUser ? adminMode : false,
        setAdminMode,
        toggleAdminMode,
      }}
    >
      {children}
    </AdminModeContext.Provider>
  );
}

export function useAdminMode() {
  return useContext(AdminModeContext);
}
