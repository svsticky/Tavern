import { t } from "i18next";
import { useEffect } from "react";
import NavBar from "~/components/Menu/NavBar/NavBar";

export default function ConfirmMail() {
  
  useEffect(() => {
  const redirectAction = async () => {
      await new Promise((resolve) => setTimeout(resolve, 1000));

      const baseUrl = `${import.meta.env.KeycloakUrl}/realms/${import.meta.env.KeycloakRealm}/login-actions/reset-credentials`;
      
      const clientId = `${import.meta.env.KeycloakClientId}`;
      const redirectUri = encodeURIComponent(window.location.origin + "/"); 

      window.location.href = `${baseUrl}?client_id=${clientId}&tab_id=...&redirect_uri=${redirectUri}`;
    };
    redirectAction();
  }, []);

  return (
    <>
        <section id="home">
            <NavBar className="px-[5%] sm:px-[10%]" maxWidthBeforeCompact={900}>
            <NavBar.Branding title="" homepage="/register" />
            </NavBar>
        </section>

        <div className="p-4">
            <p className="text-lg">{t("loading")}...</p>
        </div>
    </>
  );
}