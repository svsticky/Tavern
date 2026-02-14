import { NavLink } from "react-router";
import images from "~/constants/images";

type MenuBrandingProps = {
  icon?: string;
  title?: string;
};

export default function MenuBranding({
  icon = images.sticky_logo_head_white,
  title = "Sticky",
}: MenuBrandingProps) {
  return (
    <NavLink
      to="/"
      className="flex items-center gap-x-3 text-xl font-semibold text-white no-underline"
    >
      <img src={icon} alt="Logo" className="h-10 w-auto" />
      <p className="text-2xl font-bold my-0">{title}</p>
    </NavLink>
  );
}
