import { describe, expect, it } from "vitest";
import { appendErrorMessage, getErrorMessage } from "~/util/error.util";

describe("getErrorMessage", () => {
  it("returns the string directly when the error is a string", () => {
    expect(getErrorMessage("oops")).toBe("oops");
  });

  it("trims and ignores blank strings", () => {
    expect(getErrorMessage("   ")).toBeUndefined();
  });

  it("returns the message when the error is an Error instance", () => {
    expect(getErrorMessage(new Error("boom"))).toBe("boom");
  });

  it("returns undefined for null, undefined, and non-object primitives", () => {
    expect(getErrorMessage(null)).toBeUndefined();
    expect(getErrorMessage(undefined)).toBeUndefined();
    expect(getErrorMessage(42)).toBeUndefined();
  });

  it("extracts the first validation error message, prefixed by field name", () => {
    expect(
      getErrorMessage({
        errors: { Email: ["Email is required", "Email is invalid"] },
      }),
    ).toBe("Email: Email is required");
  });

  it("skips validation fields with empty arrays and falls through to the next", () => {
    expect(
      getErrorMessage({
        errors: { Empty: [], Name: ["Name is required"] },
      }),
    ).toBe("Name: Name is required");
  });

  it("falls back to `detail`, then `title`, then `message`, then `error`, in that order", () => {
    expect(getErrorMessage({ detail: "detail msg", title: "title msg" })).toBe(
      "detail msg",
    );
    expect(getErrorMessage({ title: "title msg", message: "msg" })).toBe(
      "title msg",
    );
    expect(getErrorMessage({ message: "msg", error: "err" })).toBe("msg");
    expect(getErrorMessage({ error: "err" })).toBe("err");
  });

  it("returns undefined when none of the known shapes match", () => {
    expect(getErrorMessage({ foo: "bar" })).toBeUndefined();
  });

  it("extracts a non-array validation error string, prefixed by field name", () => {
    expect(getErrorMessage({ errors: { General: "Something failed" } })).toBe(
      "General: Something failed",
    );
  });

  it("skips array fields with no valid string entries and falls through", () => {
    expect(
      getErrorMessage({
        errors: { Empty: ["", "   "], Name: ["Name is required"] },
      }),
    ).toBe("Name: Name is required");
  });

  it("returns undefined when the errors object has no usable messages", () => {
    expect(getErrorMessage({ errors: { Empty: [] } })).toBeUndefined();
  });

  it("returns undefined when errors is not an object", () => {
    expect(getErrorMessage({ errors: "not an object" })).toBeUndefined();
  });
});

describe("appendErrorMessage", () => {
  it("appends the extracted error message to the base message", () => {
    expect(appendErrorMessage("Save failed", new Error("network down"))).toBe(
      "Save failed: network down",
    );
  });

  it("returns just the base message when no error message can be extracted", () => {
    expect(appendErrorMessage("Save failed", undefined)).toBe("Save failed");
    expect(appendErrorMessage("Save failed", {})).toBe("Save failed");
  });
});
