import { useState, useSyncExternalStore } from "react";
import { Link } from "react-router";
import type { EnrollmentResponseDto } from "~/api/types.gen";
import { getEnv } from "~/util/config.utils";
import Tile from "../../Tiles/Tile";

const ANSWER_CYCLE_INTERVAL_MS = 3000;

let cycleTick = 0;
let cycleIntervalId: ReturnType<typeof setInterval> | null = null;
const cycleListeners = new Set<() => void>();

/**
 * Subscribes to a single, module-wide "heartbeat" that ticks every
 * `ANSWER_CYCLE_INTERVAL_MS`. Every mounted ParticipantTile shares this one
 * timer instead of running its own `setInterval`, so all tiles advance to
 * their next specification answer on exactly the same tick rather than
 * drifting apart based on when each tile happened to mount.
 */
function subscribeToCycleTick(listener: () => void) {
  cycleListeners.add(listener);
  if (cycleIntervalId === null) {
    cycleTick = 0;
    cycleIntervalId = setInterval(() => {
      cycleTick += 1;
      for (const l of cycleListeners) l();
    }, ANSWER_CYCLE_INTERVAL_MS);
  }

  return () => {
    cycleListeners.delete(listener);
    if (cycleListeners.size === 0 && cycleIntervalId !== null) {
      clearInterval(cycleIntervalId);
      cycleIntervalId = null;
    }
  };
}

function getCycleTickSnapshot() {
  return cycleTick;
}

function useAnswerCycleTick() {
  return useSyncExternalStore(
    subscribeToCycleTick,
    getCycleTickSnapshot,
    () => 0,
  );
}

/**
 * A compact tile component used to display an individual participant's information.
 *
 * Key features:
 * - **Profile Picture**: Fetches the member's profile picture or falls back to a default SVG.
 * - **Dynamic Answers**: If the enrollment contains multiple public specification answers,
 *   it automatically cycles through them with a sliding animation, synchronized across every
 *   mounted ParticipantTile via a shared heartbeat.
 * - **Hover Effects**: Includes subtle scaling and color transitions for better interactivity.
 * - **Board Navigation**: Board members can click a tile to open that member's admin profile.
 *
 * @component
 * @param {Object} props - The component props.
 * @param {EnrollmentResponseDto} props.enrollment - The enrollment data, including member details and specification answers.
 * @param {boolean} [props.isBoard] - Whether the current viewer is a board member; when true, the tile links to the member's admin page.
 *
 * @example
 * ```tsx
 * <ParticipantTile
 *   enrollment={enrollmentData}
 *   isBoard={true}
 * />
 * ```
 */
export default function ParticipantTile({
  enrollment,
  isBoard,
}: {
  enrollment: EnrollmentResponseDto;
  isBoard?: boolean;
}) {
  const imageUrl = `${getEnv("ApiUrl")}/profilepicture/view/${enrollment.member.profilePicturePath}`;
  const fallbackUrl = "/profile-picture.svg";

  const [imgError, setImgError] = useState(false);

  const isFallback = imgError || !enrollment.member.profilePicturePath;
  const answers = (enrollment.specificationAnswers || []).filter(
    (answer) => answer.isPublic,
  );
  const hasAnswers = answers.length > 0;

  const cycleTick = useAnswerCycleTick();
  const currentAnswerIndex =
    answers.length > 0 ? cycleTick % answers.length : 0;

  const isClickable = Boolean(isBoard && enrollment.member.id);

  const tile = (
    <Tile
      className={`bg-slate-50 flex items-center gap-4 border border-transparent hover:border-slate-200 hover:bg-white transition-all group ${isClickable ? "cursor-pointer" : "cursor-default"}`}
    >
      <div className="relative flex-shrink-0">
        <div className="w-12 h-12 rounded-full overflow-hidden flex items-center justify-center shadow-inner group-hover:scale-105 transition-transform duration-200 bg-(--board-primary)">
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
        <p className="font-bold text-slate-900 truncate leading-tight group-hover:text-(--board-primary-dark) transition-colors">
          {enrollment.member.firstName} {enrollment.member.lastName}
        </p>

        {hasAnswers && (
          <div className="relative h-4 overflow-hidden mt-0.5">
            <p
              key={currentAnswerIndex}
              className="text-xs text-slate-500 truncate animate-slide-up"
            >
              {answers[currentAnswerIndex].answer}
            </p>
          </div>
        )}
      </div>
    </Tile>
  );

  if (!isClickable) return tile;

  return (
    <Link
      to={`/admin/members/${enrollment.member.id}`}
      className="no-underline text-inherit"
    >
      {tile}
    </Link>
  );
}
