type Option = {
  value: string | number;
  label: string;
};

interface SelectProps extends React.SelectHTMLAttributes<HTMLSelectElement> {
  label: string;
  options: Option[];
}

export default function Select({ label, options, ...props }: SelectProps) {
  return (
    <label className="flex flex-col gap-1 w-full">
      <span className="text-sm font-medium text-gray-700">{label}</span>
      <select 
        {...props} 
        className="border p-2.5 rounded-lg focus:ring-2 outline-none transition-all bg-white"
      >
        {options.map((opt) => (
          <option key={opt.value} value={opt.value}>
            {opt.label}
          </option>
        ))}
      </select>
    </label>
  );
}