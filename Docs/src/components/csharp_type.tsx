// Simple C# type highlighter with custom colors
export const CSharpType = ({
    type,
    links,
}: {
    type: string;
    links: string[];
}) => {
    // Custom colors for syntax highlighting
    const colors = {
        primitive: "#3894c1", // Blue for primitives
        common: "#4dc6a7", // Green for common types
        custom: "#2E8B57", // Sea green for custom types
        punctuation: "#666666", // Gray for brackets, commas
    };

    // Function to colorize specific parts of a C# type
    const colorizeType = (text: string, links: string[]): React.ReactNode => {
        // Handle primitive types (blue)
        const primitives = [
            "string",
            "int",
            "bool",
            "float",
            "double",
            "decimal",
            "char",
            "byte",
            "object",
            "void",
            "long",
            "short",
            "uint",
            "ulong",
        ];

        // Common .NET types (green)
        const commonTypes = [
            "Guid",
            "DateTime",
            "TimeSpan",
            "Uri",
            "Exception",
            "Task",
            "List",
            "Dictionary",
            "IEnumerable",
            "ICollection",
            "HashSet",
            "Array",
        ];

        // Process generics like List<string>
        if (text.includes("<") && text.includes(">")) {
            const genericStart = text.indexOf("<");
            const genericEnd = text.lastIndexOf(">");

            const baseType = text.substring(0, genericStart);
            const genericContent = text.substring(genericStart + 1, genericEnd);
            const afterGeneric = text.substring(genericEnd + 1);

            return (
                <>
                    {links && links.length > 0 && links[0] ? (
                        <a
                            href={links[0]}
                            target={
                                links[0].startsWith("http") ? "_blank" : "_self"
                            }
                            className="text-inherit no-underline hover:text-inherit"
                        >
                            <span style={{ color: colors.common }}>
                                {baseType}
                            </span>
                        </a>
                    ) : (
                        <span style={{ color: colors.common }}>{baseType}</span>
                    )}
                    <span style={{ color: colors.punctuation }}>{"<"}</span>
                    {colorizeType(genericContent, links.slice(1))}
                    <span style={{ color: colors.punctuation }}>{">"}</span>
                    {afterGeneric &&
                        colorizeType(
                            afterGeneric,
                            links.slice(genericContent.split(",").length),
                        )}
                </>
            );
        }

        // Handle array types
        if (text.includes("[]")) {
            const arrayStart = text.indexOf("[]");
            const baseType = text.substring(0, arrayStart);
            const afterArray = text.substring(arrayStart + 2);

            return (
                <>
                    {colorizeType(baseType, links.splice(1))}
                    <span style={{ color: colors.punctuation }}>{"[]"}</span>
                    {afterArray && colorizeType(afterArray, links.slice(1))}
                </>
            );
        }

        // Handle comma-separated generic arguments
        if (text.includes(",")) {
            const parts = text.split(",");

            return (
                <>
                    {parts.map((part, index) => (
                        <span key={index}>
                            {colorizeType(part.trim(), [links[index]])}
                            {index < parts.length - 1 && (
                                <span style={{ color: colors.punctuation }}>
                                    ,{" "}
                                </span>
                            )}
                        </span>
                    ))}
                </>
            );
        }

        // Check for primitive types
        if (primitives.includes(text.toLowerCase())) {
            return links && links.length > 0 && links[0] ? (
                <a
                    href={links[0]}
                    target={links[0].startsWith("http") ? "_blank" : "_self"}
                    className="text-inherit no-underline hover:text-inherit"
                >
                    <span style={{ color: colors.primitive }}>{text}</span>
                </a>
            ) : (
                <span style={{ color: colors.primitive }}>{text}</span>
            );
        }

        // Check for common .NET types
        if (
            commonTypes.some((t) => text === t) ||
            text.toLowerCase().endsWith("exception") ||
            (links &&
                links.length > 0 &&
                links[0] &&
                links[0].includes("microsoft"))
        ) {
            return links && links.length > 0 && links[0] ? (
                <a
                    href={links[0]}
                    target={links[0].startsWith("http") ? "_blank" : "_self"}
                    className="text-inherit no-underline hover:text-inherit"
                >
                    <span style={{ color: colors.common }}>{text}</span>
                </a>
            ) : (
                <span style={{ color: colors.common }}>{text}</span>
            );
        }

        // Default case: probably a class or interface
        return links && links.length > 0 && links[0] ? (
            <a
                href={links[0]}
                target={links[0].startsWith("http") ? "_blank" : "_self"}
                className="text-inherit no-underline hover:text-inherit"
            >
                <span style={{ color: colors.custom }}>{text}</span>
            </a>
        ) : (
            <span style={{ color: colors.custom }}>{text}</span>
        );
    };

    return (
        <code className="rounded px-1.5 py-0.5 font-mono text-sm">
            {colorizeType(
                type.replace(/&lt;/g, "<").replace(/&gt;/g, ">"),
                links,
            )}
        </code>
    );
};
