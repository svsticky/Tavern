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
import FaviconHandler from "./components/FavIconHandler";
import { AppProvider } from "./context/AppContext";
import { getEnv } from "./util/config.utils";
import { t } from "i18next";

client.setConfig({
  baseURL: getEnv("ApiUrl") ?? "http://localhost:8080",
});

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
  const [isClient, setIsClient] = useState(false);
  const location = useLocation();

  useEffect(() => {
    const primaryLight = getEnv("BOARD_PRIMARY_LIGHT");
    const primary = getEnv("BOARD_PRIMARY");
    const primaryDark = getEnv("BOARD_PRIMARY_DARK");

    if (primaryLight)
      document.documentElement.style.setProperty(
        "--board-primary-light",
        primaryLight,
      );
    if (primary)
      document.documentElement.style.setProperty("--board-primary", primary);
    if (primaryDark)
      document.documentElement.style.setProperty(
        "--board-primary-dark",
        primaryDark,
      );

    setIsClient(true);
    if (i18n.isInitialized) {
      setI18nReady(true);
    } else {
      const handleInitialized = () => setI18nReady(true);
      i18n.on("initialized", handleInitialized);
      return () => i18n.off("initialized", handleInitialized);
    }
  }, []);

  useEffect(() => {
    const formatTitle = (path: string) => {
      const pathParts = path.replace(/^\/+|\/+$/g, '').split('/');
      const lastPart = pathParts[pathParts.length - 1] || 'dashboard';
      
      const translationKey = lastPart.replace(/-/g, '_');
      
      const translated = t(translationKey);

      const capitalizedTitle = translated.charAt(0).toUpperCase() + translated.slice(1);
      
      return `Koala | ${capitalizedTitle}`;
    };

    document.title = formatTitle(location.pathname);
  }, [location.pathname, i18n.language]);

  if (!i18nReady) return null;

  return (
    <AppProvider>
      <FaviconHandler />
      {isClient && <Toaster position="bottom-right" />}
      <Outlet />
    </AppProvider>
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
