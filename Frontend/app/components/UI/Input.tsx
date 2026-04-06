export default function Input({ label, ...props }: any) {

  if(props?.type && props.type === "checkbox") {
    return (
      <div className={`flex items-center justify-start`}>
        <input type="checkbox" {...props} />
        {label && <span className="ml-2 text-sm">{label}</span>}
      </div>
    );
  }

  return (
    <label className="flex flex-col gap-1 w-full">
      <span className="text-sm font-medium text-gray-700">{label}</span>
      <input {...props} className="border p-2 rounded-lg focus:ring-2 outline-none transition-all" />
    </label>
  );
}