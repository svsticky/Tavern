import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import PermissionChecklist, {
  MAX_CUSTOM_PERMISSION_COUNT,
  MAX_CUSTOM_PERMISSION_LENGTH,
} from "~/components/Permissions/PermissionChecklist";

const toastErrorFn = vi.fn();
const toastSuccessFn = vi.fn();
vi.mock("react-hot-toast", () => ({
  default: {
    error: (...args: unknown[]) => toastErrorFn(...args),
    success: (...args: unknown[]) => toastSuccessFn(...args),
  },
}));

describe("PermissionChecklist", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("shows a loading state while onLoad is pending", async () => {
    let resolveLoad: ((keys: string[]) => void) | undefined;
    const onLoad = vi.fn(
      () =>
        new Promise<string[]>((resolve) => {
          resolveLoad = resolve;
        }),
    );

    render(<PermissionChecklist onLoad={onLoad} onSave={vi.fn()} />);

    expect(screen.getByText("loading")).toBeInTheDocument();
    resolveLoad?.([]);
    await waitFor(() => expect(screen.queryByText("loading")).toBeNull());
  });

  it("pre-checks known permissions and lists custom ones from onLoad", async () => {
    const onLoad = vi.fn(async () => ["ViewFinances", "SomeOtherAppPerm"]);

    render(<PermissionChecklist onLoad={onLoad} onSave={vi.fn()} />);

    expect(await screen.findByLabelText("View Finances")).toBeChecked();
    expect(screen.getByLabelText("Manage Finances")).not.toBeChecked();
    expect(screen.getByText("SomeOtherAppPerm")).toBeInTheDocument();
  });

  it("renders the note when provided", async () => {
    render(
      <PermissionChecklist
        onLoad={vi.fn(async () => [])}
        onSave={vi.fn()}
        note="Applies to everyone in this group"
      />,
    );

    expect(
      await screen.findByText("Applies to everyone in this group"),
    ).toBeInTheDocument();
  });

  it("shows an error toast when onLoad rejects", async () => {
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});
    const onLoad = vi.fn(async () => {
      throw new Error("boom");
    });

    render(<PermissionChecklist onLoad={onLoad} onSave={vi.fn()} />);

    await waitFor(() => expect(toastErrorFn).toHaveBeenCalled());
    consoleError.mockRestore();
  });

  it("toggling a checkbox and saving calls onSave with the combined list", async () => {
    const onLoad = vi.fn(async () => []);
    const onSave = vi.fn(async () => {});

    render(<PermissionChecklist onLoad={onLoad} onSave={onSave} />);

    const checkbox = await screen.findByLabelText("View Members");
    fireEvent.click(checkbox);
    expect(checkbox).toBeChecked();

    fireEvent.click(screen.getByText("save_permissions"));

    await waitFor(() => expect(onSave).toHaveBeenCalledWith(["ViewMembers"]));
    await waitFor(() => expect(toastSuccessFn).toHaveBeenCalled());
  });

  it("shows an error toast when onSave rejects", async () => {
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});
    const onSave = vi.fn(async () => {
      throw new Error("boom");
    });

    render(
      <PermissionChecklist onLoad={vi.fn(async () => [])} onSave={onSave} />,
    );

    fireEvent.click(await screen.findByText("save_permissions"));

    await waitFor(() => expect(toastErrorFn).toHaveBeenCalled());
    consoleError.mockRestore();
  });

  it("adds and removes a custom permission", async () => {
    render(
      <PermissionChecklist onLoad={vi.fn(async () => [])} onSave={vi.fn()} />,
    );

    const input = await screen.findByPlaceholderText(
      "custom_permission_placeholder",
    );
    fireEvent.change(input, { target: { value: "CanApproveBudget" } });
    fireEvent.click(screen.getByText("add"));

    expect(screen.getByText("CanApproveBudget")).toBeInTheDocument();
    expect(input).toHaveValue("");

    fireEvent.click(screen.getByLabelText("remove"));
    expect(screen.queryByText("CanApproveBudget")).toBeNull();
  });

  it("adds a custom permission when Enter is pressed in the input", async () => {
    render(
      <PermissionChecklist onLoad={vi.fn(async () => [])} onSave={vi.fn()} />,
    );

    const input = await screen.findByPlaceholderText(
      "custom_permission_placeholder",
    );
    fireEvent.change(input, { target: { value: "CanDoThing" } });
    fireEvent.keyDown(input, { key: "Enter" });

    expect(screen.getByText("CanDoThing")).toBeInTheDocument();
  });

  it("disables adding a custom permission that duplicates a known permission name", async () => {
    render(
      <PermissionChecklist onLoad={vi.fn(async () => [])} onSave={vi.fn()} />,
    );

    const input = await screen.findByPlaceholderText(
      "custom_permission_placeholder",
    );
    fireEvent.change(input, { target: { value: "ViewFinances" } });

    expect(screen.getByText("add").closest("button")).toBeDisabled();
  });

  it("disables adding a custom permission longer than the max length", async () => {
    render(
      <PermissionChecklist onLoad={vi.fn(async () => [])} onSave={vi.fn()} />,
    );

    const input = await screen.findByPlaceholderText(
      "custom_permission_placeholder",
    );
    fireEvent.change(input, {
      target: { value: "a".repeat(MAX_CUSTOM_PERMISSION_LENGTH + 1) },
    });

    expect(screen.getByText("add").closest("button")).toBeDisabled();
  });

  it("locks all known permissions on when allKnownPermissionsGranted is set", async () => {
    const onLoad = vi.fn(async () => ["CustomOne"]);
    const onSave = vi.fn(async () => {});

    render(
      <PermissionChecklist
        onLoad={onLoad}
        onSave={onSave}
        allKnownPermissionsGranted
      />,
    );

    const checkbox = await screen.findByLabelText("Manage Groups");
    expect(checkbox).toBeChecked();
    expect(checkbox).toBeDisabled();

    // Clicking a disabled checkbox is a no-op in the browser; assert the toggle handler
    // itself doesn't flip it off even if invoked directly.
    fireEvent.click(checkbox);
    expect(checkbox).toBeChecked();

    expect(
      screen.getByText("all_permissions_granted_note"),
    ).toBeInTheDocument();

    fireEvent.click(screen.getByText("save_permissions"));

    await waitFor(() => expect(onSave).toHaveBeenCalledWith(["CustomOne"]));
  });

  it("stops allowing custom permissions once the count cap is reached", async () => {
    const existing = Array.from(
      { length: MAX_CUSTOM_PERMISSION_COUNT },
      (_, i) => `Custom${i}`,
    );
    render(
      <PermissionChecklist
        onLoad={vi.fn(async () => existing)}
        onSave={vi.fn()}
      />,
    );

    await screen.findByText("Custom0");
    expect(
      screen.getByText("custom_permissions_limit_reached"),
    ).toBeInTheDocument();

    const input = screen.getByPlaceholderText("custom_permission_placeholder");
    fireEvent.change(input, { target: { value: "OneMore" } });
    expect(screen.getByText("add").closest("button")).toBeDisabled();
  });
});
