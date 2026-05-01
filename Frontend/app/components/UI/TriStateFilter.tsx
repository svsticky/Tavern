import { t } from "i18next";
import Tile from "../Tiles/Tile";

/**
 * A specialized filter component that allows users to toggle between three states: Yes, No, and All.
 * 
 * This component is ideal for boolean filters that also require a "neutral" or "ignored" state 
 * (represented by `null`). It renders as a segmented control (button group) within a small Tile.
 * 
 * @component
 * @param {Object} props - The component properties.
 * @param {string} props.label - The text label identifying what is being filtered.
 * @param {boolean | null} props.value - The current state: `true` (Yes), `false` (No), or `null` (All).
 * @param {(val: boolean | null) => void} props.onChange - Callback function triggered when a new state is selected.
 */
export default function TriStateFilter({ 
  label, 
  value, 
  onChange 
}: { 
  label: string; 
  value: boolean | null; 
  onChange: (val: boolean | null) => void 
}){
    return (
    <Tile className="flex items-center justify-between p-2 bg-slate-50">
        <span className="text-sm font-medium text-slate-700">{label}</span>
        <div className="flex gap-1 bg-slate-200 p-1 rounded-md">
        <button
            onClick={() => onChange(true)}
            className={`px-3 py-1 text-xs rounded cursor-pointer ${value === true ? 'bg-white shadow-sm font-bold' : 'text-slate-500'}`}
        >
            {t("yes")}
        </button>
        <button
            onClick={() => onChange(false)}
            className={`px-3 py-1 text-xs rounded cursor-pointer ${value === false ? 'bg-white shadow-sm font-bold' : 'text-slate-500'}`}
        >
            {t("no")}
        </button>
        <button
            onClick={() => onChange(null)}
            className={`px-3 py-1 text-xs rounded cursor-pointer ${value === null ? 'bg-white shadow-sm font-bold' : 'text-slate-500'}`}
        >
            {t("all")}
        </button>
        </div>
    </Tile>
    );
}