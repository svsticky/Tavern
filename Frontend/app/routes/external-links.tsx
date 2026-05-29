import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import * as Icons from "lucide-react";
import ExternalLinkTile from "~/components/ExternalLinkTile";
import { PageHeader } from "~/components/UI/PageHeader/PageHeader";
import { getExternallinks, type ExternalLinkResponseDto } from "~/api";
import { getEnv } from "~/util/config.utils";

/**
 * A directory page providing a curated list of external services and association platforms.
 *
 * This page serves as a "Linktree" for the association, centralizing access to various
 * independent sub-systems (e.g., photo albums, wiki, file storage). It utilizes a
 * responsive grid of `ExternalLinkTile` components.
 *
 * @page
 * @component
 */
export default function ExternalLinksPage() {
  const { t, i18n } = useTranslation();
  const isDutch = i18n.language.startsWith("nl");

  const [links, setLinks] = useState<ExternalLinkResponseDto[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchLinks = async () => {
      try {
        const res = await getExternallinks();
        if (res.data) {
          // Sort by sortOrder ascending
          const sorted = [...res.data].sort((a, b) => a.sortOrder - b.sortOrder);
          setLinks(sorted);
        }
      } catch (e) {
        console.error("Failed to fetch external links:", e);
      } finally {
        setLoading(false);
      }
    };
    fetchLinks();
  }, []);

  const defaultIcons = [
    Icons.LayoutDashboard, // Mongoose
    Icons.Camera,          // Photos
    Icons.FileText,        // Files
    Icons.Calculator,      // Digidecs
    Icons.BookOpen,        // Books
    Icons.Briefcase,       // Jobs
    Icons.Github,          // Github
    Icons.MessageSquare,   // Discord
    Icons.Book,            // Stickypedia
    Icons.ShieldAlert,     // Voeljeveilig
    Icons.Trophy,          // Commissiestrijd
  ];

  const defaultColors = [
    { bg: "bg-gray-100", text: "text-gray-600" },
    { bg: "bg-blue-100", text: "text-blue-600" },
    { bg: "bg-green-100", text: "text-green-600" },
    { bg: "bg-purple-100", text: "text-purple-600" },
    { bg: "bg-yellow-100", text: "text-yellow-600" },
    { bg: "bg-red-100", text: "text-red-600" },
    { bg: "bg-gray-100", text: "text-gray-600" },
    { bg: "bg-blurple-100", text: "text-blurple-600" },
    { bg: "bg-orange-100", text: "text-orange-600" },
    { bg: "bg-pink-100", text: "text-pink-600" },
    { bg: "bg-yellow-100", text: "text-yellow-600" },
  ];

  return (
    <>
      <PageHeader title={t("external_links")} />

      {loading ? (
        <div className="text-center text-slate-500 py-12">{t("loading")}</div>
      ) : links.length === 0 ? (
        <div className="text-center text-slate-500 py-12">{t("no_external_links")}</div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {links.map((link, index) => {
            const colors = defaultColors[index % defaultColors.length] || { bg: "bg-slate-100", text: "text-slate-600" };
            
            const icon = link.iconPath ? (
              <img
                src={`${getEnv("ApiUrl")}/externallinks/${link.id}/icon`}
                alt=""
                className="w-6 h-6 object-contain"
                loading="lazy"
              />
            ) : (() => {
              const FallbackIcon = defaultIcons[index % defaultIcons.length] || Icons.Link;
              return <FallbackIcon size={24} />;
            })();

            return (
              <ExternalLinkTile
                key={link.id}
                title={isDutch ? link.titleDutch : link.titleEnglish}
                description={isDutch ? link.descriptionDutch : link.descriptionEnglish}
                url={link.url}
                iconBgColor={colors.bg}
                iconColor={colors.text}
                icon={icon}
              />
            );
          })}
        </div>
      )}
    </>
  );
}
