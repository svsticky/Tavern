import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";

/**
 * Props for the Markdown component.
 * @interface MarkdownProps
 * @property {string} children - The raw markdown string to be parsed and rendered.
 */
interface MarkdownProps {
  children: string;
}

/**
 * A stylized wrapper for rendering Markdown content with GitHub Flavored Markdown (GFM) support.
 * 
 * This component uses `react-markdown` to transform markdown strings into safe HTML. 
 * It includes custom component mapping to ensure that rendered elements (like links, 
 * headers, and lists) align with the application's design system and Tailwind configuration.
 * 
 * Key Features:
 * - **GFM Support**: Enables tables, task lists, and strikethroughs via `remark-gfm`.
 * - **Safe Links**: Automatically adds `target="_blank"` and `rel="noreferrer"` to all anchors.
 * - **Typography**: Applies consistent margins and colors to headings and lists.
 * 
 * @component
 * @param {MarkdownProps} props - The component properties.
 */
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