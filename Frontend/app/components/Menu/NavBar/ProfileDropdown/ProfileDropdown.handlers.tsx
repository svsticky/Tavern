import type React from "react";

/**
 * Toggles the visibility state of the dropdown menu.
 *
 * Note: If the dropdown is in `compact` mode, the toggle action is ignored
 * (usually because compact modes are handled via hover or external triggers).
 *
 * @function toggleDropdown
 * @param {boolean} compact - Whether the dropdown is in a compact layout mode.
 * @param {React.Dispatch<React.SetStateAction<boolean>>} setIsOpen - State setter to toggle visibility.
 * @returns {void}
 */
export const toggleDropdown = (
  compact: boolean,
  setIsOpen: React.Dispatch<React.SetStateAction<boolean>>,
) => {
  if (!compact) setIsOpen((prev) => !prev);
};

/**
 * Executes a menu option's action and closes the dropdown.
 *
 * @function handleOptionClick
 * @param {() => void} action - The callback function associated with the clicked option.
 * @param {boolean} compact - Whether the dropdown is in a compact layout mode.
 * @param {React.Dispatch<React.SetStateAction<boolean>>} setIsOpen - State setter to close the menu.
 * @returns {void}
 */
export const handleOptionClick = (
  action: () => void,
  compact: boolean,
  setIsOpen: React.Dispatch<React.SetStateAction<boolean>>,
) => {
  action();
  if (!compact) setIsOpen(false);
};

/**
 * Detects clicks outside of the dropdown element to trigger an automatic close.
 *
 * Designed to be used within a `window` or `document` click event listener.
 *
 * @function handleClickOutside
 * @param {MouseEvent} event - The native DOM mouse event.
 * @param {React.RefObject<HTMLDivElement | null>} dropdownRef - A ref to the dropdown container to check for containment.
 * @param {React.Dispatch<React.SetStateAction<boolean>>} setIsOpen - State setter to force close the menu.
 * @returns {void}
 *
 * @example
 * ```tsx
 * useEffect(() => {
 *   const handler = (e) => handleClickOutside(e, myRef, setIsOpen);
 *   document.addEventListener("mousedown", handler);
 *   return () => document.removeEventListener("mousedown", handler);
 * }, []);
 * ```
 */
export const handleClickOutside = (
  event: MouseEvent,
  dropdownRef: React.RefObject<HTMLDivElement | null>,
  setIsOpen: React.Dispatch<React.SetStateAction<boolean>>,
) => {
  if (
    dropdownRef.current &&
    !dropdownRef.current.contains(event.target as Node)
  ) {
    setIsOpen(false);
  }
};
