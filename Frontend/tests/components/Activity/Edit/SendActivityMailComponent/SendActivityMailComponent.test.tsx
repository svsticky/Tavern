import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import SendActivityMailComponent from "~/components/Activity/Edit/SendActivityMailComponent/SendActivityMailComponent";
import { handleSendMail } from "~/components/Activity/Edit/SendActivityMailComponent/SendActivityMailComponent.handlers";

vi.mock(
  "~/components/Activity/Edit/SendActivityMailComponent/SendActivityMailComponent.handlers",
  () => ({
    handleSendMail: vi.fn(),
  }),
);

vi.mock("react-quill-new/dist/quill.snow.css", () => ({}));
vi.mock("react-quill-new", () => ({
  default: ({
    value,
    onChange,
    placeholder,
  }: {
    value: string;
    onChange: (v: string) => void;
    placeholder?: string;
  }) => (
    <textarea
      data-testid="quill-stub"
      value={value}
      placeholder={placeholder}
      onChange={(e) => onChange(e.target.value)}
    />
  ),
}));

describe("SendActivityMailComponent", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("renders the subject input and disables send when the form is empty", () => {
    render(<SendActivityMailComponent activityId={1} />);
    expect(screen.getByLabelText("mail_subject")).toBeInTheDocument();
    expect(screen.getByText("send")).toBeDisabled();
  });

  it("enables send once subject and content are set, and calls handleSendMail", async () => {
    render(<SendActivityMailComponent activityId={1} />);

    fireEvent.change(screen.getByLabelText("mail_subject"), {
      target: { value: "Hello" },
    });

    const editor = await waitFor(() => screen.getByTestId("quill-stub"));
    fireEvent.change(editor, { target: { value: "<p>Body</p>" } });

    expect(screen.getByText("send")).not.toBeDisabled();

    fireEvent.click(screen.getByText("send"));

    expect(handleSendMail).toHaveBeenCalledWith(
      expect.objectContaining({
        activityId: 1,
        subject: "Hello",
        content: "<p>Body</p>",
        includeWaitingList: false,
      }),
    );
  });

  it("clears the form when handleSendMail invokes clearForm", async () => {
    vi.mocked(handleSendMail).mockImplementation(async ({ clearForm }) => {
      clearForm();
    });
    render(<SendActivityMailComponent activityId={1} />);

    fireEvent.change(screen.getByLabelText("mail_subject"), {
      target: { value: "Hello" },
    });
    const editor = await waitFor(() => screen.getByTestId("quill-stub"));
    fireEvent.change(editor, { target: { value: "<p>Body</p>" } });
    fireEvent.click(screen.getByText("send"));

    await waitFor(() =>
      expect(screen.getByLabelText("mail_subject")).toHaveValue(""),
    );
  });

  it("toggles includeWaitingList when the checkbox is clicked", () => {
    render(<SendActivityMailComponent activityId={1} />);
    fireEvent.click(screen.getByLabelText("include_waiting_list"));
    expect(screen.getByLabelText("include_waiting_list")).toBeChecked();
  });
});
