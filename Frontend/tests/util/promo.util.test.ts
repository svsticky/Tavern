import { describe, expect, it } from "vitest";
import {
  formatPromoDate,
  formatPromoPrice,
  formatPromoTime,
  formatPromoTimeRange,
} from "~/util/promo.util";

describe("promo.util", () => {
  describe("formatPromoDate", () => {
    it("formats English dates with ordinals without year", () => {
      // 2026-10-27 is a Tuesday. In Europe/Amsterdam at 18:00 UTC (19:00 or 20:00 CEST/CET)
      const d1 = new Date("2026-10-27T18:00:00Z");
      expect(formatPromoDate(d1, "EN")).toBe("Tuesday 27th October");

      const d2 = new Date("2026-05-01T12:00:00Z");
      expect(formatPromoDate(d2, "EN")).toBe("Friday 1st May");

      const d3 = new Date("2026-05-02T12:00:00Z");
      expect(formatPromoDate(d3, "EN")).toBe("Saturday 2nd May");

      const d4 = new Date("2026-05-03T12:00:00Z");
      expect(formatPromoDate(d4, "EN")).toBe("Sunday 3rd May");

      const d11 = new Date("2026-05-11T12:00:00Z");
      expect(formatPromoDate(d11, "EN")).toBe("Monday 11th May");

      const d12 = new Date("2026-05-12T12:00:00Z");
      expect(formatPromoDate(d12, "EN")).toBe("Tuesday 12th May");

      const d13 = new Date("2026-05-13T12:00:00Z");
      expect(formatPromoDate(d13, "EN")).toBe("Wednesday 13th May");
    });

    it("formats Dutch dates without ordinals or year", () => {
      const d = new Date("2026-10-27T18:00:00Z");
      expect(formatPromoDate(d, "NL")).toBe("Dinsdag 27 oktober");
    });
  });

  describe("formatPromoTime", () => {
    it("formats time as HH:mm in Amsterdam timezone", () => {
      // 2026-10-27 was during CET (+01:00) after Oct 25 daylight saving change
      const d = new Date("2026-10-27T18:00:00Z");
      expect(formatPromoTime(d)).toBe("19:00");
    });
  });

  describe("formatPromoTimeRange", () => {
    it("formats single-day range as Date StartTime - EndTime", () => {
      const start = new Date("2026-10-27T18:00:00Z");
      const end = new Date("2026-10-27T21:00:00Z");
      expect(formatPromoTimeRange(start, end, "EN")).toBe(
        "Tuesday 27th October 19:00 - 22:00",
      );
      expect(formatPromoTimeRange(start, end, "NL")).toBe(
        "Dinsdag 27 oktober 19:00 - 22:00",
      );
    });

    it("formats multi-day range with both dates", () => {
      const start = new Date("2026-10-27T18:00:00Z");
      const end = new Date("2026-10-28T21:00:00Z");
      expect(formatPromoTimeRange(start, end, "EN")).toBe(
        "Tuesday 27th October 19:00 - Wednesday 28th October 22:00",
      );
    });
  });

  describe("formatPromoPrice", () => {
    it("formats 0 or null as Free / Gratis", () => {
      expect(formatPromoPrice(0, "EN")).toBe("Free");
      expect(formatPromoPrice(null, "EN")).toBe("Free");
      expect(formatPromoPrice(undefined, "EN")).toBe("Free");
      expect(formatPromoPrice(0, "NL")).toBe("Gratis");
      expect(formatPromoPrice(null, "NL")).toBe("Gratis");
      expect(formatPromoPrice(undefined, "NL")).toBe("Gratis");
    });

    it("formats numbers with 2 decimals", () => {
      expect(formatPromoPrice(9.5, "EN")).toBe("€ 9.50");
      expect(formatPromoPrice(10, "NL")).toBe("€ 10.00");
    });
  });
});
