import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";

interface MarkdownProps {
  children: string;
}

export default function Markdown({ children }: MarkdownProps) {
  return (
    <ReactMarkdown
      remarkPlugins={[remarkGfm]}
      components={{
        a: ({ node, ...props }) => (
          <a
            className="text-(--board-primary) hover:text-(--board-primary-dark) font-semibold underline underline-offset-4"
            target="_blank"
            rel="noreferrer"
            {...props}
          />
        ),
        h1: ({ node, ...props }) => <h1 className="text-2xl font-bold mb-4" {...props} />,
        ul: ({ node, ...props }) => <ul className="list-disc ml-6 mb-4" {...props} />,
      }}
    >
      {children}
    </ReactMarkdown>
  );
}