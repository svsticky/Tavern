import * as fs from "fs";
import { dirname, join, relative } from "path";
import { generateIndexFiles } from "./generate-index-files.mjs";

// Set input and output directories
const inputDir = "../docfx-files";
const outputDir = "../content/Docs/Backend";

// Makes sure the directory exists
function ensureDirSync(dirPath) {
    fs.mkdirSync(dirPath, { recursive: true });
}

function extractTypeWithLinks(line) {
    // Set patterns for types with links
    const markdownPattern = /\[([^\]]+)\]\(([^)]+)\)/g;
    const htmlPattern = /<a\s+href=['"]([^'"]+)['"]>([^<]+)<\/a>/g;

    // Clean line
    line = line.replace(/\\/g, "");

    const links = [];
    const typeTexts = [];

    // Check if line does not match, return null
    if (!line.match(markdownPattern) && !line.match(htmlPattern)) return null;

    // Process Markdown pattern
    if (line.match(markdownPattern)) {
        // Reset pattern before using exec in a loop
        markdownPattern.lastIndex = 0;

        // Collect all links and type names from markdown
        let match;
        while ((match = markdownPattern.exec(line)) !== null) {
            typeTexts.push(match[1]);
            links.push(match[2]);
        }
    }

    // Process HTML pattern
    if (line.match(htmlPattern)) {
        // Reset pattern before using exec in a loop
        htmlPattern.lastIndex = 0;

        // Collect all links and type names from HTML
        let match;
        while ((match = htmlPattern.exec(line)) !== null) {
            typeTexts.push(match[2]); // In HTML pattern, group 2 contains the text
            links.push(match[1]); // In HTML pattern, group 1 contains the link
        }
    }

    // Parse the full type string (handle generic structure with < and >)
    let fullType = line;

    // Clean up escaped characters like \< and \> to get the proper type name
    fullType = fullType
        .replace(/\\\\/g, "\\")
        .replace(/\\</g, "<")
        .replace(/\\>/g, ">");

    // Check if we have a generic type (either by escaped < or regular <)
    const isGenericType =
        line.includes("\\<") ||
        line.includes("<") ||
        (typeTexts.length > 1 && fullType.includes(">"));

    if (isGenericType && typeTexts.length > 1) {
        // We have a generic type like Task<BLOB_STATUSCODE>
        return {
            name: "",
            type: `${typeTexts[0]}<${typeTexts.slice(1).join(", ")}>${line.includes(">[]") ? "[]" : ""}`, // Full type name
            links: links, // All links in order
            description: "",
        };
    } else {
        // Regular non-generic type
        return {
            name: "",
            type: typeTexts[0],
            links: links,
            description: "",
        };
    }
}

function escapeHTMLExceptCodeBlocks(content) {
    const codeBlockRegex = /```[\s\S]*?```/g;

    // Split the content by code blocks
    let parts = [];
    let lastIndex = 0;
    let match;

    while ((match = codeBlockRegex.exec(content)) !== null) {
        // Add text before the code block (with < escaped)
        if (match.index > lastIndex) {
            let textPart = content.substring(lastIndex, match.index);
            textPart = textPart
                .replace(/</g, "\\<")
                .replace(/<([^>]+)>/g, (_, inner) => `&lt;${inner}&gt;`);
            parts.push(textPart);
        }

        // Add the code block (unchanged)
        parts.push(match[0]);

        lastIndex = match.index + match[0].length;
    }

    // Add any remaining text after the last code block (with < escaped)
    if (lastIndex < content.length) {
        let textPart = content.substring(lastIndex);
        textPart = textPart
            .replace(/</g, "\\<")
            .replace(/<([^>]+)>/g, (_, inner) => `&lt;${inner}&gt;`);
        parts.push(textPart);
    }

    return parts.join("");
}

// -----------------
// MAIN LOGIC:
// -----------------

// Remove current output dir
try {
    fs.rmSync(outputDir, { recursive: true, force: true });
} catch {
    /* Ignore error */
}

const files = fs.readdirSync(inputDir);

// Determine which parts become folders so we can generate index files
let folders = [];
files.forEach((file) => {
    file.split(".")
        .slice(0, -2)
        .forEach((item) => {
            if (!folders.includes(item)) folders.push(item);
        });
});

let fileLocations = {};

// Place the files at the correct location
files.forEach((file) => {
    // Skip non-markdown files
    if (!file.endsWith(".md")) return;

    // Copy file to new location (using folder structure inferred from file name)
    // If a file has the same name as a folder, move it into the folder and rename it index.mdx
    const parts = file.split(".").slice(0, -1);
    const className = parts.join(".");

    const end = folders.includes(parts.at(-1)) ? "/index.mdx" : ".mdx";
    const outputLocation = join(outputDir, parts.join("/").concat(end));
    ensureDirSync(dirname(outputLocation));
    fs.copyFileSync(join(inputDir, file), outputLocation);

    fileLocations[className] = outputLocation;
});

// Clean up the content of the files and make them compatible with fumadocs
Object.entries(fileLocations).forEach(([className, file]) => {
    // Extract content
    let content = fs.readFileSync(file, "utf-8");

    // Add frontmatter heading (required for fumadocs)
    const cleanName = className.split(".").at(-1).replace(/-\d+$/, "");
    const frontmatter = `---\ntitle: ${cleanName}\n---\n`;
    // Add imports
    const imports = [
        `import { CollapsibleInherited } from "@/components/collapsible"`,
        `import { TypeTable } from "@/components/type_table"`,
        `import { CSharpType } from "@/components/csharp_type"`,
    ];

    // Remove all HTML anchors but keep inner text
    content = content.replace(/<a\b[^>]*>(.*?)<\/a>/g, "$1");

    // Remove the big heading (single #)
    content = content.replace(/^#(?![#]).*$/m, "");

    // Escape all other < characters outside code blocks
    content = escapeHTMLExceptCodeBlocks(content);

    // Update links to point to the new location
    content = content.replace(/\[(.*?)\]\((.*?)\)/g, (match, p1, p2) => {
        const _className = p2.slice(0, -3).replace(/\\/g, "");

        if (!(_className in fileLocations)) return match;

        let relativePath = relative(file, fileLocations[_className])
            .replace(/\\/g, "/")
            .replace("../", "")
            .replace("index", "../" + _className.split(".").at(-1))
            .replace(".mdx", "");

        if (file.includes("index"))
            relativePath = className.split(".").at(-1) + "/" + relativePath;

        return `<a href='./${relativePath}'>${p1.replace(/\\/g, "")}</a>`;
    });

    // Process the content to place it in nice components
    const lines = content.split("\n");
    const inheritedList = [];
    const namespacesList = [];
    const classesList = [];
    const outputLines = [];
    const parametersList = [];
    const exceptionsList = [];
    const returnsList = [];
    const structsList = [];
    const interfacesList = [];
    const enumsList = [];

    let foundNamespaces = false;
    let foundClasses = false;
    let foundInherited = false;
    let foundParameters = false;
    let foundExceptions = false;
    let foundReturns = false;
    let foundStructs = false;
    let foundInterfaces = false;
    let foundEnums = false;

    let foundClassesCount = 0;
    let foundParameterCount = 0;
    let foundExceptionCount = 0;
    let foundReturnCount = 0;
    let foundStructsCount = 0;
    let foundInterfacesCount = 0;
    let foundEnumsCount = 0;

    lines.forEach((line) => {
        // Find the namespaces section and make it into a table
        if (line.match(/^#{2,4}\s+Namespaces/)) {
            // Found the Namespaces section
            foundNamespaces = true;
            namespacesList.length = 0;
            outputLines.push(`<h4 className='mb-0 ml-1'>Namespaces</h4>`);
            return;
        }

        if (foundNamespaces) {
            // Found the namespaces section
            if (line.includes("</a>")) {
                // If we have a link, push it to the list, do nothing with it yet
                namespacesList.push(line);
                return;
            }
            // Only do something if there is something in the list
            else if (namespacesList.length > 0) {
                // If line is empty, ignore
                if (line.trim() === "") return;

                // Add namespaces links
                namespacesList.forEach((link) =>
                    outputLines.push(`- ${link.replace(/[\r\n]+/g, "")}`),
                );

                // Clear the list for the next iteration
                namespacesList.length = 0;
                foundNamespaces = false;
            }
        }

        // Find the classes section and make it into a table
        if (line.match(/^#{2,4}\s+Classes/)) {
            // Found the Classes section
            foundClasses = true;
            foundClassesCount = 0;
            classesList.length = 0;
            outputLines.push(`<h4 className='mb-0 ml-1'>Classes</h4>`);
            return;
        }

        // If we found the Classes section, add the members to the list
        if (foundClasses) {
            foundClassesCount++;

            // Ignore empty lines
            if (line.trim() === "") return;

            // Set class pattern to match the class format
            const pattern = /<a href=['"]([^'"]*)['"][^>]*>([^<]*)<\/a>/;

            // Check if line contains a class
            if (line.match(pattern)) {
                const typeWithLinks = extractTypeWithLinks(line);

                classesList.push(typeWithLinks);

                foundClassesCount = 0;

                return;
            }
            // We did not match but was still classesFound = true, so we check for description line
            // If counter is 2 and the line is not empty or starting with #, then we have a description line
            // If it is more the class did not have a description
            else if (classesList.length > 0) {
                // If we have a description line, add it to the last class
                if (foundClassesCount == 2 && !line.startsWith("#")) {
                    classesList[classesList.length - 1].description =
                        line.trim();
                    return;
                }
                // If there is no description, add the table to the output
                else if (foundClassesCount > 2 || line.startsWith("#")) {
                    // There was a line directly behind the description line, so this is probably the second part of a description
                    if (foundClassesCount == 3 && !line.startsWith("#")) {
                        // Add this line to the description, lower the foundClassesCount by one (in case there are more lines in the description), and return
                        classesList[classesList.length - 1].description =
                            classesList[classesList.length - 1].description +
                            " " +
                            line.trim();
                        foundClassesCount--;
                        return;
                    }

                    // Generate the type table and reset
                    outputLines.push(
                        `<TypeTable types={${JSON.stringify(classesList)}} />\n`,
                    );
                    classesList.length = 0;
                    foundClasses = false;
                }
            }
        }

        // Check for the start of the Inherited Members section
        if (line.match(/^#{2,4}\s+Inherited Members/)) {
            // Found the Inherited Members section
            foundInherited = true;
            outputLines.push(`\n`);
            return;
        }

        // If we found the Inherited Members section, add the members to the list
        if (foundInherited) {
            // If the start was found, add to memberslist instead
            if (line.trim() !== "") {
                // Add if line is not empty
                inheritedList.push(line);
                return;
            }

            // If no members, skip empty line
            if (inheritedList.length === 0) return;

            // If line is empty, construct the collapsible
            outputLines.push(
                `<CollapsibleInherited title='Show Inherited Members (${inheritedList.length})'>`,
            );
            inheritedList.forEach((member) => {
                // Add each member to the accordeon
                outputLines.push(`${member}`);
            });
            outputLines.push(`</CollapsibleInherited>\n`);
            inheritedList.length = 0;
            foundInherited = false;

            return;
        }

        // Find the parameter section and make it into a table
        if (line.match(/^#{2,4}\s+Parameters/)) {
            // Found the Parameters section
            foundParameters = true;
            foundParameterCount = 0;
            parametersList.length = 0;
            outputLines.push(`<h4 className='mb-0 ml-1'>Parameters</h4>`);
            return;
        }

        // If we found the Parameters section, add the members to the list
        if (foundParameters) {
            foundParameterCount++;

            // Ignore empty lines
            if (line.trim() === "") return;

            // Set parameter pattern to match the parameter format
            const markdownPattern = /`([^`]+)`\s+\[([^\]]+)\]\(([^)]+)\)/g;
            const htmlPattern =
                /`([^`]+)`\s+<a\s+href=['"]([^'"]+)['"]>([^<]+)<\/a>/g;

            // Check if line contains a parameter
            if (line.match(markdownPattern) || line.match(htmlPattern)) {
                const match = line.match(markdownPattern)
                    ? markdownPattern.exec(line)
                    : htmlPattern.exec(line);

                const typeWithLinks = extractTypeWithLinks(line);
                typeWithLinks.name = match[1];

                parametersList.push(typeWithLinks);

                foundParameterCount = 0;

                return;
            }
            // We did not match but was still parameterFound = true, so we check for description line
            // If counter is 2 and the line is not empty or starting with #, then we have a description line
            // If it is more the parameter did not have a description
            else if (parametersList.length > 0) {
                // If we have a description line, add it to the last parameter
                if (foundParameterCount == 2 && !line.startsWith("#")) {
                    parametersList[parametersList.length - 1].description =
                        line.trim();
                    return;
                }
                // If there is no description, add the table to the output
                else if (foundParameterCount > 2 || line.startsWith("#")) {
                    outputLines.push(
                        `<TypeTable types={${JSON.stringify(parametersList)}} />\n`,
                    );
                    parametersList.length = 0;
                    foundParameters = false;
                }
            }
        }

        // Find the exception section and make it into a table
        if (line.match(/^#{2,4}\s+Exceptions/)) {
            // Found the Exceptions section
            foundExceptions = true;
            foundExceptionCount = 0;
            exceptionsList.length = 0;
            outputLines.push(`<h4 className='mb-0 ml-1'>Exceptions</h4>`);
            return;
        }

        // If we found the Exceptions section, add the members to the list
        if (foundExceptions) {
            foundExceptionCount++;

            // Ignore empty lines
            if (line.trim() === "") return;

            const typeWithLinks = extractTypeWithLinks(line);

            // Check if line contains a exception
            if (typeWithLinks) {
                exceptionsList.push(typeWithLinks);

                foundExceptionCount = 0;

                return;
            }
            // We did not match but was still exceptionFound = true, so we check for description line
            // If counter is 2 and the line is not empty or starting with #, then we have a description line
            // If it is more the exception did not have a description
            else if (exceptionsList.length > 0) {
                // If we have a description line, add it to the last exception
                if (foundExceptionCount == 2 && !line.startsWith("#")) {
                    exceptionsList[exceptionsList.length - 1].description =
                        line.trim();
                    return;
                }
                // If there is no description, add the table to the output
                else if (foundExceptionCount > 2 || line.startsWith("#")) {
                    outputLines.push(
                        `<TypeTable types={${JSON.stringify(exceptionsList)}} />\n`,
                    );
                    exceptionsList.length = 0;
                    foundExceptions = false;
                }
            }
        }

        // Find the return section and make it into a table
        if (line.match(/^#{2,4}\s+Returns/)) {
            // Found the returns section
            foundReturns = true;
            foundReturnCount = 0;
            returnsList.length = 0;
            outputLines.push(`<h4 className='mb-0 ml-1'>Returns</h4>`);
            return;
        }

        // If we found the returns section, add the members to the list
        if (foundReturns) {
            foundReturnCount++;

            // Ignore empty lines
            if (line.trim() === "") return;

            const typeWithLinks = extractTypeWithLinks(line);

            // Check if line contains a return
            if (typeWithLinks) {
                returnsList.push(typeWithLinks);

                foundReturnCount = 0;

                return;
            }
            // We did not match but was still returnFound = true, so we check for description line
            // If counter is 2 and the line is not empty or starting with #, then we have a description line
            // If it is more the return did not have a description
            else if (returnsList.length > 0) {
                // If we have a description line, add it to the last return
                if (foundReturnCount == 2 && !line.startsWith("#")) {
                    returnsList[returnsList.length - 1].description =
                        line.trim();
                    return;
                }
                // If there is no description, add the table to the output
                else if (foundReturnCount > 2 || line.startsWith("#")) {
                    outputLines.push(
                        `<TypeTable types={${JSON.stringify(returnsList)}} />\n`,
                    );
                    returnsList.length = 0;
                    foundReturns = false;
                }
            }
        }

        // Find the structs section and make it into a table
        if (line.match(/^#{2,4}\s+Structs/)) {
            // Found the Structs section
            foundStructs = true;
            foundStructsCount = 0;
            structsList.length = 0;
            outputLines.push(`<h4 className='mb-0 ml-1'>Structs</h4>`);
            return;
        }

        // If we found the Structs section, add the members to the list
        if (foundStructs) {
            foundStructsCount++;

            // Ignore empty lines
            if (line.trim() === "") return;

            // Set class pattern to match the class format
            const pattern = /<a href=['"]([^'"]*)['"][^>]*>([^<]*)<\/a>/;

            // Check if line contains a class
            if (line.match(pattern)) {
                const typeWithLinks = extractTypeWithLinks(line);

                structsList.push(typeWithLinks);

                foundStructsCount = 0;

                return;
            }
            // We did not match but was still StructsFound = true, so we check for description line
            // If counter is 2 and the line is not empty or starting with #, then we have a description line
            // If it is more the class did not have a description
            else if (structsList.length > 0) {
                // If we have a description line, add it to the last class
                if (foundStructsCount == 2 && !line.startsWith("#")) {
                    structsList[structsList.length - 1].description =
                        line.trim();
                    return;
                }
                // If there is no description, add the table to the output
                else if (foundStructsCount > 2 || line.startsWith("#")) {
                    // There was a line directly behind the description line, so this is probably the second part of a description
                    if (foundStructsCount == 3 && !line.startsWith("#")) {
                        // Add this line to the description, lower the foundStructsCount by one (in case there are more lines in the description), and return
                        structsList[structsList.length - 1].description =
                            structsList[structsList.length - 1].description +
                            " " +
                            line.trim();
                        foundStructsCount--;
                        return;
                    }

                    // Generate the type table and reset
                    outputLines.push(
                        `<TypeTable types={${JSON.stringify(structsList)}} />\n`,
                    );
                    structsList.length = 0;
                    foundStructs = false;
                }
            }
        }

        // Find the Interfaces section and make it into a table
        if (line.match(/^#{2,4}\s+Interfaces/)) {
            // Found the Interfaces section
            foundInterfaces = true;
            foundInterfacesCount = 0;
            interfacesList.length = 0;
            outputLines.push(`<h4 className='mb-0 ml-1'>Interfaces</h4>`);
            return;
        }

        // If we found the Interfaces section, add the members to the list
        if (foundInterfaces) {
            foundInterfacesCount++;

            // Ignore empty lines
            if (line.trim() === "") return;

            // Set class pattern to match the class format
            const pattern = /<a href=['"]([^'"]*)['"][^>]*>([^<]*)<\/a>/;

            // Check if line contains a class
            if (line.match(pattern)) {
                const typeWithLinks = extractTypeWithLinks(line);

                interfacesList.push(typeWithLinks);

                foundInterfacesCount = 0;

                return;
            }
            // We did not match but was still InterfacesFound = true, so we check for description line
            // If counter is 2 and the line is not empty or starting with #, then we have a description line
            // If it is more the class did not have a description
            else if (interfacesList.length > 0) {
                // If we have a description line, add it to the last class
                if (foundInterfacesCount == 2 && !line.startsWith("#")) {
                    interfacesList[interfacesList.length - 1].description =
                        line.trim();
                    return;
                }
                // If there is no description, add the table to the output
                else if (foundInterfacesCount > 2 || line.startsWith("#")) {
                    // There was a line directly behind the description line, so this is probably the second part of a description
                    if (foundInterfacesCount == 3 && !line.startsWith("#")) {
                        // Add this line to the description, lower the foundInterfacesCount by one (in case there are more lines in the description), and return
                        interfacesList[interfacesList.length - 1].description =
                            interfacesList[interfacesList.length - 1]
                                .description +
                            " " +
                            line.trim();
                        foundInterfacesCount--;
                        return;
                    }

                    // Generate the type table and reset
                    outputLines.push(
                        `<TypeTable types={${JSON.stringify(interfacesList)}} />\n`,
                    );
                    interfacesList.length = 0;
                    foundInterfaces = false;
                }
            }
        }

        // Find the Enums section and make it into a table
        if (line.match(/^#{2,4}\s+Enums/)) {
            // Found the Enums section
            foundEnums = true;
            foundEnumsCount = 0;
            enumsList.length = 0;
            outputLines.push(`<h4 className='mb-0 ml-1'>Enums</h4>`);
            return;
        }

        // If we found the Enums section, add the members to the list
        if (foundEnums) {
            foundEnumsCount++;

            // Ignore empty lines
            if (line.trim() === "") return;

            // Set class pattern to match the class format
            const pattern = /<a href=['"]([^'"]*)['"][^>]*>([^<]*)<\/a>/;

            // Check if line contains a class
            if (line.match(pattern)) {
                const typeWithLinks = extractTypeWithLinks(line);

                enumsList.push(typeWithLinks);

                foundEnumsCount = 0;

                return;
            }
            // We did not match but was still EnumsFound = true, so we check for description line
            // If counter is 2 and the line is not empty or starting with #, then we have a description line
            // If it is more the class did not have a description
            else if (enumsList.length > 0) {
                // If we have a description line, add it to the last class
                if (foundEnumsCount == 2 && !line.startsWith("#")) {
                    enumsList[enumsList.length - 1].description = line.trim();
                    return;
                }
                // If there is no description, add the table to the output
                else if (foundEnumsCount > 2 || line.startsWith("#")) {
                    // There was a line directly behind the description line, so this is probably the second part of a description
                    if (foundEnumsCount == 3 && !line.startsWith("#")) {
                        // Add this line to the description, lower the foundEnumsCount by one (in case there are more lines in the description), and return
                        enumsList[enumsList.length - 1].description =
                            enumsList[enumsList.length - 1].description +
                            " " +
                            line.trim();
                        foundEnumsCount--;
                        return;
                    }

                    // Generate the type table and reset
                    outputLines.push(
                        `<TypeTable types={${JSON.stringify(enumsList)}} />\n`,
                    );
                    enumsList.length = 0;
                    foundEnums = false;
                }
            }
        }

        // Normal line, add to output
        outputLines.push(line);
    });

    // Make sure all lists are handled
    if (classesList.length > 0)
        outputLines.push(
            `<TypeTable types={${JSON.stringify(classesList)}} />\n`,
        );

    if (namespacesList.length > 0) {
        namespacesList.forEach((link) =>
            outputLines.push(`- ${link.replace(/[\r\n]+/g, "")}`),
        );
    }

    if (inheritedList.length > 0) {
        outputLines.push(
            `<CollapsibleInherited title='Show Inherited Members (${inheritedList.length})'>`,
        );
        inheritedList.forEach((member) => {
            // Add each member to the accordeon
            outputLines.push(`${member}`);
        });
        outputLines.push(`</CollapsibleInherited>\n`);
    }

    if (parametersList.length > 0)
        outputLines.push(
            `<TypeTable types={${JSON.stringify(parametersList)}} />\n`,
        );

    if (exceptionsList.length > 0)
        outputLines.push(
            `<TypeTable types={${JSON.stringify(exceptionsList)}} />\n`,
        );

    if (returnsList.length > 0)
        outputLines.push(
            `<TypeTable types={${JSON.stringify(returnsList)}} />\n`,
        );

    if (structsList.length > 0)
        outputLines.push(
            `<TypeTable types={${JSON.stringify(structsList)}} />\n`,
        );

    if (interfacesList.length > 0)
        outputLines.push(
            `<TypeTable types={${JSON.stringify(interfacesList)}} />\n`,
        );

    if (enumsList.length > 0)
        outputLines.push(
            `<TypeTable types={${JSON.stringify(enumsList)}} />\n`,
        );

    content = outputLines.join("\n");

    // Add divider lines between all headings
    const divider = "---\n";
    let isFirstHeading = true;

    content = content.replace(/^(#{1,6}\s+.+)$/gm, (match) => {
        // If it is the first heading, don't put a divider above
        if (isFirstHeading) {
            isFirstHeading = false;
            return match;
        }

        // Put divider above heading
        return `${divider}${match}`;
    });

    // Color all the markdown links containing microsoft (these are types)
    content = content.replace(
        /\[(\w+)\]\((https?:\/\/.*?microsoft\.com.*?)\)/g,
        (_, typeName, url) => {
            return `<a href='${url}' target='_blank' className="no-underline text-inherit hover:text-inherit inline"><CSharpType type='${typeName}' /></a>`;
        },
    );

    // Color all the html links with relative path (these are types)
    content = content.replace(
        /<a href=['"]((?!http)[^'"]*)['"]\s*>(\w+)<\/a>/g,
        (_, url, typeName) => {
            return `<a href='${url}' target='_self' className="no-underline text-inherit hover:text-inherit inline"><CSharpType type='${typeName}' /></a>`;
        },
    );

    // Remove newlines when arrows are used to preserve styling
    content = content.replace(/(\s)←(\s)\n/g, "$1←$2");

    // Add namespace link if the current file is a namespace itself
    if (file.includes("index")) {
        const parentNamespace = (className.match(/(.*)\.[^.]*$/) || [
            "",
            "",
        ])[1];

        if (parentNamespace.trim !== "" && fileLocations[parentNamespace])
            content =
                `\nParent namespace: [${parentNamespace}](.)\n\n` + content;
    }

    // Write adjusted content to the file
    fs.writeFileSync(
        file,
        frontmatter + imports.join("\n") + "\n" + content,
        "utf-8",
    );
});

generateIndexFiles(outputDir, "Namespaces");
