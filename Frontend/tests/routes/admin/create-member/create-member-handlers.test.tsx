import type { NavigateFunction } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { PostMemberDto } from "~/api";
import type { CreateBegunstigerFormData } from "~/routes/admin/create-member/create-member-handlers";

const { postMembers } = vi.hoisted(() => ({
  postMembers: vi.fn(),
}));

vi.mock("~/api", () => ({ postMembers }));

vi.mock("react-hot-toast", () => ({
  default: {
    success: vi.fn(),
    error: vi.fn(),
    promise: vi.fn((p) => {
      p.catch(() => {});
      return p;
    }),
  },
}));

import toast from "react-hot-toast";
import {
  handleCreateBegunstigerInputChange,
  handleCreateSubmit,
} from "~/routes/admin/create-member/create-member-handlers";

function baseFormData(
  overrides: Partial<CreateBegunstigerFormData> = {},
): CreateBegunstigerFormData {
  return {
    firstname: "John",
    lastname: "Doe",
    email: "john@example.com",
    birthDate: "2000-01-01",
    phone: "0612345678",
    parentPhone: "",
    street: "Main St",
    houseNumber: "1",
    postalCode: "1234AB",
    city: "Enschede",
    studentNumber: "s1234567",
    language: "NL",
    isBegunstiger: false,
    ...overrides,
  };
}

describe("handleCreateBegunstigerInputChange", () => {
  it("updates a text field in the form state", () => {
    const setFormData = vi.fn();
    handleCreateBegunstigerInputChange(
      {
        target: { name: "firstname", value: "Jane", type: "text" },
      } as unknown as React.ChangeEvent<HTMLInputElement>,
      setFormData,
    );

    const updater = setFormData.mock.calls[0][0];
    expect(updater(baseFormData())).toEqual(
      baseFormData({ firstname: "Jane" }),
    );
  });

  it("updates a checkbox field using the checked value", () => {
    const setFormData = vi.fn();
    handleCreateBegunstigerInputChange(
      {
        target: { name: "isBegunstiger", checked: true, type: "checkbox" },
      } as unknown as React.ChangeEvent<HTMLInputElement>,
      setFormData,
    );

    const updater = setFormData.mock.calls[0][0];
    expect(updater(baseFormData())).toEqual(
      baseFormData({ isBegunstiger: true }),
    );
  });
});

describe("handleCreateSubmit", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("does nothing when the form is invalid", async () => {
    const preventDefault = vi.fn();
    const setLoading = vi.fn();
    const navigate = vi.fn() as unknown as NavigateFunction;

    await handleCreateSubmit({
      e: { preventDefault } as unknown as React.FormEvent,
      isFormValid: false,
      setLoading,
      formData: baseFormData(),
      navigate,
    });

    expect(preventDefault).toHaveBeenCalled();
    expect(setLoading).not.toHaveBeenCalled();
    expect(postMembers).not.toHaveBeenCalled();
  });

  it("submits the payload and navigates to confirm-mail on success", async () => {
    postMembers.mockResolvedValue({ status: 201, data: { id: "1" } });
    const preventDefault = vi.fn();
    const setLoading = vi.fn();
    const navigate = vi.fn() as unknown as NavigateFunction;

    await handleCreateSubmit({
      e: { preventDefault } as unknown as React.FormEvent,
      isFormValid: true,
      setLoading,
      formData: baseFormData(),
      navigate,
    });

    await vi.waitFor(() =>
      expect(navigate).toHaveBeenCalledWith(
        "/confirm-mail?memberId=1&createdByAdmin=true",
      ),
    );

    const payload = postMembers.mock.calls[0][0].body as PostMemberDto;
    expect(payload.firstName).toBe("John");
    expect(payload.studentNumber).toBe("s1234567");
    expect(payload.parentPhoneNumber).toBeNull();
    expect(payload.begunstiger).toBe(false);
    expect(setLoading).toHaveBeenCalledWith(true);
    expect(setLoading).toHaveBeenLastCalledWith(false);
  });

  it("prefixes the student number with F_ for begunstigers", async () => {
    postMembers.mockResolvedValue({ status: 201, data: { id: "1" } });
    const navigate = vi.fn() as unknown as NavigateFunction;

    await handleCreateSubmit({
      e: { preventDefault: vi.fn() } as unknown as React.FormEvent,
      isFormValid: true,
      setLoading: vi.fn(),
      formData: baseFormData({ isBegunstiger: true, studentNumber: "1234567" }),
      navigate,
    });

    await vi.waitFor(() => expect(postMembers).toHaveBeenCalled());
    const payload = postMembers.mock.calls[0][0].body as PostMemberDto;
    expect(payload.studentNumber).toBe("F_1234567");
  });

  it("does not double-prefix a student number that already starts with F_", async () => {
    postMembers.mockResolvedValue({ status: 201, data: { id: "1" } });
    const navigate = vi.fn() as unknown as NavigateFunction;

    await handleCreateSubmit({
      e: { preventDefault: vi.fn() } as unknown as React.FormEvent,
      isFormValid: true,
      setLoading: vi.fn(),
      formData: baseFormData({
        isBegunstiger: true,
        studentNumber: "F_1234567",
      }),
      navigate,
    });

    await vi.waitFor(() => expect(postMembers).toHaveBeenCalled());
    const payload = postMembers.mock.calls[0][0].body as PostMemberDto;
    expect(payload.studentNumber).toBe("F_1234567");
  });

  it("shows an error toast and does not navigate when the response is not successful", async () => {
    postMembers.mockResolvedValue({ status: 400, error: { title: "bad" } });
    const setLoading = vi.fn();
    const navigate = vi.fn() as unknown as NavigateFunction;

    await handleCreateSubmit({
      e: { preventDefault: vi.fn() } as unknown as React.FormEvent,
      isFormValid: true,
      setLoading,
      formData: baseFormData(),
      navigate,
    });

    await vi.waitFor(() => expect(setLoading).toHaveBeenLastCalledWith(false));
    expect(navigate).not.toHaveBeenCalled();
    expect(toast.promise).toHaveBeenCalled();
  });

  it("propagates thrown/rejected errors from the API call", async () => {
    postMembers.mockRejectedValue(new Error("network down"));
    const setLoading = vi.fn();
    const navigate = vi.fn() as unknown as NavigateFunction;

    await handleCreateSubmit({
      e: { preventDefault: vi.fn() } as unknown as React.FormEvent,
      isFormValid: true,
      setLoading,
      formData: baseFormData(),
      navigate,
    });

    await vi.waitFor(() => expect(setLoading).toHaveBeenLastCalledWith(false));
    expect(navigate).not.toHaveBeenCalled();
  });
});
