import { NavLink } from "react-router";
import { getEnv } from "~/util/config.utils";

/**
 * A branding subcomponent typically used in NavBars or SideBars.
 * Displays a clickable logo and title that links to the application's homepage.
 *
 * @component
 * @param {Object} props - Component props.
 * @param {string} [props.icon] - Source URL for the brand logo.
 * @param {string} [props.title] - Text for the brand name.
 * @param {string} [props.homepage] - Route path for the branding link.
 */
export default function MenuBranding({
  icon = getEnv("LOGO_URL"),
  title = "Sticky",
  homepage = "/",
}: {
  icon?: string;
  title?: string;
  homepage?: string;
}) {
  return (
    <NavLink
      to={homepage}
      className="flex items-center gap-x-3 text-xl font-semibold text-white no-underline"
    >
      <img src={icon} alt="Logo" className="h-10 w-auto" />
      <p className="text-2xl font-bold my-0">{title}</p>
    </NavLink>
  );
}
