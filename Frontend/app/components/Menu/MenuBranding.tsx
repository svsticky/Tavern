import { NavLink } from "react-router";

type MenuBrandingProps = {
  icon?: string;
  title?: string;
  homepage?: string;
};

export default function MenuBranding({
  icon = import.meta.env.LOGO_URL,
  title = "Sticky",
  homepage = "/"
}: MenuBrandingProps) {
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
