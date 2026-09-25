import { t } from "i18next";
import { isRouteErrorResponse, useRouteError } from "react-router";
import Button from "~/components/UI/Button";
import ErrorPage from "./ErrorPage";

/**
 * Loaders throw whatever the API client rejects with, so besides router error
 * responses this also digs the status out of axios errors and plain problem
 * detail bodies.
 */
export const getErrorStatus = (error: unknown): number | undefined => {
  if (isRouteErrorResponse(error)) return error.status;
  if (!error || typeof error !== "object") return undefined;

  const { response, status } = error as {
    response?: { status?: unknown };
    status?: unknown;
  };
  if (typeof response?.status === "number") return response.status;
  if (typeof status === "number") return status;
  return undefined;
};

const getErrorDetails = (error: unknown): string => {
  if (error instanceof Error) return error.stack ?? error.message;
  if (typeof error === "string") return error;
  try {
    return JSON.stringify(error, null, 2) ?? String(error);
  } catch {
    return String(error);
  }
};

/**
 * The page shown when a route fails to load or render: the 404 and 403 pages
 * for those statuses, and a "something went wrong" page with a retry button
 * for everything else. The technical details are only shown in development.
 *
 * @component
 */
export default function RouteErrorBoundary() {
  const error = useRouteError();
  const status = getErrorStatus(error);

  if (status === 404) {
    return (
      <ErrorPage
        mood="lost"
        code="404"
        headline={t("not_found_headline")}
        description={t("not_found_description")}
      />
    );
  }

  if (status === 403) {
    return (
      <ErrorPage
        mood="forbidden"
        code="403"
        headline={t("forbidden_headline")}
        description={t("forbidden_description")}
      />
    );
  }

  return (
    <ErrorPage
      mood="error"
      code={status ? String(status) : t("error_code_fallback")}
      headline={t("error_headline")}
      description={t("error_description")}
      actions={
        <Button variant="secondary" onClick={() => window.location.reload()}>
          {t("try_again")}
        </Button>
      }
    >
      {import.meta.env.DEV && (
        <details className="w-full max-w-2xl text-left">
          <summary className="cursor-pointer text-sm text-gray-600">
            {t("error_technical_details")}
          </summary>
          <pre className="mt-2 overflow-x-auto rounded-md bg-gray-100 p-4 text-xs">
            <code>{getErrorDetails(error)}</code>
          </pre>
        </details>
      )}
    </ErrorPage>
  );
}
