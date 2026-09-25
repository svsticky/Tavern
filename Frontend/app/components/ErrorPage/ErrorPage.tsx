import { t } from "i18next";
import type { ReactNode } from "react";
import Button from "~/components/UI/Button";
import KoalaIllustration, { type KoalaMood } from "./KoalaIllustration";

/**
 * A playful full-width error page with a koala, a big status code, a headline
 * and a way back home. Shared by the 403, 404 and generic error pages.
 *
 * @component
 * @param {Object} props - The component properties.
 * @param {string} props.code - The status code to display, e.g. "404".
 * @param {string} props.headline - The short, fun headline.
 * @param {string} props.description - A friendly explanation of what happened.
 * @param {KoalaMood} props.mood - Which koala illustration to show.
 * @param {ReactNode} [props.actions] - Extra buttons shown before the way back home.
 * @param {ReactNode} [props.children] - Extra content shown below the buttons.
 */
const ErrorPage = ({
  code,
  headline,
  description,
  mood,
  actions,
  children,
}: {
  code: string;
  headline: string;
  description: string;
  mood: KoalaMood;
  actions?: ReactNode;
  children?: ReactNode;
}) => {
  return (
    <div className="flex flex-col items-center justify-center text-center py-8 sm:py-16 gap-4">
      <KoalaIllustration mood={mood} className="w-48 sm:w-64 h-auto" />
      <p className="text-7xl sm:text-8xl font-extrabold leading-none text-(--board-primary)">
        {code}
      </p>
      <h1 className="text-2xl font-bold">{headline}</h1>
      <p className="max-w-md text-gray-600">{description}</p>
      <div className="flex flex-wrap items-center justify-center gap-3">
        {actions}
        <Button href="/">{t("back_to_home")}</Button>
      </div>
      {children}
    </div>
  );
};

export default ErrorPage;
