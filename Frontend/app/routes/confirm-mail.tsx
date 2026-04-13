import { t } from "i18next";
import NavBar from "~/components/Menu/NavBar/NavBar";
import { PageHeader } from "~/components/UI/PageHeader";

export default function ConfirmMail() {
  return (
    <>
        <section id="home">
            <NavBar className="px-[5%] sm:px-[10%]" maxWidthBeforeCompact={900}>
            <NavBar.Branding title="" homepage="/register" />
            </NavBar>
        </section>

        <div className="p-4">
            <PageHeader title={t("confirm_mail")} />
            <p className="text-lg">{t("confirm_mail_description")}</p>
        </div>
    </>
  );
}