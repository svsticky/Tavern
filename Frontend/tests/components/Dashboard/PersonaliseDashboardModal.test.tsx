import { fireEvent, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import PersonaliseDashboardModal from "~/components/Dashboard/PersonaliseDashboardModal";
import { renderWithProviders } from "~/testUtils";
import {
  type DashboardWidgetConfig,
  DEFAULT_DASHBOARD_WIDGETS,
} from "~/util/dashboardWidgets";

describe("PersonaliseDashboardModal", () => {
  const mockOnClose = vi.fn();
  const mockOnSave = vi.fn();
  const mockOnReset = vi.fn();

  beforeEach(() => {
    vi.clearAllMocks();
  });

  const defaultProps = {
    isOpen: true,
    onClose: mockOnClose,
    widgets: DEFAULT_DASHBOARD_WIDGETS.map((w) => ({ ...w })),
    onSave: mockOnSave,
    onReset: mockOnReset,
  };

  it("does not render content when isOpen is false", () => {
    const { container } = renderWithProviders(
      <PersonaliseDashboardModal {...defaultProps} isOpen={false} />,
    );
    expect(container).toBeEmptyDOMElement();
  });

  it("renders modal with main and sidebar sections and widget names", () => {
    renderWithProviders(<PersonaliseDashboardModal {...defaultProps} />);

    expect(screen.getByText("personalise_dashboard")).toBeInTheDocument();
    expect(screen.getByText("main_content")).toBeInTheDocument();
    expect(screen.getByText("sidebar")).toBeInTheDocument();
    expect(screen.getByText("latest_announcements")).toBeInTheDocument();
    expect(screen.getByText("upcoming_activities")).toBeInTheDocument();
    expect(screen.getByText("my_enrollments")).toBeInTheDocument();
    expect(screen.getByText("my_groups")).toBeInTheDocument();
  });

  it("allows toggling a widget's visibility and saving", () => {
    renderWithProviders(<PersonaliseDashboardModal {...defaultProps} />);

    const announcementsCheckbox = screen.getByRole("checkbox", {
      name: /latest_announcements/i,
    });
    expect(announcementsCheckbox).toBeChecked();

    fireEvent.click(announcementsCheckbox);
    expect(announcementsCheckbox).not.toBeChecked();

    fireEvent.click(screen.getByRole("button", { name: "done" }));

    expect(mockOnSave).toHaveBeenCalledTimes(1);
    const savedWidgets: DashboardWidgetConfig[] = mockOnSave.mock.calls[0][0];
    const savedAnnouncements = savedWidgets.find(
      (w) => w.id === "announcements",
    );
    expect(savedAnnouncements?.visible).toBe(false);
    expect(mockOnClose).toHaveBeenCalledTimes(1);
  });

  it("allows moving a widget down in order", () => {
    renderWithProviders(<PersonaliseDashboardModal {...defaultProps} />);

    const moveDownButtons = screen.getAllByRole("button", {
      name: "move_down",
    });
    // First move down button is for announcements in main
    fireEvent.click(moveDownButtons[0]);

    fireEvent.click(screen.getByRole("button", { name: "done" }));

    const savedWidgets: DashboardWidgetConfig[] = mockOnSave.mock.calls[0][0];
    const announcements = savedWidgets.find((w) => w.id === "announcements");
    const upcoming = savedWidgets.find((w) => w.id === "upcoming_activities");

    expect(announcements?.order).toBe(1);
    expect(upcoming?.order).toBe(0);
  });

  it("allows switching a widget from main to sidebar", () => {
    renderWithProviders(<PersonaliseDashboardModal {...defaultProps} />);

    const moveToSidebarButtons = screen.getAllByRole("button", {
      name: "move_to_sidebar",
    });
    fireEvent.click(moveToSidebarButtons[0]); // Move announcements to sidebar

    fireEvent.click(screen.getByRole("button", { name: "done" }));

    const savedWidgets: DashboardWidgetConfig[] = mockOnSave.mock.calls[0][0];
    const announcements = savedWidgets.find((w) => w.id === "announcements");
    expect(announcements?.column).toBe("sidebar");
  });

  it("allows moving a widget up in order and moving from sidebar to main", () => {
    renderWithProviders(<PersonaliseDashboardModal {...defaultProps} />);

    // Move second main widget (upcoming_activities) up
    const moveUpButtons = screen.getAllByRole("button", { name: "move_up" });
    // Second move up button is for upcoming_activities
    fireEvent.click(moveUpButtons[1]);

    // Move first sidebar widget (my_enrollments) to main
    const moveToMainButtons = screen.getAllByRole("button", {
      name: "move_to_main",
    });
    fireEvent.click(moveToMainButtons[0]);

    fireEvent.click(screen.getByRole("button", { name: "done" }));

    const savedWidgets: DashboardWidgetConfig[] = mockOnSave.mock.calls[0][0];
    const myEnrollments = savedWidgets.find((w) => w.id === "my_enrollments");
    expect(myEnrollments?.column).toBe("main");
  });

  it("calls onReset when clicking reset to default", () => {
    renderWithProviders(<PersonaliseDashboardModal {...defaultProps} />);

    const resetBtn = screen.getByRole("button", { name: /reset_to_default/i });
    fireEvent.click(resetBtn);

    expect(mockOnReset).toHaveBeenCalledTimes(1);
    expect(mockOnClose).toHaveBeenCalledTimes(1);
  });
});
