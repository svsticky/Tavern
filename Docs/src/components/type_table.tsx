import { CSharpType } from "@/components/csharp_type";
import {
    Table,
    TableBody,
    TableCell,
    TableHead,
    TableHeader,
    TableRow,
} from "@/components/ui/table";

export type TypeItem = {
    name: string;
    description: string;
    type: string;
    links: string[];
};

// Descriptions are plain strings, so render `inline code` from the XML docs ourselves
function Description({ text }: { text: string }) {
    return text
        .split(/(`[^`]+`)/)
        .map((part, i) =>
            part.startsWith("`") && part.endsWith("`") && part.length > 1 ? (
                <code key={i}>{part.slice(1, -1)}</code>
            ) : (
                part
            ),
        );
}

export function TypeTable({
    types,
    fieldsTable = false,
}: {
    types: TypeItem[];
    fieldsTable: boolean;
}) {
    const includeName = types.some((type) => type.name.trim() !== "");
    const includeDescription = types.some(
        (type) => type.description.trim() !== "",
    );

    let i = 0;

    return (
        <Table className="mt-1">
            <TableHeader>
                <TableRow>
                    {includeName ? (
                        <TableHead className="w-[100px]">Name</TableHead>
                    ) : (
                        ""
                    )}
                    <TableHead>{fieldsTable ? "Field" : "Type"}</TableHead>
                    {includeDescription ? (
                        <TableHead>
                            {fieldsTable ? "Value" : "Description"}
                        </TableHead>
                    ) : (
                        ""
                    )}
                </TableRow>
            </TableHeader>
            <TableBody>
                {types.map((type) => (
                    <TableRow key={i++} className="hover:bg-transparent">
                        {includeName ? (
                            <TableCell className="font-medium">
                                {type.name}
                            </TableCell>
                        ) : (
                            ""
                        )}
                        <TableCell>
                            <CSharpType type={type.type} links={type.links} />
                        </TableCell>
                        {includeDescription ? (
                            <TableCell>
                                <Description text={type.description} />
                            </TableCell>
                        ) : (
                            ""
                        )}
                    </TableRow>
                ))}
            </TableBody>
        </Table>
    );
}
