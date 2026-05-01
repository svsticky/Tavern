import { useEffect, useState } from "react";
import type { EnrollmentResponseDto } from "~/api/types.gen";
import Tile from "../../Tiles/Tile";

/**
 * A compact tile component used to display an individual participant's information.
 *
 * Key features:
 * - **Profile Picture**: Fetches the member's profile picture or falls back to a default SVG.
 * - **Dynamic Answers**: If the enrollment contains multiple specification answers,
 *   it automatically cycles through them with a sliding animation every 3 seconds.
 * - **Hover Effects**: Includes subtle scaling and color transitions for better interactivity.
 *
 * @component
 * @param {Object} props - The component props.
 * @param {EnrollmentResponseDto} props.enrollment - The enrollment data, including member details and specification answers.
 *
 * @example
 * ```tsx
 * <ParticipantTile
 *   enrollment={enrollmentData}
 * />
 * ```
 */
export default function ParticipantTile({
  enrollment,
}: {
  enrollment: EnrollmentResponseDto;
}) {
  const imageUrl = `${import.meta.env.ApiUrl}/api/profilepicture/view/${enrollment.member.profilePicturePath}`;
  const fallbackUrl = "/profile-picture.svg";

  const [imgError, setImgError] = useState(false);
  const [currentAnswerIndex, setCurrentAnswerIndex] = useState(0);

  const isFallback = imgError || !enrollment.member.profilePicturePath;
  const answers = enrollment.specificationAnswers || [];
  const hasAnswers = answers.length > 0;

  useEffect(() => {
    if (answers.length <= 1) return;

    const interval = setInterval(() => {
      setCurrentAnswerIndex((prev) => (prev + 1) % answers.length);
    }, 3000);

    return () => clearInterval(interval);
  }, [answers.length]);

  return (
    <Tile className="bg-slate-50 flex items-center gap-4 border border-transparent hover:border-slate-200 hover:bg-white transition-all group cursor-default">
      <div className="relative flex-shrink-0">
        <div className="w-12 h-12 rounded-full overflow-hidden flex items-center justify-center shadow-inner group-hover:scale-105 transition-transform duration-200 bg-(--board-primary)">
          <img
            src={isFallback ? fallbackUrl : imageUrl}
            alt="Profile"
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
}
