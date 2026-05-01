/**
 * Formats markdown text for WhatsApp by converting it to a format that WhatsApp supports.
 * It handles headers, bold, italic, strikethrough, links, lists, and code formatting.
 * @param markdown The markdown text to format.
 * @returns The formatted text suitable for WhatsApp.
 */
export const formatForWhatsApp = (markdown: string | undefined | null): string => {
  if (!markdown) return "";

  return markdown
    // 1. Italic (*text* of _text_) -> _text_
    .replace(/(^|[^\w])\*([^\*\n]+)\*([^\w]|$)/g, '$1_$2_$3')

    // 2. Bold (**text**) -> *text*
    .replace(/\*\*(.*?)\*\*/g, '*$1*')

    // 3. Headers (# Header) -> *Header*
    .replace(/^#+\s+(.*)$/gm, '*$1*')

    // 4. Strikethrough (~~text~~) -> ~text~
    .replace(/~~(.*?)~~/g, '~$1~')

    // 5. Links [name](url) -> name (url)
    .replace(/\[(.*?)\]\((.*?)\)/g, '$1 ($2)')

    // 6. Lists (- of *) -> •
    .replace(/^\s*[\-\*]\s+/gm, '- ')

    // 7. Horizontal lines
    .replace(/^[\-\*_]{3,}$/gm, '')

    // 8. Tables
    .replace(/^\|?[\s\-\|:]+\|?$/gm, '')
    .replace(/^\|(.*)\|$/gm, (match, content) => {
       return content.split('|').map((c: string) => c.trim()).join(' - ');
    });
};

/**
 * Formats markdown text for Google Calendar by converting it to HTML that Google Calendar supports.
 * It handles headers, bold, italic, strikethrough, links, lists, and code formatting.
 * @param markdown The markdown text to format.
 * @returns The formatted text suitable for Google Calendar.
 */
export const formatForGoogleCalendar = (markdown: string | undefined | null): string => {
  if (!markdown) return "";

  return markdown
    // 1. Tables
    .replace(/^\|?[\s\-\|:]+\|?$/gm, '')
    .replace(/^\|(.*)\|$/gm, (match, content) => {
       return content.split('|').map((c: string) => c.trim()).filter((c: string) => c).join(' - ') + '<br>';
    })
    // 2. Horizontal lines
    .replace(/^[\-\*_]{3,}$/gm, '')
    // 3. Bold (**text**) to <b>
    .replace(/\*\*(.*?)\*\*/g, '<b>$1</b>')
    // 4. Italic (*text*) to <i>
    .replace(/\*([^\*]+)\*/g, '<i>$1</i>')
    // 5. Strikethrough (~~text~~) to <s>
    .replace(/~~(.*?)~~/g, '<s>$1</s>')
    // 6. Urls [name](url) to <a href="url">
    .replace(/\[(.*?)\]\((.*?)\)/g, '<a href="$2">$1</a>')
    // 7. Headers to <b> 
    .replace(/^#+\s+(.*)$/gm, '<b>$1</b><br>')
    // 8. New lines to <br> 
    .replace(/\n/g, '<br>')
    // 9. Lists to bullets
    .replace(/^\s*[\-\*]\s+/gm, '• ');
};
