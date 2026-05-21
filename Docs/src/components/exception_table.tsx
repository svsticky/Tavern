import { CSharpType } from "@/components/csharp_type";
import {
    Table,
    TableBody,
    TableCell,
    TableHead,
    TableHeader,
    TableRow,
} from "@/components/ui/table";

export type ExceptionItem = {
    type: string;
    description: string;
    link: string;
};

export function ExceptionTable({
    exceptions,
}: {
    exceptions: ExceptionItem[];
}) {
    const includeDescription = exceptions.some(
        (exception) => exception.description.trim() !== "",
    );

    return (
        <Table className="mt-1">
            <TableHeader>
                <TableRow>
                    <TableHead>Type</TableHead>
                    {includeDescription ? (
                        <TableHead>Description</TableHead>
                    ) : (
                        ""
                    )}
                </TableRow>
            </TableHeader>
            <TableBody>
                {exceptions.map((exception) => (
                    <TableRow key={exception.type}>
                        <TableCell>
                            <a
                                href={exception.link}
                                target={
                                    exception.link.startsWith("http")
                                        ? "_blank"
                                        : "_self"
                                }
                                className="text-inherit no-underline hover:text-inherit"
                            >
                                <CSharpType
                                    type={exception.type}
                                    links={[exception.link]}
                                />
                            </a>
                        </TableCell>
                        {!includeDescription ? (
                            ""
                        ) : (
                            <TableCell>{exception.description}</TableCell>
                        )}
                    </TableRow>
                ))}
            </TableBody>
        </Table>
    );
}
