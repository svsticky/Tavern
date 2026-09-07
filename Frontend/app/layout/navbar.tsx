import {
  Bell,
  CalendarDays,
  LayoutDashboard,
  SquareArrowOutUpRight,
  User,
} from "lucide-react";
import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { Outlet } from "react-router";
import { getMembersByIdProfilePicture } from "~/api";
import NavBar from "~/components/Menu/NavBar/NavBar";
import { useAdminMode } from "~/context/AdminModeContext";
import { useApp } from "~/context/AppContext";
import { useAuth } from "~/context/AuthContext";
import type { TokenParsed } from "~/types/TokenParsed";
import { hasPermission, isBoardOrCandidateBoard } from "~/util/group.util";

/**
 * A primary layout component that provides the main navigation structure for the application.
 *
 * This layout includes:
 * - **Dynamic Navigation**: Displays a set of standard links (Dashboard, Activities, etc.).
 * - **Identity Management**: Fetches and displays the user's profile picture and name.
 * - **Role-Based Access**: Conditionally adds administrative links to the profile dropdown if the user is a board member.
 * - **Blob Management**: Safely handles profile picture retrieval via API as a Blob and manages Object URL cleanup.
 * - **Responsive Constraints**: Configures the `NavBar` to adapt its layout based on the length of the user's name.
 *
 * @component
 */
export default function NavBarLayout() {
  const { t } = useTranslation();

  const authService = useAuth();
  const [tokenParsed, setTokenParsed] = useState<TokenParsed | null>(null);

  useEffect(() => {
    let cancelled = false;
    const loadToken = async () => {
      const token = await authService.getTokenParsed();
      if (!cancelled) {
        setTokenParsed(token);
        if (!token) {
          console.error("User not authenticated");
        }
      }
    };
    loadToken();
    return () => {
      cancelled = true;
    };
  }, [authService]);

  const { isAdminUser, setIsAdminUser, adminMode, toggleAdminMode } =
    useAdminMode();

  useEffect(() => {
    setIsAdminUser(Boolean(tokenParsed?.is_admin));
  }, [tokenParsed, setIsAdminUser]);

  const isBoard = isBoardOrCandidateBoard(tokenParsed);
  const canSeeActivitiesAdmin =
    isBoard ||
    hasPermission(tokenParsed, "EditAllActivities") ||
    hasPermission(tokenParsed, "EditActivityForGroup");
  const canSeeMembersAdmin =
    isBoard ||
    hasPermission(tokenParsed, "ViewMembers") ||
    hasPermission(tokenParsed, "ManageMembers");
  const canSeeGroupsAdmin =
    isBoard || hasPermission(tokenParsed, "ManageGroups");
  const canSeeRolesAdmin =
    isBoard ||
    hasPermission(tokenParsed, "ManageRoles") ||
    hasPermission(tokenParsed, "ManageRolePermissions");
  const canSeeFinancesAdmin =
    isBoard ||
    hasPermission(tokenParsed, "ViewFinances") ||
    hasPermission(tokenParsed, "ManageFinances");

  const [imgSrc, setImgSrc] = useState<string>("/profile-picture.svg");

  useEffect(() => {
    let url: string | null = null;
    let cancelled = false;

    async function loadData() {
      if (!authService.isAuthenticated() || !tokenParsed) return;

      try {
        const profilePictureResponse = await getMembersByIdProfilePicture({
          path: {
            id: tokenParsed.UserId,
          },
          responseType: "blob",
        });

        if (cancelled) return;

        if (
          profilePictureResponse.data instanceof Blob &&
          profilePictureResponse.status === 200
        ) {
          url = URL.createObjectURL(profilePictureResponse.data);
          setImgSrc(url);
        }

        if (profilePictureResponse.status === 404) {
          setImgSrc("/profile-picture.svg");
        }
      } catch (error) {
        console.error("Error while loading profile picture:", error);
      }
    }

    loadData();

    return () => {
      cancelled = true;
      if (url) URL.revokeObjectURL(url);
    };
  }, [authService, tokenParsed]);

  const profileOptions = {
    username: tokenParsed?.name || "",
    avatarUrl: imgSrc,
    options: [
      { label: t("account"), href: "/account" },
      ...(canSeeActivitiesAdmin
        ? [
            {
              label: `${t("all_activities")}`,
              href: "/admin/activities",
            },
          ]
        : []),
      ...(canSeeMembersAdmin
        ? [
            {
              label: `${t("members")}`,
              href: "/admin/members",
            },
          ]
        : []),
      ...(canSeeGroupsAdmin
        ? [{ label: `${t("groups")}`, href: "/admin/groups" }]
        : []),
      ...(canSeeRolesAdmin
        ? [{ label: `${t("roles")}`, href: "/admin/roles" }]
        : []),
      ...(canSeeFinancesAdmin
        ? [
            {
              label: `${t("finances")}`,
              href: "/admin/finances",
            },
          ]
        : []),
      ...(isBoard
        ? [
            {
              label: `${t("koala_settings")}`,
              href: "/admin/settings",
            },
          ]
        : []),
      { label: t("logout"), href: "/logout" },
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

  const { member } = useApp();
  const isHonoraryOrMerit = Boolean(member?.ereLid || member?.lidVanVerdienste);

  return (
    <div className="min-w-[320px]">
      <NavBar
        className="px-[5%] sm:px-[10%]"
        maxWidthBeforeCompact={900 + profileOptions.username.length * 17}
      >
        <NavBar.Branding title="" />
        {navBarItems.map((item) => (
          <NavBar.Item key={item.id} item={item} />
        ))}
        <NavBar.ProfileDropdown
          username={profileOptions.username}
          avatarUrl={profileOptions.avatarUrl}
          options={profileOptions.options}
          isHonoraryOrMerit={isHonoraryOrMerit}
          userId={member?.id}
        />
      </NavBar>
      {isAdminUser && !adminMode && (
        <aside
          aria-label={t("member_mode_active")}
          className="bg-amber-500 text-white px-[5%] sm:px-[10%] py-2 text-xs sm:text-sm font-medium flex items-center justify-between shadow-xs"
        >
          <div className="flex items-center gap-2">
            <User size={16} className="shrink-0" />
            <span>{t("member_view_banner_text")}</span>
          </div>
          <button
            type="button"
            onClick={toggleAdminMode}
            className="bg-white text-amber-900 px-3 py-1 rounded-md font-semibold text-xs hover:bg-amber-50 transition-colors cursor-pointer shrink-0 ml-3"
          >
            {t("switch_to_admin_mode")}
          </button>
        </aside>
      )}
      <main className="px-[5%] sm:px-[10%] py-5">
        <Outlet key={adminMode ? "admin" : "member"} />
      </main>
    </div>
  );
}
