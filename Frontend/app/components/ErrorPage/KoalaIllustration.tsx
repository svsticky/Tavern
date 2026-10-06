import KoalaMark from "~/components/KoalaMark";

export type KoalaMood = "lost" | "forbidden";

const INK = "#292623";

/**
 * The Koala logo with an attitude, used on the error pages. The `mood` decides
 * the expression: `forbidden` gets stern eyebrows, a frown and a "no entry" sign,
 * `lost` gets worried eyebrows and bouncing question marks.
 *
 * @component
 * @param {Object} props - The component properties.
 * @param {KoalaMood} props.mood - Which expression to give the koala.
 * @param {string} [props.className] - Extra classes for the root `<svg>`.
 */
const KoalaIllustration = ({
  mood,
  className,
}: {
  mood: KoalaMood;
  className?: string;
}) => {
  return (
    <svg
      className={className}
      viewBox="0 190 1024 730"
      xmlns="http://www.w3.org/2000/svg"
      aria-hidden="true"
      focusable="false"
    >
      <KoalaMark />

      {mood === "forbidden" ? (
        <>
          {/* Stern eyebrows, slanting down towards the nose */}
          <path
            d="M292 508 L424 560"
            stroke={INK}
            strokeWidth="30"
            strokeLinecap="round"
          />
          <path
            d="M706 548 L576 592"
            stroke={INK}
            strokeWidth="30"
            strokeLinecap="round"
          />
          {/* Disapproving frown */}
          <path
            d="M424 828 Q490 782 556 832"
            stroke={INK}
            strokeWidth="26"
            strokeLinecap="round"
            fill="none"
          />
          {/* No entry sign */}
          <circle cx="840" cy="790" r="92" fill="var(--board-primary)" />
          <rect x="778" y="763" width="124" height="54" rx="12" fill="#fff" />
        </>
      ) : (
        <>
          {/* Worried eyebrows, raised towards the middle */}
          <path
            d="M296 528 Q350 486 414 496"
            stroke={INK}
            strokeWidth="26"
            strokeLinecap="round"
            fill="none"
          />
          <path
            d="M584 540 Q640 528 696 572"
            stroke={INK}
            strokeWidth="26"
            strokeLinecap="round"
            fill="none"
          />
          {/* Small "hmm" mouth */}
          <path
            d="M446 806 Q490 792 534 810"
            stroke={INK}
            strokeWidth="24"
            strokeLinecap="round"
            fill="none"
          />
          {/* Floating question marks */}
          <text
            x="452"
            y="400"
            fontSize="190"
            fontWeight="800"
            fill="var(--board-primary)"
            className="animate-bounce"
          >
            ?
          </text>
          <text
            x="600"
            y="300"
            fontSize="100"
            fontWeight="800"
            fill="var(--board-primary)"
            opacity="0.7"
          >
            ?
          </text>
        </>
      )}
    </svg>
  );
};

export default KoalaIllustration;
