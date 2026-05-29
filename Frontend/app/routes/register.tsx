import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import {
  getRegisterreasons,
  getRegisterslides,
  type RegisterReasonResponseDto,
  type RegisterSlideResponseDto,
} from "~/api";
import NavBar from "~/components/Menu/NavBar/NavBar";
import PhotoSlideshow from "~/components/PhotoSlideShow";
import RegisterForm from "~/components/Register/RegisterForm/RegisterForm";
import RegisterReasons from "~/components/Register/RegisterReasons";
import { getEnv } from "~/util/config.utils";

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
  const [reasons, setReasons] = useState<RegisterReasonResponseDto[]>([]);
  const [slides, setSlides] = useState<RegisterSlideResponseDto[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    async function loadData() {
      try {
        const [reasonsRes, slidesRes] = await Promise.all([
          getRegisterreasons(),
          getRegisterslides(),
        ]);

        const reasonsData = reasonsRes.data ?? [];
        const slidesData = slidesRes.data ?? [];

        // Preload all slide and reason images so they display instantly without slow rendering
        const imagesToPreload: string[] = [];

        if (slidesData.length > 0) {
          slidesData.forEach((s) => {
            imagesToPreload.push(`${getEnv("ApiUrl")}/registerslides/${s.id}/image`);
          });
        } else {
          imagesToPreload.push(
            "/register_slide_photos/photo1.png",
            "/register_slide_photos/photo2.png",
            "/register_slide_photos/photo3.png"
          );
        }

        reasonsData.forEach((r) => {
          if (r.iconPath) {
            imagesToPreload.push(`${getEnv("ApiUrl")}/registerreasons/${r.id}/icon`);
          }
        });

        await Promise.all(
          imagesToPreload.map(
            (src) =>
              new Promise((resolve) => {
                const img = new globalThis.Image();
                img.src = src;
                img.onload = () => resolve(true);
                img.onerror = () => resolve(true);
              })
          )
        );

        setReasons(reasonsData);
        setSlides(slidesData);
      } catch (error) {
        console.error("Error loading registration content:", error);
      } finally {
        setLoading(false);
      }
    }
    loadData();
  }, []);

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

  const defaultImages = [
    "/register_slide_photos/photo1.png",
    "/register_slide_photos/photo2.png",
    "/register_slide_photos/photo3.png",
  ];

  const slideshowImages =
    slides.length > 0
      ? slides.map((s) => `${getEnv("ApiUrl")}/registerslides/${s.id}/image`)
      : defaultImages;

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
        {loading ? (
          <div className="w-full max-w-7xl aspect-[16/9] md:aspect-[21/9] bg-slate-200/50 rounded-3xl animate-pulse" />
        ) : (
          <PhotoSlideshow images={slideshowImages} className="w-full max-w-7xl" />
        )}

        <section id="reasons" className="w-full max-w-7xl">
          <RegisterReasons reasons={reasons} loading={loading} />
        </section>

        <section id="become-member" className="w-full max-w-xl">
          <RegisterForm />
        </section>
      </div>
    </>
  );
}
