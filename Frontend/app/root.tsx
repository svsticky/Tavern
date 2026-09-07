import type { AxiosError, AxiosResponse } from "axios";
import { useEffect, useState } from "react";
import { Toaster } from "react-hot-toast";
import {
  isRouteErrorResponse,
  Links,
  Meta,
  Outlet,
  Scripts,
  ScrollRestoration,
  useLocation,
} from "react-router";
import "./i18n";
import type { Route } from "./+types/root";
import { client } from "./api/client.gen";
import i18n from "./i18n";
import "./app.css";
import { t } from "i18next";
import Cookies from "js-cookie";
import FaviconHandler from "./components/FavIconHandler";
import { AppProvider } from "./context/AppContext";
import { getActiveAuthService } from "./layout/auth-service";
import { getEnv } from "./util/config.utils";
import {
  BOARD_THEME_SETTINGS_UPDATED_EVENT,
  loadBoardThemeSettings,
} from "./util/theme-settings";

client.setConfig({
  baseURL: getEnv("ApiUrl") ?? "http://localhost:8080",
  auth: async () => {
    const authService = getActiveAuthService();
    if (authService?.isReady() && authService.isAuthenticated()) {
      const token = await authService.getToken();
      if (token) {
        Cookies.set("access_token", token, {
          path: "/",
          secure: true,
          sameSite: "lax",
          domain: `.${window.location.hostname}`,
        });
        return token;
      }
    }
    return undefined;
  },
});

client.instance.interceptors.response.use(
  async (response: AxiosResponse) => response,
  (error: AxiosError) => {
    if (error.response) {
      if (error.response.status === 401) {
        console.warn("Unauthorized, redirecting...");
        window.location.href = "/logout";
      } else if (error.response.status === 403) {
        console.warn("Forbidden - user does not have access to this resource.");
        if (window.location.pathname !== "/") {
          window.location.href = "/";
        }
      }
    }
    return Promise.reject(error);
  },
);

export const links: Route.LinksFunction = () => [
  { rel: "preconnect", href: "https://fonts.googleapis.com" },
  {
    rel: "preconnect",
    href: "https://fonts.gstatic.com",
    crossOrigin: "anonymous",
  },
  {
    rel: "stylesheet",
    href: "https://fonts.googleapis.com/css2?family=Inter:ital,opsz,wght@0,14..32,100..900;1,14..32,100..900&display=swap",
  },
];

import { ThemeProvider } from "./context/ThemeContext";

const getDocumentLanguage = () =>
  (i18n.resolvedLanguage || i18n.language || "en").split("-")[0];

export function Layout({ children }: { children: React.ReactNode }) {
  const currentLang = getDocumentLanguage();
  return (
    <html lang={currentLang}>
      <head>
        <script src="/env-config.js" />
        <meta charSet="utf-8" />
        <meta name="viewport" content="width=device-width, initial-scale=1" />
        <Meta />
        <Links />
      </head>
      <body>
        {children}
        <ScrollRestoration />
        <Scripts />
      </body>
    </html>
  );
}

export default function App() {
  const [i18nReady, setI18nReady] = useState(false);
  const [themeReady, setThemeReady] = useState(false);
  const [themeError, setThemeError] = useState<string | null>(null);
  const [isClient, setIsClient] = useState(false);
  const location = useLocation();

  useEffect(() => {
    const syncBoardTheme = async () => {
      const loadedSettingCount = await loadBoardThemeSettings();

      if (loadedSettingCount === 0) {
        setThemeError(
          "Could not load the board colors. Please check that the backend is running.",
        );
      } else {
        setThemeError(null);
      }

      setThemeReady(true);
    };

    const handleThemeRefresh = () => {
      void syncBoardTheme();
    };

    void syncBoardTheme();
    window.addEventListener(
      BOARD_THEME_SETTINGS_UPDATED_EVENT,
      handleThemeRefresh,
    );

    setIsClient(true);
    const checkI18n = () => {
      if (i18n.isInitialized && i18n.hasLoadedNamespace("translation")) {
        setI18nReady(true);
        return true;
      }
      return false;
    };

    if (!checkI18n()) {
      const handleEvent = () => {
        checkI18n();
      };
      i18n.on("initialized", handleEvent);
      i18n.on("loaded", handleEvent);
      return () => {
        i18n.off("initialized", handleEvent);
        i18n.off("loaded", handleEvent);
        window.removeEventListener(
          BOARD_THEME_SETTINGS_UPDATED_EVENT,
          handleThemeRefresh,
        );
      };
    }
    return () => {
      window.removeEventListener(
        BOARD_THEME_SETTINGS_UPDATED_EVENT,
        handleThemeRefresh,
      );
    };
  }, []);

  // biome-ignore lint/correctness/useExhaustiveDependencies: i18n.language is needed as dependency to update the title on language change.
  useEffect(() => {
    const formatTitle = (path: string) => {
      const pathParts = path.replace(/^\/+|\/+$/g, "").split("/");

      let lastPart = pathParts[pathParts.length - 1];

      const isGuid =
        /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/.test(
          lastPart,
        );
      const isNumber = /^\d+$/.test(lastPart);

      if ((isNumber || isGuid) && pathParts.length > 1) {
        lastPart = pathParts[pathParts.length - 2];
      }

      lastPart = lastPart || "dashboard";

      const translationKey = lastPart.replace(/-/g, "_");
      const translated = t(translationKey);
      const capitalizedTitle =
        translated.charAt(0).toUpperCase() + translated.slice(1);

      return `Koala | ${capitalizedTitle}`;
    };

    document.title = formatTitle(location.pathname);
  }, [location.pathname, i18n.language]);

  if (themeError) {
    return (
      <main className="min-h-screen flex items-center justify-center p-4">
        <div className="max-w-xl rounded-md border border-red-200 bg-red-50 p-4 text-red-800">
          <h1 className="font-semibold">Error</h1>
          <p>{themeError}</p>
        </div>
      </main>
    );
  }

  if (!i18nReady || !themeReady) {
    return t("loading");
  }

  return (
    <ThemeProvider>
      <AppProvider>
        <FaviconHandler />
        {isClient && <Toaster position="bottom-right" />}
        <Outlet />
      </AppProvider>
    </ThemeProvider>
  );
}

export function ErrorBoundary({ error }: Route.ErrorBoundaryProps) {
  let message = "Oops!";
  let details = "An unexpected error occurred.";
  let stack: string | undefined;

  if (isRouteErrorResponse(error)) {
    message = error.status === 404 ? "404" : "Error";
    details =
      error.status === 404
        ? "The requested page could not be found."
        : error.statusText || details;
  } else if (error && error instanceof Error) {
    details = error.message;
    stack = error.stack;
  }

  return (
    <main className="pt-16 p-4 container mx-auto">
      <h1>{message}</h1>
      <p>{details}</p>
      {stack && (
        <pre className="w-full p-4 overflow-x-auto">
          <code>{stack}</code>
        </pre>
      )}
    </main>
  );
}
