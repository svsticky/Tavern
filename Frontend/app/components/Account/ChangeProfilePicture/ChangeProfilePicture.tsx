import { t } from "i18next";
import { useEffect, useRef, useState } from "react";
import { getMembersByIdProfilePicture } from "~/api";
import { cn } from "~/util/tailwind.util";
import { handleProfilePictureUpload } from "./ChangeProfilePicture.handlers";

/**
 * Renders a component for changing a user's profile picture.
 * @param {Object} props - The component props.
 * @param {string} props.userId - The ID of the user whose profile picture is being changed.
 * @param {React.ReactNode} [props.children] - Optional children to render below the profile picture.
 * @param {boolean} [props.isHonoraryOrMerit] - Whether to show a gold border for honorary members / members of merit.
 * @returns {JSX.Element} - The rendered component.
 */
export default function ChangeProfilePicture({
  userId,
  children,
  isHonoraryOrMerit = false,
}: {
  userId: string;
  children?: React.ReactNode;
  isHonoraryOrMerit?: boolean;
}) {
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [profilePictureSrc, setProfilePictureSrc] = useState<string | null>(
    null,
  );

  useEffect(() => {
    let url = null as string | null;
    const loadProfilePicture = async () => {
      try {
        const ppRes = await getMembersByIdProfilePicture({
          path: { id: userId },
          responseType: "blob",
        });
        if (
          !ppRes.error &&
          ppRes.data instanceof Blob &&
          ppRes.status === 200
        ) {
          url = URL.createObjectURL(ppRes.data);
          setProfilePictureSrc(url);
        } else {
          setProfilePictureSrc("/profile-picture.svg");
        }
      } catch (err) {
        console.error("Failed to load profile picture:", err);
        setProfilePictureSrc("/profile-picture.svg");
      }
    };

    loadProfilePicture();

    return () => {
      if (url) URL.revokeObjectURL(url);
    };
  }, [userId]);

  return (
    <div className="flex flex-col items-center lg:w-48">
      <div
        className="relative w-40 h-40 group cursor-pointer"
        onClick={() => fileInputRef.current?.click()}
      >
        <div
          className={cn(
            "w-full h-full rounded-full overflow-hidden flex items-center justify-center bg-(--board-primary) shadow-md border-4 transition-transform group-hover:scale-105",
            isHonoraryOrMerit
              ? "border-amber-400 ring-4 ring-amber-300/50 shadow-amber-200"
              : "border-white",
          )}
        >
          <img
            src={profilePictureSrc || "/profile-picture.svg"}
            className={
              profilePictureSrc && profilePictureSrc !== "/profile-picture.svg"
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

      <div className="mt-6 text-center">{children}</div>
    </div>
  );
}
