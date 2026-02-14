type MenuFooterProps = {
  children?: React.ReactNode;
};

export default function MenuFooter({ children }: MenuFooterProps) {
  return <div className="border-t border-white/20">{children}</div>;
}
