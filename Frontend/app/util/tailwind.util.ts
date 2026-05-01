import clsx, { type ClassValue } from "clsx";
import { twMerge } from "tailwind-merge";

/**
 * Merges multiple class names into a single string.
 * @param inputs An array of class values to merge.
 * @returns A single string containing all merged class names.
 */
export function cn(...inputs: ClassValue[]): string {
  return twMerge(clsx(inputs));
}
