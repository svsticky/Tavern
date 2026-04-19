export const FormHeader = ({ title, border = true, children }: { title: string, border?: boolean, children?: React.ReactNode }) => (
  <div className={`flex items-end justify-between ${border ? "border-b mb-4" : "mb-2"}`}>
    <h3 className="font-bold uppercase text-xs text-gray-500 tracking-wider leading-none pb-1">
      {title}
    </h3>
    <div className="flex items-center gap-2 pb-1">
      {children}
    </div>
  </div>
);