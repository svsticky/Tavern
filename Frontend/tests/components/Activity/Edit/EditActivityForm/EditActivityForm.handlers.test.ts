import type React from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type {
  GetSpecificationQuestionResponseDto,
  GroupResponseDto,
} from "~/api";
import {
  addQuestion,
  handleActivityFormChange,
  handleActivitySubmit,
  handleDeleteActivity,
  loadGroups,
  removeQuestion,
  updateQuestion,
} from "~/components/Activity/Edit/EditActivityForm/EditActivityForm.handlers";

const {
  postActivities,
  patchActivitiesById,
  postActivitiesByIdPoster,
  deleteActivitiesById,
  getGroups,
} = vi.hoisted(() => ({
  postActivities: vi.fn(),
  patchActivitiesById: vi.fn(),
  postActivitiesByIdPoster: vi.fn(),
  deleteActivitiesById: vi.fn(),
  getGroups: vi.fn(),
}));

vi.mock("~/api", () => ({
  postActivities,
  patchActivitiesById,
  postActivitiesByIdPoster,
  deleteActivitiesById,
  getGroups,
}));

const toastErrorFn = vi.fn();
vi.mock("react-hot-toast", () => ({
  default: {
    promise: vi.fn((promise: Promise<unknown>, opts: any) => {
      promise
        .then(
          (data) => opts.success?.(data),
          (err) => opts.error?.(err),
        )
        .catch(() => {});
      return promise;
    }),
    success: vi.fn(),
    error: (...args: unknown[]) => toastErrorFn(...args),
  },
}));

describe("loadGroups", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("sets groups on success", async () => {
    const groups: GroupResponseDto[] = [
      { id: 1, name: "Board" } as GroupResponseDto,
    ];
    getGroups.mockResolvedValue({ data: groups });
    const setGroups = vi.fn();
    const setLoading = vi.fn();

    await loadGroups(setLoading, setGroups);

    expect(setGroups).toHaveBeenCalledWith(groups);
    expect(setLoading).toHaveBeenCalledWith(false);
  });

  it("logs and shows an error toast on failure", async () => {
    getGroups.mockResolvedValue({ error: "fail" });
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});
    const setGroups = vi.fn();

    await loadGroups(vi.fn(), setGroups);

    expect(setGroups).not.toHaveBeenCalled();
    expect(consoleError).toHaveBeenCalled();
    expect(toastErrorFn).toHaveBeenCalled();
    consoleError.mockRestore();
  });
});

describe("addQuestion", () => {
  it("appends a blank question template", () => {
    const setQuestions = vi.fn();
    addQuestion([], setQuestions);

    expect(setQuestions).toHaveBeenCalledWith([
      {
        questionDutch: "",
        questionEnglish: "",
        type: "String",
        isMandatory: false,
        isPublic: true,
        options: [],
      },
    ]);
  });

  it("preserves existing questions when appending", () => {
    const existing = [{ questionDutch: "Existing" }];
    const setQuestions = vi.fn();
    addQuestion(existing, setQuestions);

    const result = setQuestions.mock.calls[0][0];
    expect(result).toHaveLength(2);
    expect(result[0]).toBe(existing[0]);
  });
});

describe("removeQuestion", () => {
  it("removes only the question at the given index", () => {
    const questions = [
      { questionDutch: "A" },
      { questionDutch: "B" },
      { questionDutch: "C" },
    ];
    const setQuestions = vi.fn();
    removeQuestion(1, questions, setQuestions);

    expect(setQuestions).toHaveBeenCalledWith([
      { questionDutch: "A" },
      { questionDutch: "C" },
    ]);
  });
});

describe("updateQuestion", () => {
  it("updates a single field without mutating other questions", () => {
    const questions: Partial<GetSpecificationQuestionResponseDto>[] = [
      { questionDutch: "A", isMandatory: false },
      { questionDutch: "B", isMandatory: false },
    ];
    const setQuestions = vi.fn();
    updateQuestion(0, "isMandatory", true, questions, setQuestions);

    expect(setQuestions).toHaveBeenCalledWith([
      { questionDutch: "A", isMandatory: true },
      { questionDutch: "B", isMandatory: false },
    ]);
    // original array must not be mutated
    expect(questions[0].isMandatory).toBe(false);
  });
});

describe("handleActivityFormChange", () => {
  function buildForm(fields: Record<string, string>) {
    const form = document.createElement("form");
    for (const [name, value] of Object.entries(fields)) {
      const input = document.createElement("input");
      input.name = name;
      input.value = value;
      form.appendChild(input);
    }
    return form;
  }

  const requiredFields = {
    Name: "Party",
    DateTimeStart: "2026-08-01T10:00",
    DateTimeEnd: "2026-08-01T12:00",
    Location: "Enschede",
    OrganizerId: "1",
    DutchDescription: "Feest",
    EnglishDescription: "Party",
  };

  it("marks the form valid when all required fields are present", () => {
    const form = buildForm(requiredFields);
    const setFormValid = vi.fn();

    handleActivityFormChange(
      { currentTarget: form } as unknown as React.FormEvent<HTMLFormElement>,
      setFormValid,
    );

    expect(setFormValid).toHaveBeenCalledWith(true);
  });

  it("marks the form invalid when a required field is missing", () => {
    const { Location: _Location, ...rest } = requiredFields;
    const form = buildForm(rest);
    const setFormValid = vi.fn();

    handleActivityFormChange(
      { currentTarget: form } as unknown as React.FormEvent<HTMLFormElement>,
      setFormValid,
    );

    expect(setFormValid).toHaveBeenCalledWith(false);
  });
});

describe("handleActivitySubmit", () => {
  function buildFormEvent(fields: Record<string, string>) {
    const form = document.createElement("form");
    for (const [name, value] of Object.entries(fields)) {
      const input = document.createElement("input");
      input.name = name;
      input.value = value;
      form.appendChild(input);
    }
    return {
      preventDefault: vi.fn(),
      currentTarget: form,
    } as unknown as React.FormEvent<HTMLFormElement>;
  }

  const baseFields = {
    Name: "Party",
    Location: "Enschede",
    DutchDescription: "Feest",
    EnglishDescription: "Party",
    Price: "5",
    DateTimeStart: "2026-08-01T10:00",
    DateTimeEnd: "2026-08-01T12:00",
    VatRate: "21",
    GLAccountId: "GL1",
    CostUnitId: "CU1",
    CostCenterId: "CC1",
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("creating as a non-board member strips financial fields from the payload", async () => {
    postActivities.mockResolvedValue({ data: { id: 1 } });
    const navigate = vi.fn();

    await handleActivitySubmit({
      e: buildFormEvent(baseFields),
      canEditStructural: false,
      canManageFinances: false,
      questions: [],
      setSaving: vi.fn(),
      isEdit: false,
      id: undefined,
      pathname: "/activities/new",
      navigate,
    });

    const payload = postActivities.mock.calls[0][0].body;
    expect(payload.VatRate).toBeUndefined();
    expect(payload.GLAccountId).toBeUndefined();
    expect(payload.CostUnitId).toBeUndefined();
    expect(payload.CostCenterId).toBeUndefined();
    expect(payload.ShowInKoala).toBe(false);
    expect(payload.ShowOnWebsite).toBe(false);
    expect(payload.IsEnrollable).toBe(false);
  });

  it("creating as a board member includes financial fields in the payload", async () => {
    postActivities.mockResolvedValue({ data: { id: 1 } });

    await handleActivitySubmit({
      e: buildFormEvent(baseFields),
      canEditStructural: true,
      canManageFinances: true,
      questions: [],
      setSaving: vi.fn(),
      isEdit: false,
      id: undefined,
      pathname: "/activities/new",
      navigate: vi.fn(),
    });

    const payload = postActivities.mock.calls[0][0].body;
    expect(payload.VatRate).toBe(21);
    expect(payload.GLAccountId).toBe("GL1");
    expect(payload.CostUnitId).toBe("CU1");
    expect(payload.CostCenterId).toBe("CC1");
  });

  it("editing as a non-board member omits financial patch operations entirely", async () => {
    patchActivitiesById.mockResolvedValue({});

    await handleActivitySubmit({
      e: buildFormEvent(baseFields),
      canEditStructural: false,
      canManageFinances: false,
      questions: [],
      setSaving: vi.fn(),
      isEdit: true,
      id: "5",
      pathname: "/activities/5/edit",
      navigate: vi.fn(),
    });

    const operations = patchActivitiesById.mock.calls[0][0].body as {
      path: string;
    }[];
    const paths = operations.map((op) => op.path);
    expect(paths).not.toContain("/VatRate");
    expect(paths).not.toContain("/GLAccountId");
    expect(paths).not.toContain("/CostUnitId");
    expect(paths).not.toContain("/CostCenterId");
    expect(paths).not.toContain("/ShowInKoala");
    expect(paths).not.toContain("/IsEnrollable");
  });

  it("editing as a board member includes financial patch operations", async () => {
    patchActivitiesById.mockResolvedValue({});

    await handleActivitySubmit({
      e: buildFormEvent(baseFields),
      canEditStructural: true,
      canManageFinances: true,
      questions: [],
      setSaving: vi.fn(),
      isEdit: true,
      id: "5",
      pathname: "/activities/5/edit",
      navigate: vi.fn(),
    });

    const operations = patchActivitiesById.mock.calls[0][0].body as {
      path: string;
      value: unknown;
    }[];
    const vatRateOp = operations.find((op) => op.path === "/VatRate");
    expect(vatRateOp?.value).toBe(21);
  });

  it("redirects to the created activity on success", async () => {
    postActivities.mockResolvedValue({ data: { id: 99 } });
    const navigate = vi.fn();

    await handleActivitySubmit({
      e: buildFormEvent(baseFields),
      canEditStructural: true,
      canManageFinances: true,
      questions: [],
      setSaving: vi.fn(),
      isEdit: false,
      id: undefined,
      pathname: "/activities/new",
      navigate,
    });

    expect(navigate).toHaveBeenCalledWith("/activities/99");
  });

  it("prefixes the admin path when submitting from the admin section", async () => {
    patchActivitiesById.mockResolvedValue({});
    const navigate = vi.fn();

    await handleActivitySubmit({
      e: buildFormEvent(baseFields),
      canEditStructural: true,
      canManageFinances: true,
      questions: [],
      setSaving: vi.fn(),
      isEdit: true,
      id: "5",
      pathname: "/admin/activities/5/edit",
      navigate,
    });

    expect(navigate).toHaveBeenCalledWith("/admin/activities/5");
  });

  it("calls setSaving(true) then setSaving(false) around the request", async () => {
    postActivities.mockResolvedValue({ data: { id: 1 } });
    const setSaving = vi.fn();

    await handleActivitySubmit({
      e: buildFormEvent(baseFields),
      canEditStructural: true,
      canManageFinances: true,
      questions: [],
      setSaving,
      isEdit: false,
      id: undefined,
      pathname: "/activities/new",
      navigate: vi.fn(),
    });

    expect(setSaving).toHaveBeenNthCalledWith(1, true);
    expect(setSaving).toHaveBeenNthCalledWith(2, false);
  });

  it("sums AudienceBit checkboxes into the AllowedAudience payload", async () => {
    postActivities.mockResolvedValue({ data: { id: 1 } });
    const form = document.createElement("form");
    for (const [name, value] of Object.entries(baseFields)) {
      const input = document.createElement("input");
      input.name = name;
      input.value = value;
      form.appendChild(input);
    }
    for (const bit of ["1", "2"]) {
      const input = document.createElement("input");
      input.type = "checkbox";
      input.name = "AudienceBit";
      input.value = bit;
      input.checked = true;
      form.appendChild(input);
    }
    const e = {
      preventDefault: vi.fn(),
      currentTarget: form,
    } as unknown as React.FormEvent<HTMLFormElement>;

    await handleActivitySubmit({
      e,
      canEditStructural: true,
      canManageFinances: true,
      questions: [],
      setSaving: vi.fn(),
      isEdit: false,
      id: undefined,
      pathname: "/activities/new",
      navigate: vi.fn(),
    });

    expect(postActivities.mock.calls[0][0].body.AllowedAudience).toBeTruthy();
  });

  it("uploads a poster file when editing and one is provided", async () => {
    patchActivitiesById.mockResolvedValue({});
    postActivitiesByIdPoster.mockResolvedValue({});
    const file = new File(["data"], "poster.png", { type: "image/png" });
    // jsdom's FormData doesn't faithfully read files assigned via a stubbed
    // input.files, so intercept FormData.get("Poster") directly for this test.
    const originalGet = FormData.prototype.get;
    const getSpy = vi
      .spyOn(FormData.prototype, "get")
      .mockImplementation(function (this: FormData, name: string) {
        if (name === "Poster") return file;
        return originalGet.call(this, name);
      });

    await handleActivitySubmit({
      e: buildFormEvent(baseFields),
      canEditStructural: true,
      canManageFinances: true,
      questions: [],
      setSaving: vi.fn(),
      isEdit: true,
      id: "5",
      pathname: "/activities/5/edit",
      navigate: vi.fn(),
    });

    await vi.waitFor(() =>
      expect(postActivitiesByIdPoster).toHaveBeenCalledWith({
        path: { id: 5 },
        body: { poster: file },
      }),
    );
    getSpy.mockRestore();
  });

  it("logs and rethrows when poster upload fails", async () => {
    patchActivitiesById.mockResolvedValue({});
    postActivitiesByIdPoster.mockResolvedValue({ error: "fail" });
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});
    const file = new File(["data"], "poster.png", { type: "image/png" });
    const originalGet = FormData.prototype.get;
    const getSpy = vi
      .spyOn(FormData.prototype, "get")
      .mockImplementation(function (this: FormData, name: string) {
        if (name === "Poster") return file;
        return originalGet.call(this, name);
      });

    await handleActivitySubmit({
      e: buildFormEvent(baseFields),
      canEditStructural: true,
      canManageFinances: true,
      questions: [],
      setSaving: vi.fn(),
      isEdit: true,
      id: "5",
      pathname: "/activities/5/edit",
      navigate: vi.fn(),
    });

    await vi.waitFor(() => expect(consoleError).toHaveBeenCalled());
    consoleError.mockRestore();
    getSpy.mockRestore();
  });

  it("logs and rethrows when updating fails", async () => {
    patchActivitiesById.mockResolvedValue({ error: "fail" });
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});

    await handleActivitySubmit({
      e: buildFormEvent(baseFields),
      canEditStructural: true,
      canManageFinances: true,
      questions: [],
      setSaving: vi.fn(),
      isEdit: true,
      id: "5",
      pathname: "/activities/5/edit",
      navigate: vi.fn(),
    });

    await vi.waitFor(() => expect(consoleError).toHaveBeenCalled());
    consoleError.mockRestore();
  });

  it("logs and rethrows when creation fails", async () => {
    postActivities.mockResolvedValue({ error: "fail" });
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});

    await handleActivitySubmit({
      e: buildFormEvent(baseFields),
      canEditStructural: true,
      canManageFinances: true,
      questions: [],
      setSaving: vi.fn(),
      isEdit: false,
      id: undefined,
      pathname: "/activities/new",
      navigate: vi.fn(),
    });

    await vi.waitFor(() => expect(consoleError).toHaveBeenCalled());
    consoleError.mockRestore();
  });
});

describe("handleDeleteActivity", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("calls onSuccess after a successful delete", async () => {
    deleteActivitiesById.mockResolvedValue({});
    const onSuccess = vi.fn();

    await handleDeleteActivity(5, onSuccess);

    expect(deleteActivitiesById).toHaveBeenCalledWith({ path: { id: 5 } });
    await vi.waitFor(() => expect(onSuccess).toHaveBeenCalled());
  });

  it("does not call onSuccess when delete fails", async () => {
    deleteActivitiesById.mockResolvedValue({ error: true, message: "bad" });
    const onSuccess = vi.fn();

    await handleDeleteActivity(5, onSuccess);

    expect(onSuccess).not.toHaveBeenCalled();
  });
});
