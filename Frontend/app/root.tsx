import {
  isRouteErrorResponse,
  Links,
  Meta,
  Outlet,
  Scripts,
  ScrollRestoration,
  useLoaderData,
} from "react-router";
import { ReactKeycloakProvider } from "@react-keycloak/web";
import Keycloak from "keycloak-js";
import { useEffect, useState } from "react";

import "./i18n";
import i18n from "./i18n";
import { client } from "./api/client.gen";
import type { Route } from "./+types/root";
import "./app.css";

const keycloak = new Keycloak({
  url: import.meta.env.KeycloakUrl ?? "http://localhost:8085/",
  realm: import.meta.env.KeycloakRealm ?? "master",
  clientId: import.meta.env.KeycloakClientId ?? "react",
});

export const links: Route.LinksFunction = () => [
  { rel: "preconnect", href: "https://fonts.googleapis.com" },
  { rel: "preconnect", href: "https://fonts.gstatic.com", crossOrigin: "anonymous" },
  {
    rel: "stylesheet",
    href: "https://fonts.googleapis.com/css2?family=Inter:ital,opsz,wght@0,14..32,100..900;1,14..32,100..900&display=swap",
  },
];

export function Layout({ children }: { children: React.ReactNode }) {
 useEffect(() => {
    const interceptor = client.instance.interceptors.response.use(
      (response) => {
        if (response.status === 200) {
          console.log(`request to ${response.config.url} was successful`);
        }
        return response;
      },
      (error) => {
        if (error.response && error.response.status === 401) {
          console.log("Unauthorized, redirecting...");
          window.location.href = "/logout";
        }
        return Promise.reject(error);
      }
    );

    return () => client.instance.interceptors.response.eject(interceptor);
  }, []);

  return (
    <html lang="en">
      <head>
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

  useEffect(() => {
    if (i18n.isInitialized) {
      setI18nReady(true);
    } else {
      const handleInitialized = () => setI18nReady(true);
      i18n.on("initialized", handleInitialized);
      return () => i18n.off("initialized", handleInitialized);
    }
  }, []);

  if (!i18nReady) return null;

  return (
    <ReactKeycloakProvider 
      authClient={keycloak}
      initOptions={{ onLoad: 'check-sso' }}
    >
      <Outlet />
    </ReactKeycloakProvider>
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
  } else if (import.meta.env.DEV && error && error instanceof Error) {
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
