import type React from "react";

export const toggleDropdown = (compact: boolean, setIsOpen: React.Dispatch<React.SetStateAction<boolean>>) => {
  if (!compact) setIsOpen((prev) => !prev);
};

export const handleOptionClick = (action: () => void, compact: boolean, setIsOpen: React.Dispatch<React.SetStateAction<boolean>>) => {
  action();
  if (!compact) setIsOpen(false);
};

export const handleClickOutside = (
  event: MouseEvent,
  dropdownRef: React.RefObject<HTMLDivElement | null>,
  setIsOpen: React.Dispatch<React.SetStateAction<boolean>>
) => {
  if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
    setIsOpen(false);
  }
};
