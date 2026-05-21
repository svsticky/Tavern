// @ts-ignore
import { Prism as SyntaxHighlighter } from "react-syntax-highlighter";
// @ts-ignore
import { oneDark } from "react-syntax-highlighter/dist/cjs/styles/prism";

type Part =
    | { type: "text"; content: string }
    | { type: "link"; text: string; href: string };

function splitCodeWithLinks(code: string): Part[] {
    const regex = /\[([^\]]+)\]\(([^)]+)\)/g;
    const parts: Part[] = [];
    let lastIndex = 0;
    let match;

    while ((match = regex.exec(code)) !== null) {
        const index = match.index;
        if (index > lastIndex) {
            parts.push({ type: "text", content: code.slice(lastIndex, index) });
        }
        parts.push({ type: "link", text: match[1], href: match[2] });
        lastIndex = regex.lastIndex;
    }
    if (lastIndex < code.length) {
        parts.push({ type: "text", content: code.slice(lastIndex) });
    }
    return parts;
}

export function CodeBlock({
    language,
    children,
}: {
    language: string;
    children: string;
}) {
    const codeString =
        typeof children === "string" ? children : String(children);
    const parts = splitCodeWithLinks(codeString);

    return (
        <pre
            style={{
                backgroundColor: "#282c34",
                color: "white",
                padding: "1em",
                fontFamily: "monospace",
                whiteSpace: "pre-wrap",
                margin: 0,
                maxHeight: "400px",
                overflow: "auto",
                borderRadius: "6px",
            }}
        >
            {parts.map((part, i) => {
                if (part.type === "text") {
                    // Highlight the text part with the specified language
                    return (
                        <SyntaxHighlighter
                            key={i}
                            language={language}
                            style={oneDark}
                            PreTag="span"
                            CodeTag="span"
                            customStyle={{
                                display: "inline",
                                margin: 0,
                                padding: 0,
                                backgroundColor: "transparent",
                            }}
                            wrapLines={false}
                            showLineNumbers={false}
                        >
                            {part.content}
                        </SyntaxHighlighter>
                    );
                }
                if (part.type === "link") {
                    return (
                        <a
                            key={i}
                            href={part.href}
                            target="_blank"
                            rel="noopener noreferrer"
                            style={{
                                textDecoration: "underline",
                                backgroundColor: "transparent",
                            }}
                        >
                            <SyntaxHighlighter
                                language={language}
                                style={oneDark}
                                PreTag="span"
                                CodeTag="span"
                                customStyle={{
                                    display: "inline",
                                    margin: 0,
                                    padding: 0,
                                    backgroundColor: "transparent",
                                }}
                                wrapLines={false}
                                showLineNumbers={false}
                            >
                                {part.text}
                            </SyntaxHighlighter>
                        </a>
                    );
                }
                return null;
            })}
        </pre>
    );
}
