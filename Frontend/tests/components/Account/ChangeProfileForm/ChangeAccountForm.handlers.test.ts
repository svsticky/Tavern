import { beforeEach, describe, expect, it, vi } from "vitest";
import {
  handleChangeEmail,
  handleChangePassword,
  handleConfigureMFA,
  handleSaveAccount,
  handleSubscriptionChange,
} from "~/components/Account/ChangeProfileForm/ChangeAccountForm.handlers";
import type { ChangeAccountFormData } from "~/components/Account/ChangeProfileForm/ChangeAccountForm.types";
import { createMockAuthService } from "~/testUtils";

const { patchMembersById } = vi.hoisted(() => ({
  patchMembersById: vi.fn(),
}));

vi.mock("~/api", () => ({ patchMembersById }));
vi.mock("react-hot-toast", () => ({
  default: {
    promise: vi.fn((p: Promise<unknown>, opts: any) => {
      p.then(
        (data) => opts?.success?.(data),
        (err) => opts?.error?.(err),
      ).catch(() => {});
      return p;
    }),
  },
}));

describe("handleSubscriptionChange", () => {
  it("sets the flag bit when checked", () => {
    const setFormData = vi.fn();
    handleSubscriptionChange(2, true, setFormData);

    const updater = setFormData.mock.calls[0][0];
    expect(updater({ mailSubscriptions: 1 })).toEqual({
      mailSubscriptions: 3,
    });
  });

  it("clears the flag bit when unchecked", () => {
    const setFormData = vi.fn();
    handleSubscriptionChange(2, false, setFormData);

    const updater = setFormData.mock.calls[0][0];
    expect(updater({ mailSubscriptions: 3 })).toEqual({
      mailSubscriptions: 1,
    });
  });
});

describe("auth redirect handlers", () => {
  beforeEach(() => {
    Object.defineProperty(window, "location", {
      value: { href: "" },
      writable: true,
    });
  });

  it("handleChangePassword redirects to the auth service's update-password URL", async () => {
    const authService = createMockAuthService({
      getUpdatePasswordUrl: vi.fn(async () => "https://kc.example.com/pw"),
    });

    await handleChangePassword(authService);

    expect(window.location.href).toBe("https://kc.example.com/pw");
  });

  it("handleChangeEmail redirects to the auth service's update-email URL", async () => {
    const authService = createMockAuthService({
      getUpdateEmailUrl: vi.fn(async () => "https://kc.example.com/email"),
    });

    await handleChangeEmail(authService);

    expect(window.location.href).toBe("https://kc.example.com/email");
  });

  it("handleConfigureMFA redirects to the auth service's MFA URL", async () => {
    const authService = createMockAuthService({
      configureMFA: vi.fn(async () => "https://kc.example.com/mfa"),
    });

    await handleConfigureMFA(authService);

    expect(window.location.href).toBe("https://kc.example.com/mfa");
  });

  it("falls back to /logout when no auth service is provided", async () => {
    // @ts-expect-error - exercising the falsy-authService branch intentionally
    await handleChangePassword(null);
    expect(window.location.href).toBe("/logout");

    // @ts-expect-error - exercising the falsy-authService branch intentionally
    await handleChangeEmail(null);
    expect(window.location.href).toBe("/logout");

    // @ts-expect-error - exercising the falsy-authService branch intentionally
    await handleConfigureMFA(null);
    expect(window.location.href).toBe("/logout");
  });
});

describe("handleSaveAccount", () => {
  const formData: ChangeAccountFormData = {
    phoneNumber: "0612345678",
    street: "Street",
    houseNumber: "1",
    postalCode: "1234AB",
    city: "Enschede",
    parentPhoneNumber: "",
    preferredLanguage: "EN",
    mailSubscriptions: 1,
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("patches the member and updates state on success", async () => {
    patchMembersById.mockResolvedValue({});
    const setSaving = vi.fn();
    const setMember = vi.fn();

    await handleSaveAccount("user-1", formData, setSaving, setMember);

    expect(patchMembersById).toHaveBeenCalledWith({
      path: { id: "user-1" },
      body: expect.arrayContaining([
        { op: "replace", path: "/phoneNumber", value: formData.phoneNumber },
      ]),
    });
    expect(setSaving).toHaveBeenNthCalledWith(1, true);
    await vi.waitFor(() => expect(setMember).toHaveBeenCalled());
    expect(setSaving).toHaveBeenNthCalledWith(2, false);

    const updater = setMember.mock.calls[0][0];
    expect(updater({ id: "user-1", phoneNumber: "old" })).toMatchObject({
      phoneNumber: formData.phoneNumber,
    });
    expect(updater(null)).toBeNull();
  });

  it("logs and still clears saving state when the patch fails", async () => {
    patchMembersById.mockResolvedValue({ error: { title: "Boom" } });
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});
    const setSaving = vi.fn();
    const setMember = vi.fn();

    await handleSaveAccount("user-1", formData, setSaving, setMember);

    await vi.waitFor(() => expect(consoleError).toHaveBeenCalled());
    expect(setSaving).toHaveBeenLastCalledWith(false);
    expect(setMember).not.toHaveBeenCalled();
    consoleError.mockRestore();
  });
});
