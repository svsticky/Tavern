import { t } from "i18next";
import ErrorPage from "~/components/ErrorPage/ErrorPage";

/**
 * Catch-all page shown when the requested route does not exist (HTTP 404).
 *
 * @page
 * @component
 */
export default function NotFoundPage() {
  return (
    <ErrorPage
      mood="lost"
      code="404"
      headline={t("not_found_headline")}
      description={t("not_found_description")}
    />
  );
}
