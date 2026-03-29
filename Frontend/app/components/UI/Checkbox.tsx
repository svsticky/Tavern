export default function Checkbox({ label, ...props }: any) {
    return (
        <label className="flex items-center gap-2 cursor-pointer group">
            <input type="checkbox" {...props} className="w-5 h-5 accent-[var(--primary-board)] rounded border-gray-300" />
            <span className="text-sm font-medium text-gray-700 group-hover:text-black transition-colors">{label}</span>
        </label>
    );
}