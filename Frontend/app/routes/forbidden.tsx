import { t } from "i18next";
import ErrorPage from "~/components/ErrorPage/ErrorPage";

/**
 * Page shown when the user tries to access a resource they have no permission for
 * (HTTP 403).
 *
 * @page
 * @component
 */
export default function ForbiddenPage() {
  return (
    <ErrorPage
      mood="forbidden"
      code="403"
      headline={t("forbidden_headline")}
      description={t("forbidden_description")}
    />
  );
}
