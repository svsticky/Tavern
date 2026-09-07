import { t } from "i18next";
import { Trash2 } from "lucide-react";
import { useEffect, useRef, useState } from "react";
import { getMembersByIdProfilePicture } from "~/api";
import { cn } from "~/util/tailwind.util";
import {
  handleProfilePictureDelete,
  handleProfilePictureUpload,
} from "./ChangeProfilePicture.handlers";

export type ProfileFrame = "default" | "primary" | "gold";

export const getStoredProfileFrame = (
  userId: string,
  isHonoraryOrMerit: boolean,
): ProfileFrame => {
  if (typeof window === "undefined") {
    return isHonoraryOrMerit ? "gold" : "default";
  }
  try {
    const saved = localStorage.getItem(
      `profile_frame_${userId}`,
    ) as ProfileFrame | null;
    if (saved === "gold" && !isHonoraryOrMerit) return "default";
    if (saved && ["default", "primary", "gold"].includes(saved)) return saved;
  } catch {
    // Ignore localStorage errors
  }
  return isHonoraryOrMerit ? "gold" : "default";
};

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
  const [selectedFrame, setSelectedFrame] = useState<ProfileFrame>(() =>
    getStoredProfileFrame(userId, isHonoraryOrMerit),
  );

  useEffect(() => {
    setSelectedFrame(getStoredProfileFrame(userId, isHonoraryOrMerit));
  }, [userId, isHonoraryOrMerit]);

  const handleFrameSelect = (frame: ProfileFrame) => {
    setSelectedFrame(frame);
    try {
      localStorage.setItem(`profile_frame_${userId}`, frame);
    } catch {
      // Ignore localStorage errors
    }
    window.dispatchEvent(
      new CustomEvent("profile_frame_changed", {
        detail: { userId, frame },
      }),
    );
  };

  const getFrameBorderClasses = () => {
    switch (selectedFrame) {
      case "gold":
        return "border-amber-400 ring-4 ring-amber-300/50 shadow-amber-200";
      case "primary":
        return "border-(--board-primary-dark) ring-4 ring-(--board-primary)/40 shadow-md";
      default:
        return "border-white shadow-md";
    }
  };

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
            "w-full h-full rounded-full overflow-hidden flex items-center justify-center bg-(--board-primary) transition-transform group-hover:scale-105 border-4",
            getFrameBorderClasses(),
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

      {profilePictureSrc && profilePictureSrc !== "/profile-picture.svg" && (
        <button
          type="button"
          onClick={() =>
            handleProfilePictureDelete(userId, () => {
              setProfilePictureSrc("/profile-picture.svg");
            })
          }
          className="mt-3 inline-flex items-center gap-1.5 px-3 py-1 text-xs font-medium text-red-600 hover:text-red-700 hover:bg-red-50 rounded-md border border-red-200 transition-colors cursor-pointer"
        >
          <Trash2 className="w-3.5 h-3.5" />
          {t("remove_profile_picture")}
        </button>
      )}

      <div className="mt-4 flex flex-col items-center gap-1.5 w-full">
        <span className="text-xs font-semibold text-gray-600 uppercase tracking-wider">
          {t("profile_frame")}
        </span>
        <fieldset
          className="flex items-center gap-1.5 justify-center flex-wrap"
          aria-label={t("profile_frame")}
        >
          <button
            type="button"
            aria-pressed={selectedFrame === "default"}
            onClick={() => handleFrameSelect("default")}
            className={cn(
              "px-2.5 py-1 text-xs font-medium rounded-full border transition-all cursor-pointer",
              selectedFrame === "default"
                ? "bg-(--board-primary) text-white border-(--board-primary) shadow-xs"
                : "bg-white text-gray-700 border-gray-300 hover:bg-gray-50",
            )}
          >
            {t("frame_default")}
          </button>
          <button
            type="button"
            aria-pressed={selectedFrame === "primary"}
            onClick={() => handleFrameSelect("primary")}
            className={cn(
              "px-2.5 py-1 text-xs font-medium rounded-full border transition-all cursor-pointer",
              selectedFrame === "primary"
                ? "bg-(--board-primary-dark) text-white border-(--board-primary-dark) shadow-xs"
                : "bg-white text-gray-700 border-gray-300 hover:bg-gray-50",
            )}
          >
            {t("frame_primary")}
          </button>
          {isHonoraryOrMerit && (
            <button
              type="button"
              aria-pressed={selectedFrame === "gold"}
              onClick={() => handleFrameSelect("gold")}
              className={cn(
                "px-2.5 py-1 text-xs font-bold rounded-full border transition-all cursor-pointer",
                selectedFrame === "gold"
                  ? "bg-amber-400 text-amber-950 border-amber-500 shadow-sm ring-2 ring-amber-300/50"
                  : "bg-amber-50 text-amber-800 border-amber-300 hover:bg-amber-100",
              )}
            >
              {t("frame_gold")}
            </button>
          )}
        </fieldset>
      </div>

      <div className="mt-6 text-center">{children}</div>
    </div>
  );
}
