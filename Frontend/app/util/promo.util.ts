import { getEnv } from "./config.utils";
import { isSameDayInAssociationTimeZone } from "./date.util";

function getOrdinalSuffix(day: number): string {
  if (day >= 11 && day <= 13) return "th";
  switch (day % 10) {
    case 1:
      return "st";
    case 2:
      return "nd";
    case 3:
      return "rd";
    default:
      return "th";
  }
}

/**
 * Formats a date for promotional messages (e.g. "Tuesday 27th October" or "Dinsdag 27 oktober")
 * without a full year, respecting the association timezone and specified language.
 */
export function formatPromoDate(date: Date, locale: "NL" | "EN"): string {
  const timeZone = getEnv("AssociationTimeZone") || "Europe/Amsterdam";
  const intlLocale = locale === "NL" ? "nl-NL" : "en-US";

  const weekdayFormatter = new Intl.DateTimeFormat(intlLocale, {
    timeZone,
    weekday: "long",
  });
  const rawWeekday = weekdayFormatter.format(date);
  const weekday = rawWeekday.charAt(0).toUpperCase() + rawWeekday.slice(1);

  const monthFormatter = new Intl.DateTimeFormat(intlLocale, {
    timeZone,
    month: "long",
  });
  const month = monthFormatter.format(date);

  const dayFormatter = new Intl.DateTimeFormat(intlLocale, {
    timeZone,
    day: "numeric",
  });
  const day = Number.parseInt(dayFormatter.format(date), 10);

  if (locale === "NL") {
    return `${weekday} ${day} ${month}`;
  }

  return `${weekday} ${day}${getOrdinalSuffix(day)} ${month}`;
}

/**
 * Formats a time as "HH:mm" in the association timezone.
 */
export function formatPromoTime(date: Date): string {
  const timeZone = getEnv("AssociationTimeZone") || "Europe/Amsterdam";
  const formatter = new Intl.DateTimeFormat("en-GB", {
    timeZone,
    hour: "2-digit",
    minute: "2-digit",
    hourCycle: "h23",
  });
  return formatter.format(date);
}

/**
 * Formats a promotional time range (e.g. "Tuesday 27th October 19:00 - 22:00").
 */
export function formatPromoTimeRange(
  startDate: Date,
  endDate: Date,
  locale: "NL" | "EN",
): string {
  const isSameDay = isSameDayInAssociationTimeZone(startDate, endDate);
  const startDay = formatPromoDate(startDate, locale);
  const startTime = formatPromoTime(startDate);
  const endTime = formatPromoTime(endDate);

  if (isSameDay) {
    return `${startDay} ${startTime} - ${endTime}`;
  }

  const endDay = formatPromoDate(endDate, locale);
  return `${startDay} ${startTime} - ${endDay} ${endTime}`;
}

/**
 * Formats a price for promotional messages (e.g. "€ 9.50" or "Gratis" / "Free").
 */
export function formatPromoPrice(
  price: number | null | undefined,
  locale: "NL" | "EN",
): string {
  if (price === 0 || price == null) {
    return locale === "NL" ? "Gratis" : "Free";
  }
  return `€ ${price.toFixed(2)}`;
}
