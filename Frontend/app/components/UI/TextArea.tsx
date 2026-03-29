export default function TextArea({ label, ...props }: any) {
  return (
    <label className="flex flex-col gap-1 w-full">
      <span className="text-sm font-medium text-gray-700">{label}</span>
      <textarea {...props} className="border p-2 rounded-lg focus:ring-2 outline-none transition-all" />
    </label>
  );
}