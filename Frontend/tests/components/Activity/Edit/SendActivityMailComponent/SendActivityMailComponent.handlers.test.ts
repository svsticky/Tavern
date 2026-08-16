import { beforeEach, describe, expect, it, vi } from "vitest";
import { handleSendMail } from "~/components/Activity/Edit/SendActivityMailComponent/SendActivityMailComponent.handlers";

const { postMailsActivity } = vi.hoisted(() => ({
  postMailsActivity: vi.fn(),
}));

vi.mock("~/api", () => ({ postMailsActivity }));

const toastErrorFn = vi.fn();
vi.mock("react-hot-toast", () => ({
  default: Object.assign((...args: unknown[]) => args, {
    success: vi.fn(),
    error: (...args: unknown[]) => toastErrorFn(...args),
    promise: vi.fn((p: Promise<unknown>, opts: any) => {
      p.then(
        (data) => opts.success?.(data),
        (err) => opts.error?.(err),
      ).catch(() => {});
      return p;
    }),
  }),
}));

describe("handleSendMail", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("shows an error toast and does not send when content is empty", async () => {
    await handleSendMail({
      activityId: 1,
      subject: "Hi",
      content: "",
      includeWaitingList: false,
      setLoading: vi.fn(),
      clearForm: vi.fn(),
    });

    expect(postMailsActivity).not.toHaveBeenCalled();
    expect(toastErrorFn).toHaveBeenCalled();
  });

  it("shows an error toast and does not send when content is an empty paragraph", async () => {
    await handleSendMail({
      activityId: 1,
      subject: "Hi",
      content: "<p><br></p>",
      includeWaitingList: false,
      setLoading: vi.fn(),
      clearForm: vi.fn(),
    });

    expect(postMailsActivity).not.toHaveBeenCalled();
    expect(toastErrorFn).toHaveBeenCalled();
  });

  it("sends the mail and clears the form on success", async () => {
    postMailsActivity.mockResolvedValue({});
    const setLoading = vi.fn();
    const clearForm = vi.fn();

    await handleSendMail({
      activityId: 1,
      subject: "Hi",
      content: "<p>Hello</p>",
      includeWaitingList: true,
      setLoading,
      clearForm,
    });

    expect(postMailsActivity).toHaveBeenCalledWith({
      body: {
        activityId: 1,
        htmlContent: "<p>Hello</p>",
        subject: "Hi",
        includeWaitingList: true,
      },
    });
    await vi.waitFor(() => expect(clearForm).toHaveBeenCalled());
    expect(setLoading).toHaveBeenCalledWith(true);
    expect(setLoading).toHaveBeenCalledWith(false);
  });

  it("does not clear the form when the response has an error", async () => {
    postMailsActivity.mockResolvedValue({ error: "fail" });
    const clearForm = vi.fn();

    await handleSendMail({
      activityId: 1,
      subject: "Hi",
      content: "<p>Hello</p>",
      includeWaitingList: false,
      setLoading: vi.fn(),
      clearForm,
    });

    await vi.waitFor(() => expect(postMailsActivity).toHaveBeenCalled());
    expect(clearForm).not.toHaveBeenCalled();
  });
});
