import { waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { Study } from "~/api";

const {
  getMailinglists,
  getRegistrationdocuments,
  getSettingsById,
  getStudies,
  postMembers,
  postPaymentsMembership,
} = vi.hoisted(() => ({
  getMailinglists: vi.fn(),
  getRegistrationdocuments: vi.fn(),
  getSettingsById: vi.fn(),
  getStudies: vi.fn(),
  postMembers: vi.fn(),
  postPaymentsMembership: vi.fn(),
}));

vi.mock("~/api", () => ({
  getMailinglists,
  getRegistrationdocuments,
  getSettingsById,
  getStudies,
  postMembers,
  postPaymentsMembership,
}));

vi.mock("react-hot-toast", () => ({
  default: {
    error: vi.fn(),
    promise: vi.fn((p: Promise<unknown>) => p.catch(() => {})),
  },
}));

import toast from "react-hot-toast";
import {
  handleRegisterInputChange,
  handleRegisterSubmit,
  handleStudyToggle,
  loadMailingLists,
  loadMastersMustPay,
  loadPrice,
  loadRegisterFormData,
  loadRegistrationDocuments,
  loadStudies,
  loadStudyStartDates,
} from "~/components/Register/RegisterForm/RegisterForm.handlers";

function makeEvent() {
  return { preventDefault: vi.fn() } as unknown as React.FormEvent;
}

function baseFormData() {
  return {
    firstname: "Jane",
    lastname: "Doe",
    email: "jane@example.com",
    birthDate: "2000-01-01",
    phone: "0612345678",
    parentPhone: "",
    street: "Main street",
    houseNumber: "1",
    postalCode: "1234AB",
    city: "Utrecht",
    studentNumber: "1234567",
  };
}

describe("RegisterForm.handlers", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe("loadStudies", () => {
    it("returns the studies on success", async () => {
      const studies: Study[] = [{ id: 1, title: "CS" } as Study];
      getStudies.mockResolvedValue({ data: studies });

      await expect(loadStudies()).resolves.toEqual(studies);
    });

    it("shows an error toast and returns no studies on failure", async () => {
      const consoleError = vi
        .spyOn(console, "error")
        .mockImplementation(() => {});
      getStudies.mockResolvedValue({ error: true, message: "boom" });

      await expect(loadStudies()).resolves.toEqual([]);
      expect(toast.error).toHaveBeenCalled();
      consoleError.mockRestore();
    });
  });

  describe("loadMastersMustPay", () => {
    it("returns true when the setting value is '1'", async () => {
      getSettingsById.mockResolvedValue({ data: { value: "1" } });

      await expect(loadMastersMustPay()).resolves.toBe(true);
      expect(getSettingsById).toHaveBeenCalledWith({
        path: { id: "MastersShouldPayMembership" },
      });
    });

    it("returns false when the setting value is anything else", async () => {
      getSettingsById.mockResolvedValue({ data: { value: "0" } });

      await expect(loadMastersMustPay()).resolves.toBe(false);
    });

    it("shows an error toast and returns null on failure", async () => {
      const consoleError = vi
        .spyOn(console, "error")
        .mockImplementation(() => {});
      getSettingsById.mockResolvedValue({ error: true });

      await expect(loadMastersMustPay()).resolves.toBeNull();
      expect(toast.error).toHaveBeenCalled();
      consoleError.mockRestore();
    });
  });

  describe("loadPrice", () => {
    it("returns the price and expiration time when both are set", async () => {
      getSettingsById.mockImplementation(({ path }: any) => {
        if (path.id === "MembershipPrice") {
          return Promise.resolve({ data: { value: "42.5" } });
        }
        return Promise.resolve({ data: { value: "2" } });
      });

      await expect(loadPrice()).resolves.toEqual({
        price: 42.5,
        membershipPaymentExpirationTime: 2,
      });
    });

    it("returns no expiration time when it is blank", async () => {
      getSettingsById.mockImplementation(({ path }: any) => {
        if (path.id === "MembershipPrice") {
          return Promise.resolve({ data: { value: "10" } });
        }
        return Promise.resolve({ data: { value: "   " } });
      });

      await expect(loadPrice()).resolves.toEqual({
        price: 10,
        membershipPaymentExpirationTime: null,
      });
    });

    it("shows an error toast and returns nulls when required values are missing", async () => {
      const consoleError = vi
        .spyOn(console, "error")
        .mockImplementation(() => {});
      getSettingsById.mockResolvedValue({ data: {} });

      await expect(loadPrice()).resolves.toEqual({
        price: null,
        membershipPaymentExpirationTime: null,
      });
      expect(toast.error).toHaveBeenCalled();
      consoleError.mockRestore();
    });
  });

  describe("loadMailingLists", () => {
    it("returns the mailing lists on success", async () => {
      const lists = [{ id: "list-1", name: "News" }];
      getMailinglists.mockResolvedValue({ data: lists });

      await expect(loadMailingLists()).resolves.toEqual(lists);
    });

    it("shows an error toast and returns no lists on failure", async () => {
      const consoleError = vi
        .spyOn(console, "error")
        .mockImplementation(() => {});
      getMailinglists.mockResolvedValue({ error: true });

      await expect(loadMailingLists()).resolves.toEqual([]);
      expect(toast.error).toHaveBeenCalled();
      consoleError.mockRestore();
    });
  });

  describe("loadRegistrationDocuments", () => {
    it("returns the documents sorted by sortOrder", async () => {
      getRegistrationdocuments.mockResolvedValue({
        data: [
          { id: 1, sortOrder: 2 },
          { id: 2, sortOrder: 1 },
        ],
      });

      await expect(loadRegistrationDocuments()).resolves.toEqual([
        { id: 2, sortOrder: 1 },
        { id: 1, sortOrder: 2 },
      ]);
    });

    it("returns no documents when there is no data", async () => {
      getRegistrationdocuments.mockResolvedValue({ data: undefined });

      await expect(loadRegistrationDocuments()).resolves.toEqual([]);
    });

    it("logs errors and returns no documents", async () => {
      const consoleError = vi
        .spyOn(console, "error")
        .mockImplementation(() => {});
      getRegistrationdocuments.mockRejectedValue(new Error("fail"));

      await expect(loadRegistrationDocuments()).resolves.toEqual([]);
      expect(consoleError).toHaveBeenCalled();
      consoleError.mockRestore();
    });
  });

  describe("loadStudyStartDates", () => {
    it("returns the start dates when a value is set", async () => {
      getSettingsById.mockResolvedValue({ data: { value: "09-01,02-01" } });

      await expect(loadStudyStartDates()).resolves.toBe("09-01,02-01");
    });

    it("returns null when there is no value", async () => {
      getSettingsById.mockResolvedValue({ data: {} });

      await expect(loadStudyStartDates()).resolves.toBeNull();
    });

    it("logs errors and returns null", async () => {
      const consoleError = vi
        .spyOn(console, "error")
        .mockImplementation(() => {});
      getSettingsById.mockRejectedValue(new Error("fail"));

      await expect(loadStudyStartDates()).resolves.toBeNull();
      consoleError.mockRestore();
    });
  });

  describe("loadRegisterFormData", () => {
    it("combines all the form's data", async () => {
      getStudies.mockResolvedValue({ data: [{ id: 1 }] });
      getMailinglists.mockResolvedValue({ data: [{ id: "list-1" }] });
      getRegistrationdocuments.mockResolvedValue({
        data: [{ id: 3, sortOrder: 1 }],
      });
      getSettingsById.mockImplementation(({ path }: any) =>
        Promise.resolve({
          data: {
            value: {
              MastersShouldPayMembership: "1",
              MembershipPrice: "25",
              MembershipPaymentExpirationTime: "7",
              StudyStartDates: "09-01",
            }[path.id as string],
          },
        }),
      );

      await expect(loadRegisterFormData()).resolves.toEqual({
        studies: [{ id: 1 }],
        mastersMustPay: true,
        price: 25,
        membershipPaymentExpirationTime: 7,
        mailingLists: [{ id: "list-1" }],
        documents: [{ id: 3, sortOrder: 1 }],
        studyStartDates: "09-01",
      });
    });
  });

  describe("handleRegisterInputChange", () => {
    it("updates the field matching the input's name", () => {
      const setFormData = vi.fn();
      handleRegisterInputChange(
        { target: { name: "firstname", value: "Jane" } } as any,
        setFormData,
      );

      const updater = setFormData.mock.calls[0][0];
      expect(updater({ firstname: "" })).toEqual({ firstname: "Jane" });
    });
  });

  describe("handleStudyToggle", () => {
    it("adds an id that is not yet selected", () => {
      const setSelectedStudies = vi.fn();
      handleStudyToggle(3, setSelectedStudies);
      const updater = setSelectedStudies.mock.calls[0][0];
      expect(updater([1, 2])).toEqual([1, 2, 3]);
    });

    it("removes an id that is already selected", () => {
      const setSelectedStudies = vi.fn();
      handleStudyToggle(2, setSelectedStudies);
      const updater = setSelectedStudies.mock.calls[0][0];
      expect(updater([1, 2])).toEqual([1]);
    });
  });

  describe("handleRegisterSubmit", () => {
    const navigate = vi.fn();
    let originalLocation: Location;

    beforeEach(() => {
      originalLocation = window.location;
      // @ts-expect-error - jsdom's location isn't directly assignable
      delete window.location;
      // @ts-expect-error - stub with just what the handler touches
      window.location = { href: "" };
    });

    afterEach(() => {
      // @ts-expect-error - restoring the real Location object after the stub above
      window.location = originalLocation;
    });

    it("does nothing when the form is not valid", async () => {
      const setLoading = vi.fn();

      await handleRegisterSubmit({
        e: makeEvent(),
        isFormValid: false,
        setLoading,
        formData: baseFormData(),
        selectedStudies: [1],
        selectedStartDate: "2024-09-01",
        subscribedMailinglistIds: [],
        studies: [],
        navigate,
        mastersMustPay: false,
      });

      expect(setLoading).not.toHaveBeenCalled();
      expect(postMembers).not.toHaveBeenCalled();
    });

    it("calls preventDefault even when the form is not valid", async () => {
      const e = makeEvent();
      await handleRegisterSubmit({
        e,
        isFormValid: false,
        setLoading: vi.fn(),
        formData: baseFormData(),
        selectedStudies: [],
        selectedStartDate: "",
        subscribedMailinglistIds: [],
        studies: [],
        navigate,
        mastersMustPay: null,
      });

      expect(e.preventDefault).toHaveBeenCalledTimes(1);
    });

    it("redirects to checkout when masters must pay is true", async () => {
      postMembers.mockResolvedValue({ status: 201, data: { id: "member-1" } });
      postPaymentsMembership.mockResolvedValue({
        status: 200,
        data: { checkoutUrl: "https://pay.example.com/checkout" },
      });
      const setLoading = vi.fn();

      await handleRegisterSubmit({
        e: makeEvent(),
        isFormValid: true,
        setLoading,
        formData: baseFormData(),
        selectedStudies: [1],
        selectedStartDate: "2024-09-01",
        subscribedMailinglistIds: [],
        studies: [{ id: 1, title: "Master CS", type: "Master" } as Study],
        navigate,
        mastersMustPay: true,
      });

      expect(postMembers).toHaveBeenCalledTimes(1);
      const payload = postMembers.mock.calls[0][0].body;
      expect(payload.firstName).toBe("Jane");
      expect(payload.preferredLanguage).toBe("EN");
      expect(payload.studyEnrollments).toEqual([
        {
          studyId: 1,
          memberId: "00000000-0000-0000-0000-000000000000",
          enrollmentDate: new Date("2024-09-01").toISOString(),
        },
      ]);

      await waitFor(() =>
        expect(postPaymentsMembership).toHaveBeenCalledWith({
          body: { memberId: "member-1" },
        }),
      );
      await waitFor(() =>
        expect(window.location.href).toBe("https://pay.example.com/checkout"),
      );
      await waitFor(() => expect(setLoading).toHaveBeenCalledWith(false));
    });

    it("redirects to checkout when no selected study is a Master (masters must pay unknown)", async () => {
      postMembers.mockResolvedValue({ status: 201, data: { id: "member-2" } });
      postPaymentsMembership.mockResolvedValue({
        status: 200,
        data: { checkoutUrl: "https://pay.example.com/checkout2" },
      });

      await handleRegisterSubmit({
        e: makeEvent(),
        isFormValid: true,
        setLoading: vi.fn(),
        formData: baseFormData(),
        selectedStudies: [1],
        selectedStartDate: "2024-09-01",
        subscribedMailinglistIds: [],
        studies: [{ id: 1, title: "Bachelor CS", type: "Bachelor" } as Study],
        navigate,
        mastersMustPay: false,
      });

      await waitFor(() =>
        expect(window.location.href).toBe("https://pay.example.com/checkout2"),
      );
      expect(navigate).not.toHaveBeenCalled();
    });

    it("navigates to confirm-mail when masters do not have to pay and a Master study is selected", async () => {
      postMembers.mockResolvedValue({ status: 201, data: { id: "member-3" } });

      await handleRegisterSubmit({
        e: makeEvent(),
        isFormValid: true,
        setLoading: vi.fn(),
        formData: baseFormData(),
        selectedStudies: [1],
        selectedStartDate: "2024-09-01",
        subscribedMailinglistIds: [],
        studies: [{ id: 1, title: "Master CS", type: "Master" } as Study],
        navigate,
        mastersMustPay: false,
      });

      await waitFor(() =>
        expect(navigate).toHaveBeenCalledWith(
          "/confirm-mail?memberId=member-3",
        ),
      );
      expect(postPaymentsMembership).not.toHaveBeenCalled();
    });

    it("throws when registration does not return status 201", async () => {
      const consoleError = vi
        .spyOn(console, "error")
        .mockImplementation(() => {});
      postMembers.mockResolvedValue({ status: 400, error: "bad" });

      await handleRegisterSubmit({
        e: makeEvent(),
        isFormValid: true,
        setLoading: vi.fn(),
        formData: baseFormData(),
        selectedStudies: [1],
        selectedStartDate: "2024-09-01",
        subscribedMailinglistIds: [],
        studies: [],
        navigate,
        mastersMustPay: true,
      });

      await waitFor(() => expect(consoleError).toHaveBeenCalled());
      expect(navigate).not.toHaveBeenCalled();
      expect(postPaymentsMembership).not.toHaveBeenCalled();
      consoleError.mockRestore();
    });

    it("throws when the payment initiation fails", async () => {
      const consoleError = vi
        .spyOn(console, "error")
        .mockImplementation(() => {});
      postMembers.mockResolvedValue({ status: 201, data: { id: "member-4" } });
      postPaymentsMembership.mockResolvedValue({ status: 500, error: "fail" });

      await handleRegisterSubmit({
        e: makeEvent(),
        isFormValid: true,
        setLoading: vi.fn(),
        formData: baseFormData(),
        selectedStudies: [1],
        selectedStartDate: "2024-09-01",
        subscribedMailinglistIds: [],
        studies: [],
        navigate,
        mastersMustPay: true,
      });

      await waitFor(() => expect(consoleError).toHaveBeenCalled());
      consoleError.mockRestore();
    });

    it("falls back to the current date when no start date is selected", async () => {
      postMembers.mockResolvedValue({ status: 201, data: { id: "member-5" } });
      postPaymentsMembership.mockResolvedValue({
        status: 200,
        data: { checkoutUrl: "https://pay.example.com/checkout3" },
      });

      await handleRegisterSubmit({
        e: makeEvent(),
        isFormValid: true,
        setLoading: vi.fn(),
        formData: baseFormData(),
        selectedStudies: [1],
        selectedStartDate: "",
        subscribedMailinglistIds: [],
        studies: [],
        navigate,
        mastersMustPay: true,
      });

      const payload = postMembers.mock.calls[0][0].body;
      expect(payload.studyEnrollments[0].enrollmentDate).not.toBe("");
      expect(
        () => new Date(payload.studyEnrollments[0].enrollmentDate),
      ).not.toThrow();
    });
  });
});
