import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import DataTableTile, { type Column } from "~/components/Tiles/DataTableTile";

type Row = { id: number; name: string };

const rows: Row[] = [
  { id: 1, name: "Alice" },
  { id: 2, name: "Bob" },
];

const columns: Column<Row>[] = [{ header: "Name", render: (row) => row.name }];

describe("DataTableTile", () => {
  it("renders a header and a row per data item", () => {
    render(<DataTableTile data={rows} columns={columns} />);

    expect(screen.getByText("Name")).toBeInTheDocument();
    expect(screen.getByText("Alice")).toBeInTheDocument();
    expect(screen.getByText("Bob")).toBeInTheDocument();
  });

  it("calls onRowClick with the row's data when a row is clicked", async () => {
    const user = userEvent.setup();
    const onRowClick = vi.fn();
    render(
      <DataTableTile data={rows} columns={columns} onRowClick={onRowClick} />,
    );

    await user.click(screen.getByText("Alice"));

    expect(onRowClick).toHaveBeenCalledWith(rows[0]);
  });

  it("shows the default empty message when there is no data", () => {
    render(<DataTableTile data={[]} columns={columns} />);
    expect(screen.getByText("no_data_found")).toBeInTheDocument();
  });

  it("shows a custom empty message when provided", () => {
    render(
      <DataTableTile data={[]} columns={columns} emptyText="Nothing here" />,
    );
    expect(screen.getByText("Nothing here")).toBeInTheDocument();
  });

  it("suppresses the empty message when emptyText is an empty string", () => {
    render(<DataTableTile data={[]} columns={columns} emptyText="" />);
    expect(screen.queryByText("no_data_found")).not.toBeInTheDocument();
  });

  const actionColumns: Column<Row>[] = [
    ...columns,
    { header: <button type="button">add</button>, render: () => null },
  ];

  it("renders the mobile action header wrapper only once, below the table by default", () => {
    const { container } = render(
      <DataTableTile data={rows} columns={actionColumns} />,
    );

    // Present once in the desktop <thead> (CSS-hidden on mobile, but still in
    // the DOM) and once in the dedicated mobile-only block - that block must
    // not be duplicated.
    expect(screen.getAllByText("add")).toHaveLength(2);
    const mobileBlocks = container.querySelectorAll(".lg\\:hidden");
    expect(mobileBlocks).toHaveLength(1);

    const table = container.querySelector("table")!;
    expect(
      table.compareDocumentPosition(mobileBlocks[0]) &
        Node.DOCUMENT_POSITION_FOLLOWING,
    ).toBeTruthy();
  });

  it("renders the mobile action header wrapper above the table when mobileActionsPosition is 'top'", () => {
    const { container } = render(
      <DataTableTile
        data={rows}
        columns={actionColumns}
        mobileActionsPosition="top"
      />,
    );

    expect(screen.getAllByText("add")).toHaveLength(2);
    const mobileBlocks = container.querySelectorAll(".lg\\:hidden");
    expect(mobileBlocks).toHaveLength(1);

    const table = container.querySelector("table")!;
    expect(
      table.compareDocumentPosition(mobileBlocks[0]) &
        Node.DOCUMENT_POSITION_PRECEDING,
    ).toBeTruthy();
  });
});
