import { useTranslation } from "react-i18next";
import { useLoaderData } from "react-router";
import { getRegisterreasons, getRegisterslides } from "~/api";
import NavBar from "~/components/Menu/NavBar/NavBar";
import PhotoSlideshow from "~/components/PhotoSlideShow";
import RegisterForm from "~/components/Register/RegisterForm/RegisterForm";
import { loadRegisterFormData } from "~/components/Register/RegisterForm/RegisterForm.handlers";
import RegisterReasons from "~/components/Register/RegisterReasons";
import StickyLoadingLogo from "~/components/StickyLoadingLogo";
import { getEnv } from "~/util/config.utils";

const defaultImages = [
  "/register_slide_photos/photo1.png",
  "/register_slide_photos/photo2.png",
  "/register_slide_photos/photo3.png",
];

/**
 * Loads an image into the browser cache so it displays instantly once rendered.
 * Resolves on failure too, so one broken image can't block the page.
 *
 * @param {string} src - The URL of the image.
 * @returns {Promise<void>} Resolves once the image has loaded or failed to load.
 */
const preloadImage = (src: string) =>
  new Promise<void>((resolve) => {
    const img = new globalThis.Image();
    img.onload = () => resolve();
    img.onerror = () => resolve();
    img.src = src;
  });

/**
 * Loads the slides, reasons and registration form data. The page is public, so
 * this doesn't require a login. Failing slides or reasons fall back to the
 * default photos and an empty reasons list instead of failing the page.
 */
export async function clientLoader() {
  const [reasonsRes, slidesRes, formData] = await Promise.all([
    getRegisterreasons().catch((error) => {
      console.error("Error loading register reasons:", error);
      return null;
    }),
    getRegisterslides().catch((error) => {
      console.error("Error loading register slides:", error);
      return null;
    }),
    loadRegisterFormData(),
  ]);

  const reasons = reasonsRes?.data ?? [];
  const slides = slidesRes?.data ?? [];

  const slideshowImages =
    slides.length > 0
      ? slides.map((s) => `${getEnv("ApiUrl")}/registerslides/${s.id}/image`)
      : defaultImages;
  const reasonIcons = reasons
    .filter((r) => r.iconPath)
    .map((r) => `${getEnv("ApiUrl")}/registerreasons/${r.id}/icon`);

  await Promise.all([...slideshowImages, ...reasonIcons].map(preloadImage));

  return { reasons, slideshowImages, formData };
}

/**
 * Shown while the route's `clientLoader` runs on the first page load.
 *
 * @component
 */
export function HydrateFallback() {
  return <StickyLoadingLogo />;
}

/**
 * The public-facing membership registration landing page.
 *
 * This page is designed as a high-conversion "One-Pager" for prospective members.
 * It follows a logical flow to encourage sign-ups:
 * 1. **Visual Hook**: A `PhotoSlideshow` displaying the association's atmosphere.
 * 2. **Value Proposition**: The `RegisterReasons` section (anchored by #reasons)
 *    explaining the benefits of joining.
 * 3. **Call to Action**: The `RegisterForm` (anchored by #become-member) for
 *    collecting registration data.
 *
 * Features:
 * - **Smooth Navigation**: The `NavBar` uses hash-links (#) to allow users to
 *   jump between sections on the same page.
 * - **Responsive Constraints**: Uses `max-w-7xl` for informational content and
 *   a tighter `max-w-xl` for the form to ensure readability and focus.
 * - **Branding**: Implements a subtle themed background using `bg-(--board-primary)/5`.
 *
 * @page
 * @component
 */
export default function Register() {
  const { t } = useTranslation();
  const { reasons, slideshowImages, formData } =
    useLoaderData<typeof clientLoader>();

  const navBarItems = [
    {
      id: "home",
      label: t("home"),
      href: "/register#home",
    },
    {
      id: "why",
      label: t("who_are_we"),
      href: "/register#reasons",
    },
    {
      id: "become_member",
      label: t("become_member"),
      href: "/register#become-member",
    },
  ];

  return (
    <>
      <section id="home">
        <NavBar className="px-[5%] sm:px-[10%]" maxWidthBeforeCompact={900}>
          <NavBar.Branding title="" homepage="/register" />
          {navBarItems.map((item) => (
            <NavBar.Item key={item.id} item={item} />
          ))}
        </NavBar>
      </section>

      <div className="flex flex-col min-h-screen w-full gap-6 items-center justify-center p-4 bg-(--board-primary)/5">
        <PhotoSlideshow images={slideshowImages} className="w-full max-w-7xl" />

        <section id="reasons" className="w-full max-w-7xl">
          <RegisterReasons reasons={reasons} />
        </section>

        <section id="become-member" className="w-full max-w-xl">
          <RegisterForm data={formData} />
        </section>
      </div>
    </>
  );
}
