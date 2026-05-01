import { t } from "i18next";
import { useState, useEffect, useMemo } from "react";
import 'react-quill-new/dist/quill.snow.css';

/**
 * Props for the HtmlEditor component.
 * @interface HtmlEditorProps
 * @property {string} value - The HTML string content of the editor.
 * @property {(content: string) => void} onChange - Callback triggered when the content changes.
 * @property {string} [placeholder] - Placeholder text displayed when the editor is empty.
 * @property {string} [label] - Optional label text displayed above the editor.
 */
interface HtmlEditorProps {
    value: string;
    onChange: (content: string) => void;
    placeholder?: string;
    label?: string;
}

/**
 * A Rich Text (HTML) Editor component based on Quill.
 * 
 * This component utilizes dynamic importing for `react-quill-new` to ensure 
 * compatibility with Server-Side Rendering (SSR) environments where `document` 
 * is not available during initial load. It features a customized toolbar 
 * and specific CSS overrides to integrate seamlessly with the application's design.
 * 
 * @component
 * @param {HtmlEditorProps} props - The component properties.
 */
export default function HtmlEditor({ value, onChange, placeholder, label }: HtmlEditorProps) {
    const [QuillEditor, setQuillEditor] = useState<any>(null);
    const [isMounted, setIsMounted] = useState(false);

    useEffect(() => {
        setIsMounted(true);
        import('react-quill-new').then((mod) => {
            setQuillEditor(() => mod.default);
        });
    }, []);

    const modules = useMemo(() => ({
        toolbar: [
            [{ 'header': [1, 2, false] }],
            ['bold', 'italic', 'underline'],
            [{ 'list': 'ordered' }, { 'list': 'bullet' }],
            ['link', 'clean']
        ],
    }), []);

    if (!isMounted || !QuillEditor) {
        return (
            <div className="flex flex-col gap-2">
                {label && <label className="text-sm font-medium text-gray-700">{label}</label>}
                <div className="h-72 w-full bg-gray-50 border border-gray-200 rounded-md animate-pulse flex items-center justify-center text-gray-400">
                    <span>{t("loading")}</span>
                </div>
            </div>
        );
    }

    return (
        <div className="flex flex-col gap-2 html-editor-container">
            {label && <label className="text-sm font-medium text-gray-700">{label}</label>}
            
            <div className="quill-wrapper bg-white border rounded-md overflow-hidden shadow-sm">
                <QuillEditor 
                    theme="snow" 
                    value={value} 
                    onChange={onChange} 
                    modules={modules}
                    placeholder={placeholder}
                    className="h-64 mb-12"
                />
            </div>

            <style dangerouslySetInnerHTML={{ __html: `
                .quill-wrapper .ql-toolbar.ql-snow {
                    border-top: none !important;
                    border-left: none !important;
                    border-right: none !important;
                    border-bottom: 1px solid #e5e7eb !important;
                    background-color: #f9fafb;
                }
                .quill-wrapper .ql-container.ql-snow {
                    border: none !important;
                }
                /* Dit zorgt ervoor dat die enorme pijlen (SVGs) nooit groter worden dan de knop */
                .quill-wrapper .ql-toolbar .ql-formats svg {
                    width: 18px !important;
                    height: 18px !important;
                }
                .quill-wrapper .ql-editor {
                    font-size: 16px;
                    min-height: 200px;
                }
            `}} />
        </div>
    );
}