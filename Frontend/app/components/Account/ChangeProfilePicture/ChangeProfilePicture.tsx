import { t } from "i18next";
import { useRef, useState } from "react";
import Button from "~/components/UI/Button";
import {
  handleProfilePictureDelete,
  handleProfilePictureUpload,
} from "./ChangeProfilePicture.handlers";

/**
 * Renders a component for changing a user's profile picture.
 * @param {Object} props - The component props.
 * @param {string} props.userId - The ID of the user whose profile picture is being changed.
 * @param {string | null} props.profilePictureSrc - The current picture's URL, loaded by the route (see `loadProfilePictureSrc`).
 * @param {React.ReactNode} [props.children] - Optional children to render below the profile picture.
 * @returns {JSX.Element} - The rendered component.
 */
export default function ChangeProfilePicture({
  userId,
  profilePictureSrc,
  children,
}: {
  userId: string;
  profilePictureSrc: string | null;
  children?: React.ReactNode;
}) {
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [profilePictureSrcState, setProfilePictureSrc] = useState<string | null>(profilePictureSrc);

  return (
    <div className="flex flex-col items-center lg:w-48">
      <div
        className="relative w-40 h-40 group cursor-pointer"
        onClick={() => fileInputRef.current?.click()}
      >
        <div className="w-full h-full rounded-full overflow-hidden flex items-center justify-center bg-(--board-primary) shadow-md border-4 border-white transition-transform group-hover:scale-105">
          <img
            src={profilePictureSrcState || "/profile-picture.svg"}
            className={
              profilePictureSrcState && profilePictureSrcState !== "/profile-picture.svg"
                ? "w-full h-full object-cover"
                : "w-2/3 h-2/3 opacity-80"
            }
            alt="Profile"
          />
        </div>
        <div className="absolute inset-0 flex items-center justify-center bg-black/40 text-white rounded-full opacity-0 group-hover:opacity-100 transition-opacity text-xs font-bold uppercase">
          {t("change")}
        </div>
      </div>
      <input
        type="file"
        ref={fileInputRef}
        hidden
        accept="image/*"
        onChange={() =>
          fileInputRef.current?.files &&
          handleProfilePictureUpload(
            {
              target: fileInputRef.current,
            } as React.ChangeEvent<HTMLInputElement>,
            userId,
          )
        }
      />

      {profilePictureSrcState && profilePictureSrcState !== "/profile-picture.svg" && (
        <Button
          variant="secondary"
          className="mt-3 text-xs py-1.5 px-3 border border-red-200 text-red-600 hover:bg-red-50 hover:text-red-700"
          onClick={() => handleProfilePictureDelete(userId)}
          type="button"
        >
          {t("remove")}
        </Button>
      )}

      <div className="mt-6 text-center">{children}</div>
    </div>
  );
}
