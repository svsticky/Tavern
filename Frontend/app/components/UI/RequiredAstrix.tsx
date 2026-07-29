export default function RequiredAsterisk({ required }: { required: boolean }) {
  if (!required) return null;
  return <span className="text-red-500 ml-0.5 text-sm inline-block">*</span>;
}
