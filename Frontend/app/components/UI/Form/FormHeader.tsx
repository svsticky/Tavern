export const FormHeader = ({ title, border = true }: { title: string, border?: boolean }) => (
  <div className={border ? "border-b pb-2 mb-4" : "mb-2"}>
    <h3 className="font-bold uppercase text-xs text-gray-500 tracking-wider">
      {title}
    </h3>
  </div>
);