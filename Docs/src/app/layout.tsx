import { RootProvider } from 'fumadocs-ui/provider/next';
import { Inter } from "next/font/google";
import "./globals.css";

// Metadata
export const metadata = {
  title: 'Commissiestrijd - Documentation',
  icons: [
    {
      rel: 'icon',
      type: 'image/png',
      url: 'https://svsticky.nl/favicon-32x32.png?v=493e7616b0e8a256684f189691fc6faa',
    },
    {
      rel: 'icon',
      type: 'image/svg+xml',
      url: 'https://public.svsticky.nl/logos/hoofd_outline_kleur.svg',
    },
  ],
};

// Font Inter
const inter = Inter({ subsets: ["latin"] });

export default function RootLayout({
    children,
}: Readonly<{ children: React.ReactNode }>) {
    return (
        <html lang="en" suppressHydrationWarning>
            <body className={inter.className}>
                <RootProvider>{children}</RootProvider>
            </body>
        </html>
    );
}