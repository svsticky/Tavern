import { t } from "i18next";
import type { ReactNode } from "react";
import { useMemo, useState } from "react";

export interface Column<T> {
  header: React.ReactNode | string;
  render: (item: T) => ReactNode;
  className?: string;
  /** When provided, the column header becomes clickable and rows are sorted by this value. */
  sortValue?: (item: T) => string | number | null | undefined;
}

interface DataTableProps<T> {
  data: T[];
  columns: Column<T>[];
  emptyText?: string;
  onRowClick?: (item: T) => void;
  defaultSortColumn?: number;
  defaultSortDir?: "asc" | "desc";
}

export default function DataTableTile<T>({
  data,
  columns,
  emptyText,
  onRowClick,
  defaultSortColumn,
  defaultSortDir = "asc",
}: DataTableProps<T>) {
  const [sortCol, setSortCol] = useState<number | null>(
    defaultSortColumn ?? null,
  );
  const [sortDir, setSortDir] = useState<"asc" | "desc">(defaultSortDir);

  const sortedData = useMemo(() => {
    if (sortCol === null) return data;
    const col = columns[sortCol];
    if (!col?.sortValue) return data;
    return [...data].sort((a, b) => {
      const va = col.sortValue!(a) ?? "";
      const vb = col.sortValue!(b) ?? "";
      const cmp =
        typeof va === "number" && typeof vb === "number"
          ? va - vb
          : String(va).localeCompare(String(vb));
      return sortDir === "asc" ? cmp : -cmp;
    });
  }, [data, sortCol, sortDir, columns]);

  const handleHeaderClick = (i: number) => {
    if (!columns[i].sortValue) return;
    if (sortCol === i) {
      setSortDir((d) => (d === "asc" ? "desc" : "asc"));
    } else {
      setSortCol(i);
      setSortDir("asc");
    }
  };

  const mobileHeaderActions = columns.filter(
    (column) => typeof column.header !== "string" && column.header != null,
  );

  return (
    <div className="w-full overflow-x-auto">
      <table className="w-full min-w-full border-collapse block lg:table">
        <thead className="hidden lg:table-header-group">
          <tr className="border-b border-slate-100 text-slate-400 text-sm font-medium">
            {columns.map((col, i) => {
              const sortable = !!col.sortValue;
              const active = sortCol === i;
              return (
                <th
                  key={i}
                  onClick={() => handleHeaderClick(i)}
                  className={`py-4 px-4 text-left font-medium whitespace-nowrap ${col.className || ""} ${sortable ? "cursor-pointer select-none hover:text-slate-600" : ""}`}
                >
                  <span className="inline-flex items-center gap-1">
                    {col.header}
                    {sortable && (
                      <span className="text-xs">
                        {active ? (sortDir === "asc" ? "▲" : "▼") : "⇅"}
                      </span>
                    )}
                  </span>
                </th>
              );
            })}
          </tr>
        </thead>

        <tbody className="block lg:table-row-group">
          {sortedData.map((item, rowIndex) => (
            <tr
              key={rowIndex}
              onClick={() => onRowClick?.(item)}
              className={`
                block lg:table-row
                border-b border-slate-100 last:border-0
                transition-colors
                mb-4 lg:mb-0
                ${onRowClick ? "cursor-pointer hover:bg-slate-50" : ""}
              `}
            >
              {columns.map((col, colIndex) => (
                <td
                  key={colIndex}
                  data-label={typeof col.header === "string" ? col.header : ""}
                  className={`
                    px-4 py-2 lg:py-4
                    block lg:table-cell
                    text-left

                    ${typeof col.header === "string" && col.header.length > 0 ? "before:content-[attr(data-label)] before:block" : "before:hidden"}
                    lg:before:hidden
                    before:text-[10px] before:tracking-wider before:text-slate-400 before:font-bold

                    ${col.className || ""}
                    !w-full lg:!w-auto
                  `}
                >
                  <div
                    className={`text-slate-700 font-medium lg:font-normal lg:text-inherit whitespace-normal ${
                      colIndex === columns.length - 1
                        ? "w-full lg:w-auto [&_div]:w-full [&_button]:!w-full lg:[&_button]:!w-auto"
                        : ""
                    }`}
                  >
                    {col.render(item)}
                  </div>
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>

      {mobileHeaderActions.length > 0 && (
        <div className="lg:hidden mt-2 flex flex-col gap-2 w-full">
          {mobileHeaderActions.map((column, index) => (
            <div key={index} className="w-full [&>*]:w-full [&_button]:w-full">
              {column.header}
            </div>
          ))}
        </div>
      )}

      {sortedData.length === 0 && emptyText !== "" && (
        <div className="p-8 text-center text-slate-400">
          {emptyText == null ? t("no_data_found") : emptyText}
        </div>
      )}
    </div>
  );
}
