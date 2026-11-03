import { t } from "i18next";
import { useLoaderData } from "react-router";
import ChangeAccountForm from "~/components/Account/ChangeProfileForm/ChangeAccountForm";
import { loadMemberMailinglists } from "~/components/Account/ChangeProfileForm/ChangeAccountForm.handlers";
import ChangeProfilePicture from "~/components/Account/ChangeProfilePicture/ChangeProfilePicture";
import { loadProfilePictureSrc } from "~/components/Account/ChangeProfilePicture/ChangeProfilePicture.handlers";
import StickyLoadingLogo from "~/components/StickyLoadingLogo";
import { PageHeader } from "~/components/UI/PageHeader";
import { useApp } from "~/context/AppContext";
import { requireTokenParsed } from "~/util/loaderAuth.util";

/**
 * Loads the logged-in user's ID, profile picture and mailing lists.
 * Redirects to the login page when the user isn't logged in.
 */
export async function clientLoader() {
  const tokenParsed = await requireTokenParsed();
  const [profilePictureSrc, mailingListsData] = await Promise.all([
    loadProfilePictureSrc(tokenParsed.UserId),
    loadMemberMailinglists(tokenParsed.UserId),
  ]);
  return { userId: tokenParsed.UserId, profilePictureSrc, ...mailingListsData };
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
 * The primary profile management page for the authenticated user.
 *
 * This page provides a centralized interface for users to:
 * - **View Identity**: Displays the user's full name and student number.
 * - **Manage Media**: Allows updating the profile picture via the `ChangeProfilePicture` component.
 * - **Edit Information**: Provides a form to modify account details (email, phone, etc.)
 *   via the `ChangeAccountForm` component.
 *
 * The component relies on the global `useApp` context for current member data;
 * its `clientLoader` provides the logged-in user's ID, profile picture and mailing lists.
 *
 * @page
 * @component
 */
export default function AccountPage() {
  const { member } = useApp();
  const { userId, profilePictureSrc, mailingLists, mailingListsUnavailable } =
    useLoaderData<typeof clientLoader>();
  if (!member?.id) return null;

  return (
    <>
      <PageHeader title={t("account")} />

      <div className="flex flex-col lg:flex-row gap-12">
        {/* Left: Profile Picture */}
        <ChangeProfilePicture
          userId={userId}
          profilePictureSrc={profilePictureSrc}
        >
          <h2 className="font-bold text-l">
            {member?.firstName} {member?.lastName}
          </h2>
          <p className="text-gray-500 font-mono text-sm">
            {member?.studentNumber}
          </p>
        </ChangeProfilePicture>

        {/* Right: Forms */}
        {member && (
          <ChangeAccountForm
            member={member}
            mailingLists={mailingLists}
            mailingListsUnavailable={mailingListsUnavailable}
          />
        )}
      </div>
    </>
  );
}
