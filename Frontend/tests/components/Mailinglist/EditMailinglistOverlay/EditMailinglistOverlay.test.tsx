import { fireEvent, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import type { Mailinglist } from "~/api";
import EditMailingListOverlay from "~/components/Mailinglist/EditMailinglistOverlay/EditMailinglistOverlay";

const { handleMailingListDelete, handleMailingListSubmit } = vi.hoisted(() => ({
  handleMailingListDelete: vi.fn(),
  handleMailingListSubmit: vi.fn(),
}));

vi.mock(
  "~/components/Mailinglist/EditMailinglistOverlay/EditMailinglistOverlay.handlers",
  () => ({
    handleMailingListDelete,
    handleMailingListSubmit,
  }),
);

describe("EditMailingListOverlay", () => {
  it("renders empty fields and a disabled create button for a new list", () => {
    render(<EditMailingListOverlay onMailingListEdited={vi.fn()} />);

    expect(screen.getByLabelText(/name/)).toHaveValue("");
    expect(screen.getByRole("button", { name: "create" })).toBeDisabled();
    expect(
      screen.queryByRole("button", { name: "delete" }),
    ).not.toBeInTheDocument();
  });

  it("pre-fills fields and shows save/delete for an existing list", () => {
    const list: Mailinglist = {
      id: 1,
      name: "Newsletter",
      serviceId: "svc-1",
    } as Mailinglist;

    render(
      <EditMailingListOverlay
        onMailingListEdited={vi.fn()}
        mailingList={list}
      />,
    );

    expect(screen.getByLabelText(/name/)).toHaveValue("Newsletter");
    expect(screen.getByLabelText("service_id")).toHaveValue("svc-1");
    expect(screen.getByRole("button", { name: "save" })).toBeEnabled();
    expect(screen.getByRole("button", { name: "delete" })).toBeInTheDocument();
  });

  it("enables the create button once a name is entered", async () => {
    const user = userEvent.setup();
    render(<EditMailingListOverlay onMailingListEdited={vi.fn()} />);

    await user.type(screen.getByLabelText(/name/), "New list");

    expect(screen.getByRole("button", { name: "create" })).toBeEnabled();
  });

  it("updates the service id field as the user types", () => {
    render(<EditMailingListOverlay onMailingListEdited={vi.fn()} />);
    fireEvent.change(screen.getByLabelText(/service_id/), {
      target: { value: "newsletter_general" },
    });
    expect(screen.getByLabelText(/service_id/)).toHaveValue(
      "newsletter_general",
    );
  });

  it("calls handleMailingListSubmit when save/create is clicked", async () => {
    const user = userEvent.setup();
    const onMailingListEdited = vi.fn();
    render(
      <EditMailingListOverlay onMailingListEdited={onMailingListEdited} />,
    );

    await user.type(screen.getByLabelText(/name/), "New list");
    await user.click(screen.getByRole("button", { name: "create" }));

    expect(handleMailingListSubmit).toHaveBeenCalledWith(
      expect.objectContaining({
        formData: { name: "New list", serviceId: "" },
        onComplete: onMailingListEdited,
      }),
    );
  });

  it("calls handleMailingListDelete when delete is clicked", async () => {
    const user = userEvent.setup();
    const list: Mailinglist = { id: 1, name: "Newsletter" } as Mailinglist;
    const onMailingListEdited = vi.fn();
    render(
      <EditMailingListOverlay
        onMailingListEdited={onMailingListEdited}
        mailingList={list}
      />,
    );

    await user.click(screen.getByRole("button", { name: "delete" }));

    expect(handleMailingListDelete).toHaveBeenCalledWith({
      mailingList: list,
      setLoading: expect.any(Function),
      onComplete: onMailingListEdited,
    });
  });
});
