import { t } from "i18next";
import type { ReactNode } from "react";

export interface Column<T> {
  header: React.ReactNode | string;
  render: (item: T) => ReactNode;
  className?: string;
}

interface DataTableProps<T> {
  data: T[];
  columns: Column<T>[];
  emptyText?: string;
  onRowClick?: (item: T) => void;
}

export default function DataTableTile<T>({ data, columns, emptyText, onRowClick }: DataTableProps<T>) {
  return (
    <div className="w-full overflow-x-auto">
      <table className="w-full min-w-full border-collapse block lg:table">
        
        <thead className="hidden lg:table-header-group">
          <tr className="border-b border-slate-100 text-slate-400 text-sm font-medium">
            {columns.map((col, i) => (
              <th 
                key={i} 
                className={`py-4 px-4 text-left font-medium whitespace-nowrap ${col.className || ""}`}
              >
                {col.header}
              </th>
            ))}
          </tr>
        </thead>

        <tbody className="block lg:table-row-group">
          {data.map((item, rowIndex) => (
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
                  data-label={col.header}
                  className={`
                    px-4 py-2 lg:py-4 
                    block lg:table-cell
                    text-left
                    
                    ${col.header ? "before:content-[attr(data-label)] before:block" : "before:hidden"}
                    lg:before:hidden 
                    before:text-[10px] before:tracking-wider before:text-slate-400 before:font-bold
                    
                    ${col.className || ""}
                  `}
                >
                  <div className="text-slate-700 font-medium lg:font-normal lg:text-inherit whitespace-normal">
                    {col.render(item)}
                  </div>
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
      
      {data.length === 0 && emptyText !== "" && (
        <div className="p-8 text-center text-slate-400">
          {emptyText == null ? t("no_data_found") : emptyText}
        </div>
      )}
    </div>
  );
}