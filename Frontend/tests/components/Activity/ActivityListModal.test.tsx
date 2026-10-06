import { fireEvent, render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router";
import { describe, expect, it, vi } from "vitest";
import ActivityListModal, {
  type ActivityListModalItem,
} from "~/components/Activity/ActivityListModal";
import { formatDate } from "~/util/date.util";

function renderModal(
  items: ActivityListModalItem[],
  onClose: () => void = vi.fn(),
) {
  return render(
    <MemoryRouter>
      <ActivityListModal
        isOpen
        onClose={onClose}
        title="Activities"
        items={items}
        emptyText="Nothing here"
      />
    </MemoryRouter>,
  );
}

describe("ActivityListModal", () => {
  it("renders nothing when closed", () => {
    render(
      <MemoryRouter>
        <ActivityListModal
          isOpen={false}
          onClose={vi.fn()}
          title="Activities"
          items={[]}
          emptyText="Nothing here"
        />
      </MemoryRouter>,
    );
    expect(screen.queryByText("Activities")).not.toBeInTheDocument();
  });

  it("shows the empty text when there are no items", () => {
    renderModal([]);
    expect(screen.getByText("Nothing here")).toBeInTheDocument();
  });

  it("sorts the items by start date, placing items without a date last", () => {
    renderModal([
      { name: "No date" },
      { name: "Later", dateTimeStart: "2026-03-01T10:00:00Z" },
      { name: "Earlier", dateTimeStart: "2026-01-01T10:00:00Z" },
    ]);

    const names = screen
      .getAllByRole("listitem")
      .map((item) => item.querySelector("span")?.textContent);
    expect(names).toEqual(["Earlier", "Later", "No date"]);
  });

  it("shows the date when no price is given", () => {
    renderModal([{ name: "Party", dateTimeStart: "2026-01-01T10:00:00Z" }]);
    expect(
      screen.getByText(
        formatDate(new Date("2026-01-01T10:00:00Z"), "dateOnly"),
      ),
    ).toBeInTheDocument();
  });

  it("shows the price instead of the date when one is given", () => {
    renderModal([
      { name: "Paid", dateTimeStart: "2026-01-01T10:00:00Z", price: 5 },
      { name: "Free", dateTimeStart: "2026-01-02T10:00:00Z", price: 0 },
    ]);
    expect(screen.getByText("€5.00")).toBeInTheDocument();
    expect(screen.getByText("free")).toBeInTheDocument();
    expect(
      screen.queryByText(
        formatDate(new Date("2026-01-01T10:00:00Z"), "dateOnly"),
      ),
    ).not.toBeInTheDocument();
  });

  it("links activities that have not ended yet and closes the modal on click", () => {
    const onClose = vi.fn();
    renderModal(
      [
        {
          id: 1,
          name: "Upcoming",
          dateTimeStart: "2099-01-01T10:00:00Z",
          dateTimeEnd: "2099-01-01T12:00:00Z",
        },
      ],
      onClose,
    );

    const link = screen.getByRole("link", { name: /Upcoming/ });
    expect(link).toHaveAttribute("href", "/activities/1");

    fireEvent.click(link);
    expect(onClose).toHaveBeenCalled();
  });

  it("does not link activities that have already ended", () => {
    renderModal([
      {
        id: 1,
        name: "Past",
        dateTimeStart: "2020-01-01T10:00:00Z",
        dateTimeEnd: "2020-01-01T12:00:00Z",
      },
    ]);
    expect(screen.getByText("Past")).toBeInTheDocument();
    expect(screen.queryByRole("link")).not.toBeInTheDocument();
  });
});
