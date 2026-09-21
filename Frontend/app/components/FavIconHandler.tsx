import { useEffect } from "react";
import { koalaMarkSvgString } from "./KoalaMark";

/**
 * A utility component that dynamically generates and updates the browser's favicon
 * based on the application's primary theme color.
 *
 * This component performs the following steps:
 * 1. Reads the `--board-primary` CSS variable from the document root.
 * 2. Injects that color into a predefined SVG template.
 * 3. Converts the SVG string into a Blob URL.
 * 4. Updates or creates a `<link rel="icon">` tag in the document head.
 *
 * This ensures the brand identity is reflected even in browser tabs without
 * requiring multiple static image files.
 *
 * @component
 * @returns {null} This component does not render any visual UI elements.
 */
const FaviconHandler = () => {
  useEffect(() => {
    const rootStyle = getComputedStyle(document.documentElement);
    const primaryColor =
      rootStyle.getPropertyValue("--board-primary").trim() || "#f96a1f";

    const svgString = koalaMarkSvgString(primaryColor);

    const blob = new Blob([svgString], { type: "image/svg+xml" });
    const url = URL.createObjectURL(blob);

    let link = document.querySelector(
      "link[rel~='icon']",
    ) as HTMLLinkElement | null;

    if (!link) {
      link = document.createElement("link");
      link.rel = "icon";
      document.head.appendChild(link);
    }
    link.href = url;

    return () => URL.revokeObjectURL(url);
  }, []);

  return null;
};

export default FaviconHandler;
