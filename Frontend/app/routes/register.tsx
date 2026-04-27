import { t } from "i18next";
import { House } from "lucide-react";
import NavBar from "~/components/Menu/NavBar/NavBar";
import RegisterForm from "~/components/Register/RegisterForm/RegisterForm";
import RegisterPhotos from "~/components/PhotoSlideShow";
import RegisterReasons from "~/components/Register/RegisterReasons";
import PhotoSlideshow from "~/components/PhotoSlideShow";

export default function Register() {
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
    }
  ];

  const images = [
    "/register_slide_photos/photo1.png",
    "/register_slide_photos/photo2.png",
    "/register_slide_photos/photo3.png",
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
        <PhotoSlideshow images={images} className="w-full max-w-7xl" />

        <section id="reasons" className="w-full max-w-7xl" >
          <RegisterReasons />
        </section>
        
        <section id="become-member" className="w-full max-w-xl">
          <RegisterForm />
        </section>
      </div>
    </>
  );
}
