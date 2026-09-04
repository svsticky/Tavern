import { describe, expect, it } from "vitest";
import { capitalizeFirst } from "~/util/string.util";

describe("capitalizeFirst", () => {
  it("uppercases the first character of a lowercase string", () => {
    expect(capitalizeFirst("maandag")).toBe("Maandag");
  });

  it("leaves an already-capitalized string unchanged", () => {
    expect(capitalizeFirst("Monday")).toBe("Monday");
  });

  it("leaves the rest of the string untouched", () => {
    expect(capitalizeFirst("woensdag 12 maart")).toBe("Woensdag 12 maart");
  });

  it("returns an empty string unchanged", () => {
    expect(capitalizeFirst("")).toBe("");
  });

  it("handles a single-character string", () => {
    expect(capitalizeFirst("a")).toBe("A");
  });
});
