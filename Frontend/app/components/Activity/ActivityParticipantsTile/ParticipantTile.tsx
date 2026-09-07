import { useContext, useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { Link } from "react-router";
import type { EnrollmentResponseDto } from "~/api/types.gen";
import AuthContext from "~/context/AuthContext";
import type { TokenParsed } from "~/types/TokenParsed";
import { getEnv } from "~/util/config.utils";
import { isBoardOrCandidateBoard } from "~/util/group.util";
import { cn } from "~/util/tailwind.util";
import Tile from "../../Tiles/Tile";

/**
 * A compact tile component used to display an individual participant's information.
 *
 * Key features:
 * - **Profile Picture**: Fetches the member's profile picture or falls back to a default SVG.
 * - **Dynamic Answers**: If the enrollment contains multiple specification answers,
 *   it automatically cycles through them with a sliding animation every 3 seconds.
 * - **Hover Effects**: Includes subtle scaling and color transitions for admins who can interact with it.
 * - **Honorary / Merit Recognition**: Displays a gold border and badge for honorary members and members of merit.
 * - **Admin Interactivity**: Registered members are clickable for admins to open their details page.
 *
 * @component
 * @param {Object} props - The component props.
 * @param {EnrollmentResponseDto} props.enrollment - The enrollment data, including member details and specification answers.
 * @param {boolean} [props.isAdmin] - Optional flag indicating whether the viewer has admin privileges.
 */
export default function ParticipantTile({
  enrollment,
  isAdmin: isAdminProp,
}: {
  enrollment: EnrollmentResponseDto;
  isAdmin?: boolean;
}) {
  const { t } = useTranslation();
  const authService = useContext(AuthContext);
  const [tokenParsed, setTokenParsed] = useState<TokenParsed | null>(null);

  useEffect(() => {
    if (isAdminProp !== undefined || !authService) return;
    let cancelled = false;
    authService.getTokenParsed().then((token) => {
      if (!cancelled) setTokenParsed(token);
    });
    return () => {
      cancelled = true;
    };
  }, [authService, isAdminProp]);

  const isAdmin = isAdminProp ?? isBoardOrCandidateBoard(tokenParsed);
  const isClickable = isAdmin && Boolean(enrollment.member.id);

  const imageUrl = `${getEnv("ApiUrl")}/profilepicture/view/${enrollment.member.profilePicturePath}`;
  const fallbackUrl = "/profile-picture.svg";

  const [imgError, setImgError] = useState(false);
  const [currentAnswerIndex, setCurrentAnswerIndex] = useState(0);

  const isFallback = imgError || !enrollment.member.profilePicturePath;
  const answers = enrollment.specificationAnswers || [];
  const hasAnswers = answers.length > 0;
  const isHonoraryOrMerit = Boolean(
    enrollment.member.ereLid || enrollment.member.lidVanVerdienste,
  );

  useEffect(() => {
    if (answers.length <= 1) return;

    const interval = setInterval(() => {
      setCurrentAnswerIndex((prev) => (prev + 1) % answers.length);
    }, 3000);

    return () => clearInterval(interval);
  }, [answers.length]);

  const tileContent = (
    <Tile
      className={cn(
        "bg-gray-50 flex items-center gap-4 border transition-all",
        isClickable ? "cursor-pointer" : "cursor-default",
        isHonoraryOrMerit
          ? cn(
              "border-amber-400 bg-amber-50/20 shadow-[0_0_0_1px_rgba(251,191,36,0.5)]",
              isClickable &&
                "group-hover:border-amber-500 group-hover:bg-amber-50/40",
            )
          : cn(
              "border-transparent",
              isClickable && "group-hover:border-gray-200 group-hover:bg-white",
            ),
      )}
    >
      <div className="relative flex-shrink-0">
        <div
          className={cn(
            "w-12 h-12 rounded-full overflow-hidden flex items-center justify-center shadow-inner bg-(--board-primary)",
            isClickable &&
              "group-hover:scale-105 transition-transform duration-200",
            isHonoraryOrMerit && "ring-2 ring-amber-400 ring-offset-2",
          )}
        >
          <img
            src={isFallback ? fallbackUrl : imageUrl}
            alt="Profile"
            crossOrigin="use-credentials"
            loading="lazy"
            onError={() => setImgError(true)}
            className={
              isFallback
                ? "w-8 h-8 object-contain"
                : "w-full h-full object-cover"
            }
          />
        </div>
      </div>

      <div className="overflow-hidden flex flex-col justify-center min-w-0">
        <div className="flex items-center gap-1.5 min-w-0">
          <p
            className={cn(
              "font-bold text-gray-900 truncate leading-tight",
              isClickable &&
                "group-hover:text-(--board-primary-dark) transition-colors",
            )}
          >
            {enrollment.member.firstName} {enrollment.member.lastName}
          </p>
          {enrollment.member.ereLid && (
            <span
              className="shrink-0 px-1.5 py-0.5 text-[10px] font-bold rounded-full bg-amber-100 text-amber-800 border border-amber-400"
              title={t("ere_lid")}
            >
              {t("ere_lid")}
            </span>
          )}
          {!enrollment.member.ereLid && enrollment.member.lidVanVerdienste && (
            <span
              className="shrink-0 px-1.5 py-0.5 text-[10px] font-bold rounded-full bg-amber-100 text-amber-800 border border-amber-400"
              title={t("lid_van_verdienste")}
            >
              {t("lid_van_verdienste")}
            </span>
          )}
        </div>

        {hasAnswers && (
          <div className="relative h-4 overflow-hidden mt-0.5">
            <p
              key={currentAnswerIndex}
              className="text-xs text-gray-500 truncate animate-slide-up"
            >
              {answers[currentAnswerIndex].answer}
            </p>
          </div>
        )}
      </div>
    </Tile>
  );

  if (isClickable) {
    return (
      <Link
        to={`/admin/members/${enrollment.member.id}`}
        className="block group"
      >
        {tileContent}
      </Link>
    );
  }

  return tileContent;
}
