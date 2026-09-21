import KoalaMark from "./KoalaMark";

/**
 * Full-screen splash shown while the app is bootstrapping (i18n + board theme,
 * and later, auth init). Renders the "Sticky" koala mark tinted to the board's
 * primary color with a buzzing loading animation, instead of a bare
 * "Loading..." text.
 */
const StickyLoadingLogo = () => {
  return (
    <main className="flex min-h-screen items-center justify-center bg-white">
      <svg
        className="h-24 w-24 animate-sticky-buzz drop-shadow-[0_6px_16px_rgba(0,0,0,0.12)]"
        viewBox="0 0 1024 1024"
        xmlns="http://www.w3.org/2000/svg"
        role="img"
        aria-label="Loading"
      >
        <KoalaMark />
      </svg>
    </main>
  );
};

export default StickyLoadingLogo;
