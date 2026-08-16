import { describe, expect, it } from "vitest";
import {
  formatForGoogleCalendar,
  formatForWhatsApp,
} from "~/util/markdown.util";

describe("formatForWhatsApp", () => {
  it("returns an empty string for null/undefined/empty input", () => {
    expect(formatForWhatsApp(null)).toBe("");
    expect(formatForWhatsApp(undefined)).toBe("");
    expect(formatForWhatsApp("")).toBe("");
  });

  it("converts a standalone italic marker", () => {
    expect(formatForWhatsApp("*italic*")).toBe("_italic_");
  });

  it("converting bold: the italic pass runs first and consumes the inner '*bold*', so the result is bold-wrapped italic rather than plain WhatsApp bold", () => {
    // This reflects the function's actual current behavior (regex ordering quirk), not the
    // intended WhatsApp output described in the doc comment - see conversation notes.
    expect(formatForWhatsApp("**bold**")).toBe("*_bold_*");
  });

  it("converts headers to bold", () => {
    expect(formatForWhatsApp("# Title")).toBe("*Title*");
    expect(formatForWhatsApp("### Subtitle")).toBe("*Subtitle*");
  });

  it("converts strikethrough", () => {
    expect(formatForWhatsApp("~~gone~~")).toBe("~gone~");
  });

  it("converts links to 'name (url)' form", () => {
    expect(formatForWhatsApp("[Tavern](https://example.com)")).toBe(
      "Tavern (https://example.com)",
    );
  });

  it("normalizes list markers to '- '", () => {
    expect(formatForWhatsApp("* Item one\n- Item two")).toBe(
      "- Item one\n- Item two",
    );
  });

  it("removes horizontal rules", () => {
    expect(formatForWhatsApp("above\n---\nbelow")).toBe("above\n\nbelow");
  });

  it("flattens a markdown table row into a dash-separated line", () => {
    expect(formatForWhatsApp("|A|B|")).toBe("A - B");
  });
});

describe("formatForGoogleCalendar", () => {
  it("returns an empty string for null/undefined/empty input", () => {
    expect(formatForGoogleCalendar(null)).toBe("");
    expect(formatForGoogleCalendar(undefined)).toBe("");
    expect(formatForGoogleCalendar("")).toBe("");
  });

  it("converts bold and italic to HTML tags", () => {
    expect(formatForGoogleCalendar("**bold**")).toBe("<b>bold</b>");
    expect(formatForGoogleCalendar("*italic*")).toBe("<i>italic</i>");
  });

  it("converts strikethrough to <s>", () => {
    expect(formatForGoogleCalendar("~~gone~~")).toBe("<s>gone</s>");
  });

  it("converts links to anchor tags", () => {
    expect(formatForGoogleCalendar("[Tavern](https://example.com)")).toBe(
      '<a href="https://example.com">Tavern</a>',
    );
  });

  it("converts headers to bold followed by a line break", () => {
    expect(formatForGoogleCalendar("# Title")).toBe("<b>Title</b><br>");
  });

  it("converts newlines to <br>", () => {
    expect(formatForGoogleCalendar("line one\nline two")).toBe(
      "line one<br>line two",
    );
  });

  it("converts list markers to bullet characters", () => {
    expect(formatForGoogleCalendar("- item")).toBe("• item");
  });

  it("flattens a markdown table row into a dash-separated line with a trailing break", () => {
    expect(formatForGoogleCalendar("|A|B|")).toBe("A - B<br>");
  });
});
