import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { GetSpecificationQuestionResponseDto } from "~/api";
import {
  ACTIVITY_CREATE_DRAFT_KEY,
  type ActivityDraft,
  clearActivityDraft,
  extractDraftFromForm,
  isActivityDraftValid,
  isDraftNotEmpty,
  loadActivityDraft,
  saveActivityDraft,
} from "~/util/activityDraft.util";

describe("activityDraft.util", () => {
  beforeEach(() => {
    localStorage.clear();
    vi.restoreAllMocks();
  });

  afterEach(() => {
    localStorage.clear();
  });

  describe("isDraftNotEmpty", () => {
    it("returns false for null or undefined", () => {
      expect(isDraftNotEmpty(null)).toBe(false);
      expect(isDraftNotEmpty(undefined as unknown as ActivityDraft)).toBe(
        false,
      );
    });

    it("returns false for empty object or whitespace-only fields", () => {
      expect(isDraftNotEmpty({})).toBe(false);
      expect(
        isDraftNotEmpty({
          name: "   ",
          location: "",
          dutchDescription: "  \n  ",
          englishDescription: "",
          specificationQuestions: [],
        }),
      );
    });

    it("returns true when any identifying field is filled", () => {
      expect(isDraftNotEmpty({ name: "Board Game Night" })).toBe(true);
      expect(isDraftNotEmpty({ location: "Sticky Room" })).toBe(true);
      expect(
        isDraftNotEmpty({ dutchDescription: "Gezellig spelletjes spelen" }),
      ).toBe(true);
      expect(isDraftNotEmpty({ englishDescription: "Fun board games" })).toBe(
        true,
      );
      expect(isDraftNotEmpty({ dateTimeStart: "2026-10-01T18:00" })).toBe(true);
      expect(isDraftNotEmpty({ dateTimeEnd: "2026-10-01T22:00" })).toBe(true);
      expect(isDraftNotEmpty({ enrollmentDeadline: "2026-10-01T12:00" })).toBe(
        true,
      );
      expect(
        isDraftNotEmpty({ unenrollmentDeadline: "2026-10-01T12:00" }),
      ).toBe(true);
      expect(isDraftNotEmpty({ enrollOpenDate: "2026-09-20T12:00" })).toBe(
        true,
      );
      expect(isDraftNotEmpty({ organizerId: "2" })).toBe(true);
      expect(isDraftNotEmpty({ price: "5.00" })).toBe(true);
      expect(isDraftNotEmpty({ participantLimit: "30" })).toBe(true);
      expect(isDraftNotEmpty({ vatRate: "21" })).toBe(true);
      expect(isDraftNotEmpty({ glAccountId: "1234" })).toBe(true);
      expect(isDraftNotEmpty({ costUnitId: "5678" })).toBe(true);
      expect(isDraftNotEmpty({ costCenterId: "9012" })).toBe(true);
      expect(isDraftNotEmpty({ paymentDeadline: "2026-10-01" })).toBe(true);
      expect(
        isDraftNotEmpty({
          specificationQuestions: [
            { questionDutch: "Dieetwensen?", type: "String" },
          ],
        }),
      ).toBe(true);
    });
  });

  describe("isActivityDraftValid", () => {
    it("returns false for null or empty object", () => {
      expect(isActivityDraftValid(null)).toBe(false);
      expect(isActivityDraftValid({})).toBe(false);
    });

    it("returns false if any required field is missing", () => {
      const base: ActivityDraft = {
        name: "LAN Party",
        location: "KBG",
        dutchDescription: "Gamen",
        englishDescription: "Gaming",
        dateTimeStart: "2026-10-01T18:00",
        dateTimeEnd: "2026-10-01T23:00",
        organizerId: "1",
      };

      expect(isActivityDraftValid({ ...base, name: "" })).toBe(false);
      expect(isActivityDraftValid({ ...base, location: "   " })).toBe(false);
      expect(isActivityDraftValid({ ...base, dutchDescription: "" })).toBe(
        false,
      );
      expect(isActivityDraftValid({ ...base, englishDescription: "" })).toBe(
        false,
      );
      expect(isActivityDraftValid({ ...base, dateTimeStart: undefined })).toBe(
        false,
      );
      expect(isActivityDraftValid({ ...base, dateTimeEnd: undefined })).toBe(
        false,
      );
      expect(isActivityDraftValid({ ...base, organizerId: "" })).toBe(false);
    });

    it("returns true when all required fields are present and non-empty", () => {
      const valid: ActivityDraft = {
        name: "LAN Party",
        location: "KBG",
        dutchDescription: "Gamen",
        englishDescription: "Gaming",
        dateTimeStart: "2026-10-01T18:00",
        dateTimeEnd: "2026-10-01T23:00",
        organizerId: "1",
      };
      expect(isActivityDraftValid(valid)).toBe(true);
    });
  });

  describe("extractDraftFromForm", () => {
    it("extracts all field values, checkboxes, audience flags, and questions from a form", () => {
      const form = document.createElement("form");

      form.innerHTML = `
        <input name="Name" value="Awesome Workshop" />
        <input name="Location" value="BBG 061" />
        <textarea name="DutchDescription">Leuke workshop</textarea>
        <textarea name="EnglishDescription">Fun workshop</textarea>
        <input name="DateTimeStart" value="2026-11-01T13:00" />
        <input name="DateTimeEnd" value="2026-11-01T17:00" />
        <input name="EnrollmentDeadline" value="2026-10-31T12:00" />
        <input name="UnenrollmentDeadline" value="2026-10-31T18:00" />
        <input name="EnrollOpenDate" value="2026-10-01T12:00" />
        <input name="IsWeeklyDrinks" type="checkbox" checked />
        <input name="AudienceBit" value="1" type="checkbox" checked />
        <input name="AudienceBit" value="2" type="checkbox" checked />
        <select name="OrganizerId">
          <option value="5" selected>Education Committee</option>
        </select>
        <input name="Price" value="2.50" />
        <input name="ParticipantLimit" value="25" />
        <input name="VatRate" value="21" />
        <input name="GLAccountId" value="4000" />
        <input name="CostUnitId" value="CU1" />
        <input name="CostCenterId" value="CC1" />
        <input name="PaymentDeadline" value="2026-11-05" />
        <input name="IsOpenForPayment" type="checkbox" checked />
        <input name="IsEnrollable" type="checkbox" checked />
        <input name="ShowInKoala" type="checkbox" />
        <input name="ShowOnWebsite" type="checkbox" checked />
        <input name="AreParticipantsVisible" type="checkbox" checked />
        <input name="IsAdultOnly" type="checkbox" />
      `;

      const questions: Partial<GetSpecificationQuestionResponseDto>[] = [
        { questionDutch: "Laptop mee?", type: "Boolean" },
      ];

      const draft = extractDraftFromForm(form, questions);

      expect(draft.name).toBe("Awesome Workshop");
      expect(draft.location).toBe("BBG 061");
      expect(draft.dutchDescription).toBe("Leuke workshop");
      expect(draft.englishDescription).toBe("Fun workshop");
      expect(draft.dateTimeStart).toBe("2026-11-01T13:00");
      expect(draft.dateTimeEnd).toBe("2026-11-01T17:00");
      expect(draft.enrollmentDeadline).toBe("2026-10-31T12:00");
      expect(draft.unenrollmentDeadline).toBe("2026-10-31T18:00");
      expect(draft.enrollOpenDate).toBe("2026-10-01T12:00");
      expect(draft.isWeeklyDrinks).toBe(true);
      expect(draft.organizerId).toBe("5");
      expect(draft.price).toBe("2.50");
      expect(draft.participantLimit).toBe("25");
      expect(draft.vatRate).toBe("21");
      expect(draft.glAccountId).toBe("4000");
      expect(draft.costUnitId).toBe("CU1");
      expect(draft.costCenterId).toBe("CC1");
      expect(draft.paymentDeadline).toBe("2026-11-05");
      expect(draft.isOpenForPayment).toBe(true);
      expect(draft.isEnrollable).toBe(true);
      expect(draft.showInKoala).toBe(false);
      expect(draft.showOnWebsite).toBe(true);
      expect(draft.areParticipantsVisible).toBe(true);
      expect(draft.isAdultOnly).toBe(false);
      expect(draft.specificationQuestions).toEqual(questions);
      expect(draft.savedAt).toBeDefined();
    });

    it("handles empty form extracting empty string fallbacks", () => {
      const emptyForm = document.createElement("form");
      const draft = extractDraftFromForm(emptyForm, []);
      expect(draft.name).toBe("");
      expect(draft.location).toBe("");
      expect(draft.dutchDescription).toBe("");
      expect(draft.englishDescription).toBe("");
      expect(draft.dateTimeStart).toBe("");
      expect(draft.dateTimeEnd).toBe("");
      expect(draft.enrollmentDeadline).toBe("");
      expect(draft.unenrollmentDeadline).toBe("");
      expect(draft.enrollOpenDate).toBe("");
      expect(draft.isWeeklyDrinks).toBe(false);
      expect(draft.organizerId).toBe("");
      expect(draft.price).toBe("");
      expect(draft.participantLimit).toBe("");
      expect(draft.vatRate).toBe("");
      expect(draft.glAccountId).toBe("");
      expect(draft.costUnitId).toBe("");
      expect(draft.costCenterId).toBe("");
      expect(draft.paymentDeadline).toBe("");
      expect(draft.isOpenForPayment).toBe(false);
      expect(draft.isEnrollable).toBe(false);
      expect(draft.showInKoala).toBe(false);
      expect(draft.showOnWebsite).toBe(false);
      expect(draft.areParticipantsVisible).toBe(false);
      expect(draft.isAdultOnly).toBe(false);
    });
  });

  describe("saveActivityDraft and loadActivityDraft", () => {
    it("saves and loads draft correctly", () => {
      const draft: ActivityDraft = {
        name: "BBQ Party",
        location: "Park",
        price: "10.00",
        savedAt: new Date().toISOString(),
      };

      saveActivityDraft(draft);

      const loaded = loadActivityDraft();
      expect(loaded).not.toBeNull();
      expect(loaded?.name).toBe("BBQ Party");
      expect(loaded?.location).toBe("Park");
      expect(loaded?.price).toBe("10.00");
    });

    it("returns null when no draft exists in localStorage", () => {
      expect(loadActivityDraft()).toBeNull();
    });

    it("returns null when stored content is invalid JSON", () => {
      localStorage.setItem(ACTIVITY_CREATE_DRAFT_KEY, "invalid-json{{");
      expect(loadActivityDraft()).toBeNull();
    });

    it("returns null when stored draft is empty or not an object", () => {
      localStorage.setItem(
        ACTIVITY_CREATE_DRAFT_KEY,
        JSON.stringify("string-not-object"),
      );
      expect(loadActivityDraft()).toBeNull();

      localStorage.setItem(ACTIVITY_CREATE_DRAFT_KEY, JSON.stringify({}));
      expect(loadActivityDraft()).toBeNull();
    });

    it("catches error if setItem throws (e.g. QuotaExceededError)", () => {
      const spy = vi
        .spyOn(Storage.prototype, "setItem")
        .mockImplementation(() => {
          throw new Error("Quota exceeded");
        });
      const consoleError = vi
        .spyOn(console, "error")
        .mockImplementation(() => {});

      expect(() => saveActivityDraft({ name: "Big Event" })).not.toThrow();

      expect(consoleError).toHaveBeenCalled();
      spy.mockRestore();
      consoleError.mockRestore();
    });
  });

  describe("clearActivityDraft", () => {
    it("removes draft from localStorage", () => {
      saveActivityDraft({ name: "Temp Activity" });
      expect(loadActivityDraft()).not.toBeNull();

      clearActivityDraft();
      expect(loadActivityDraft()).toBeNull();
      expect(localStorage.getItem(ACTIVITY_CREATE_DRAFT_KEY)).toBeNull();
    });

    it("catches error if removeItem throws", () => {
      const spy = vi
        .spyOn(Storage.prototype, "removeItem")
        .mockImplementation(() => {
          throw new Error("Storage failure");
        });
      const consoleError = vi
        .spyOn(console, "error")
        .mockImplementation(() => {});

      expect(() => clearActivityDraft()).not.toThrow();
      expect(consoleError).toHaveBeenCalled();

      spy.mockRestore();
      consoleError.mockRestore();
    });

    it("handles environment without localStorage gracefully", () => {
      const originalLocalStorage = window.localStorage;
      Object.defineProperty(window, "localStorage", {
        value: undefined,
        configurable: true,
        writable: true,
      });

      expect(loadActivityDraft()).toBeNull();
      expect(() => saveActivityDraft({ name: "No Storage" })).not.toThrow();
      expect(() => clearActivityDraft()).not.toThrow();

      Object.defineProperty(window, "localStorage", {
        value: originalLocalStorage,
        configurable: true,
        writable: true,
      });
    });
  });
});
