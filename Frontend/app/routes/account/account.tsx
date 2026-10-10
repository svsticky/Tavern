import { t } from "i18next";
import { useEffect, useState } from "react";
import ChangeAccountForm from "~/components/Account/ChangeProfileForm/ChangeAccountForm";
import ChangeProfilePicture from "~/components/Account/ChangeProfilePicture/ChangeProfilePicture";
import { PageHeader } from "~/components/UI/PageHeader";
import { useApp } from "~/context/AppContext";
import { useAuth } from "~/context/AuthContext";

/**
 * The primary profile management page for the authenticated user.
 *
 * This page provides a centralized interface for users to:
 * - **View Identity**: Displays the user's full name and student number.
 * - **Manage Media**: Allows updating the profile picture via the `ChangeProfilePicture` component.
 * - **Edit Information**: Provides a form to modify account details (email, phone, etc.)
 *   via the `ChangeAccountForm` component.
 *
 * The component relies on the global `useApp` context for current member data
 * and the `useAuth` context for identifying the logged-in user's ID.
 *
 * @page
 * @component
 */
export default function AccountPage() {
  const authService = useAuth();
  const [userId, setUserId] = useState<string | null>(null);

  useEffect(() => {
    const loadUserId = async () => {
      const tokenParsed = await authService.getTokenParsed();
      setUserId(tokenParsed?.UserId || null);
      if (!tokenParsed?.UserId) {
        console.error("User not authenticated");
        return;
      }
    };
    loadUserId();
  }, [authService]);

  const { member } = useApp();

  if (!userId) return null;

  return (
    <>
      <PageHeader title={t("account")} />

      <div className="flex flex-col lg:flex-row gap-12">
        {/* Left: Profile Picture */}
        <ChangeProfilePicture
          userId={userId}
          isHonoraryOrMerit={Boolean(
            member?.ereLid || member?.lidVanVerdienste,
          )}
        >
          <div className="flex flex-col items-center gap-1 mt-2">
            <div className="flex items-center gap-1.5 flex-wrap justify-center">
              <h2 className="font-bold text-l">
                {member?.firstName} {member?.lastName}
              </h2>
              {member?.ereLid && (
                <span className="shrink-0 px-2 py-0.5 text-xs font-bold rounded-full bg-amber-100 text-amber-800 border border-amber-400">
                  {t("ere_lid")}
                </span>
              )}
              {!member?.ereLid && member?.lidVanVerdienste && (
                <span className="shrink-0 px-2 py-0.5 text-xs font-bold rounded-full bg-amber-100 text-amber-800 border border-amber-400">
                  {t("lid_van_verdienste")}
                </span>
              )}
            </div>
            <p className="text-gray-500 font-mono text-sm">
              {member?.studentNumber}
            </p>
          </div>
        </ChangeProfilePicture>

        {/* Right: Forms */}
        {member && <ChangeAccountForm member={member} />}
      </div>
    </>
  );
}
