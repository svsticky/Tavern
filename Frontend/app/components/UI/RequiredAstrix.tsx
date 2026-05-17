export default function RequiredAsterisk({ required }: { required: boolean }) {
  return required ? <span className="text-red-500 ml-1">*</span> : null;
}
