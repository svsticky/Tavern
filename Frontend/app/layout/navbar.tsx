import { useKeycloak } from "@react-keycloak/web";
import {
  Bell,
  CalendarDays,
  LayoutDashboard,
  SquareArrowOutUpRight,
} from "lucide-react";
import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { Outlet, useNavigate } from "react-router";
import { getApiMembersByIdProfilePicture } from "~/api";
import NavBar from "~/components/Menu/NavBar/NavBar";
import { isBoardOrCandidateBoard } from "~/util/group.util";

export default function NavBarLayout() {
  const { t } = useTranslation();
  const navigate = useNavigate();

  const { keycloak, initialized } = useKeycloak();

  const [imgSrc, setImgSrc] = useState<string>("/profile-picture.svg");

  useEffect(() => {
      let url: string | null = null;
      async function loadData() {
        if (!initialized || !keycloak.authenticated) return;
  
        try {
          var profilePictureResponse = await getApiMembersByIdProfilePicture({
            path: {
              id: keycloak.tokenParsed?.UserId ?? 0
            },
            responseType: 'blob'
          })
        
          if (profilePictureResponse.data instanceof Blob && profilePictureResponse.status === 200) {
            url = URL.createObjectURL(profilePictureResponse.data);
            setImgSrc(url);
          }

          if(profilePictureResponse.status === 404) {
            setImgSrc("/profile-picture.svg");
          }
        } catch (error) {
          console.error("Error while loading profile picture:", error);
        }
      }
  
      loadData();

      return () => {
        if (url) URL.revokeObjectURL(url);
      };  
    }, [initialized, keycloak.authenticated]);

  const isBoard = isBoardOrCandidateBoard(keycloak.tokenParsed);

  const profileOptions = {
    username: keycloak.tokenParsed?.name || "",
    avatarUrl: imgSrc,
    options: [
      { label: t("account"), action: () => navigate("/account") },
      ...(isBoard ? [{ label: `${t("activity")} ${t("management")}`, action: () => navigate("/admin/activities") }] : []),
      ...(isBoard ? [{ label: `${t("member")} ${t("management")}`, action: () => navigate("/admin/members") }] : []),
      ...(isBoard ? [{ label: `${t("group")} ${t("management")}`, action: () => navigate("/admin/groups") }] : []),
      ...(isBoard ? [{ label: `${t("finances")}`, action: () => navigate("/admin/finances") }] : []),
      { label: t("logout"), action: () => navigate("/logout") },
    ],
  };

  const navBarItems = [
    {
      id: "dashboard",
      label: t("dashboard"),
      href: "/",
      icon: LayoutDashboard,
    },
    {
      id: "activities",
      label: t("activities"),
      href: "/activities",
      icon: CalendarDays,
    },
    {
      id: "announcements",
      label: t("announcements"),
      href: "/announcements",
      icon: Bell,
    },
    {
      id: "external-links",
      label: t("external_links"),
      href: "/external-links",
      icon: SquareArrowOutUpRight,
    },
  ];

  return (
    <div className="min-w-[320px]">
      <NavBar className="px-[5%] sm:px-[10%]" maxWidthBeforeCompact={900 + profileOptions.username.length * 17}>
        <NavBar.Branding title="" />
        {navBarItems.map((item) => (
          <NavBar.Item key={item.id} item={item} />
        ))}
        <NavBar.ProfileDropdown
          username={profileOptions.username}
          avatarUrl={profileOptions.avatarUrl}
          options={profileOptions.options}
        />
      </NavBar>
      <main className="px-[5%] sm:px-[10%] py-5">
        <Outlet />
      </main>
    </div>
  );
}
