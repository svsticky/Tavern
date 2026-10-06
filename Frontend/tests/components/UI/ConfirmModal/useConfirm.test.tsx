import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import { useConfirm } from "~/components/UI/ConfirmModal/useConfirm";
import { render } from "~/testUtils";

function TestComponent({ onResult }: { onResult: (result: boolean) => void }) {
  const [confirmModal, confirm] = useConfirm();

  return (
    <>
      <button
        onClick={async () => {
          const result = await confirm("Are you sure?");
          onResult(result);
        }}
      >
        trigger
      </button>
      {confirmModal}
    </>
  );
}

describe("useConfirm", () => {
  it("does not show the modal until confirm is called", () => {
    render(<TestComponent onResult={() => {}} />);
    expect(screen.queryByText("Are you sure?")).not.toBeInTheDocument();
  });

  it("resolves true when the confirm button is clicked", async () => {
    const user = userEvent.setup();
    const results: boolean[] = [];
    render(<TestComponent onResult={(r) => results.push(r)} />);

    await user.click(screen.getByText("trigger"));
    expect(await screen.findByText("Are you sure?")).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "confirm" }));

    expect(results).toEqual([true]);
    expect(screen.queryByText("Are you sure?")).not.toBeInTheDocument();
  });

  it("resolves false when the cancel button is clicked", async () => {
    const user = userEvent.setup();
    const results: boolean[] = [];
    render(<TestComponent onResult={(r) => results.push(r)} />);

    await user.click(screen.getByText("trigger"));
    expect(await screen.findByText("Are you sure?")).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "cancel" }));

    expect(results).toEqual([false]);
    expect(screen.queryByText("Are you sure?")).not.toBeInTheDocument();
  });

  it("resolves false when the modal is dismissed via Escape", async () => {
    const user = userEvent.setup();
    const results: boolean[] = [];
    render(<TestComponent onResult={(r) => results.push(r)} />);

    await user.click(screen.getByText("trigger"));
    expect(await screen.findByText("Are you sure?")).toBeInTheDocument();

    await user.keyboard("{Escape}");

    expect(results).toEqual([false]);
  });
});
