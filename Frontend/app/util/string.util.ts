/**
 * Uppercases the first character of a string, leaving the rest untouched. Some locales (e.g. Dutch)
 * render weekday names/abbreviations lowercase by default, which is only correct when the weekday
 * isn't the first word of the sentence it appears in - callers should apply this at the specific
 * call sites where a `formatDate` result actually starts the sentence, not unconditionally.
 *
 * @param str - The string to capitalize.
 * @returns The string with its first character uppercased.
 */
export function capitalizeFirst(str: string): string {
  return str.charAt(0).toUpperCase() + str.slice(1);
}
