import { describe, expect, it, vi } from "vitest";
import type { ActivityResponseDto } from "~/api";
import {
  downloadActivityEnrollmentsCsv,
  escapeCsvField,
  generateActivityEnrollmentsCsv,
} from "~/util/activityCsv.util";

describe("activityCsv.util", () => {
  describe("escapeCsvField", () => {
    it("returns empty string for null and undefined", () => {
      expect(escapeCsvField(null)).toBe("");
      expect(escapeCsvField(undefined)).toBe("");
    });

    it("returns regular strings as-is", () => {
      expect(escapeCsvField("Hello World")).toBe("Hello World");
      expect(escapeCsvField(123)).toBe("123");
    });

    it("escapes semicolons and quotes properly", () => {
      expect(escapeCsvField("Hello; World")).toBe('"Hello; World"');
      expect(escapeCsvField('He said "Hi"')).toBe('"He said ""Hi"""');
      expect(escapeCsvField("Multi\nLine")).toBe('"Multi\nLine"');
    });

    it("neutralizes potential formula injection characters", () => {
      expect(escapeCsvField("=SUM(1+1)")).toBe("'=SUM(1+1)");
      expect(escapeCsvField("+cmd")).toBe("'+cmd");
      expect(escapeCsvField("-10")).toBe("'-10");
      expect(escapeCsvField("@eval")).toBe("'@eval");
    });
  });

  describe("generateActivityEnrollmentsCsv", () => {
    const mockActivity = {
      id: 1,
      name: "Tavern Borrel",
      price: 0,
      location: "Sticky Room",
      dateTimeStart: "2026-09-10T16:00:00Z",
      dateTimeEnd: "2026-09-10T22:00:00Z",
      specificationQuestions: [
        {
          id: 10,
          questionDutch: "Dieetwensen",
          questionEnglish: "Dietary restrictions",
          type: "String",
          isMandatory: false,
          isPublic: true,
        },
        {
          id: 20,
          questionDutch: "T-Shirt Maat",
          questionEnglish: "T-Shirt Size",
          type: "MultipleChoice",
          isMandatory: true,
          isPublic: true,
        },
      ],
      enrollments: [
        {
          isOnWaitingList: true,
          member: {
            id: "user-2",
            firstName: "Bob",
            lastName: "Waitlist",
            email: "bob@example.com",
          },
          specificationAnswers: [
            { answerId: 2, questionId: 10, answer: "Geen; Vegetarisch" },
          ],
          activity: null!,
        },
        {
          isOnWaitingList: false,
          member: {
            id: "user-1",
            firstName: "Alice",
            lastName: "Participant",
            email: "alice@example.com",
          },
          specificationAnswers: [
            { answerId: 1, questionId: 10, answer: "Vegan" },
            { answerId: 3, questionId: 20, answer: "L" },
          ],
          activity: null!,
        },
      ],
    } as unknown as ActivityResponseDto;

    it("includes UTF-8 BOM, Dutch headers, and sorts participants before waiting list", () => {
      const csv = generateActivityEnrollmentsCsv(mockActivity, true);

      expect(csv.startsWith("\uFEFF")).toBe(true);

      const lines = csv.replace(/^\uFEFF/, "").split("\r\n");
      expect(lines[0]).toBe(
        "Voornaam;Achternaam;E-mail;Status;Dieetwensen;T-Shirt Maat",
      );

      // Participant first
      expect(lines[1]).toBe(
        "Alice;Participant;alice@example.com;Deelnemer;Vegan;L",
      );

      // Waiting list second, with escaped semicolon answer
      expect(lines[2]).toBe(
        'Bob;Waitlist;bob@example.com;Wachtlijst;"Geen; Vegetarisch";',
      );
    });

    it("generates English headers when isDutch is false", () => {
      const csv = generateActivityEnrollmentsCsv(mockActivity, false);
      const lines = csv.replace(/^\uFEFF/, "").split("\r\n");

      expect(lines[0]).toBe(
        "First Name;Last Name;Email;Status;Dietary restrictions;T-Shirt Size",
      );
      expect(lines[1]).toContain("Participant");
      expect(lines[2]).toContain("Waiting List");
    });
  });

  describe("downloadActivityEnrollmentsCsv", () => {
    it("creates an anchor and triggers download", () => {
      const createObjectURLMock = vi.fn().mockReturnValue("blob:mock-url");
      const revokeObjectURLMock = vi.fn();
      globalThis.URL.createObjectURL = createObjectURLMock;
      globalThis.URL.revokeObjectURL = revokeObjectURLMock;

      const clickSpy = vi
        .spyOn(HTMLAnchorElement.prototype, "click")
        .mockImplementation(() => {});

      downloadActivityEnrollmentsCsv({
        id: 1,
        name: "Cool Event",
        price: 0,
        dateTimeStart: "2026-09-10T16:00:00Z",
        dateTimeEnd: "2026-09-10T22:00:00Z",
        enrollments: [],
        specificationQuestions: [],
      } as unknown as ActivityResponseDto);

      expect(createObjectURLMock).toHaveBeenCalled();
      expect(clickSpy).toHaveBeenCalled();
      expect(revokeObjectURLMock).toHaveBeenCalledWith("blob:mock-url");

      clickSpy.mockRestore();
    });
  });
});
