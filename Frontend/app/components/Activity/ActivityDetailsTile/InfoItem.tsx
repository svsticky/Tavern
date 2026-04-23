export default function InfoItem({ icon, label, value }: { icon: React.ReactNode, label: string, value: string }) {
  return (
    <div className="flex items-start gap-3">
      <div className="mt-1 p-2 bg-slate-50 rounded-lg text-slate-400 font-bold">{icon}</div>
      <div>
        <p className="text-[10px] uppercase font-bold text-slate-400 tracking-wider leading-none mb-1">{label}</p>
        <p className="text-slate-700 font-semibold leading-tight">{value}</p>
      </div>
    </div>
  );
}