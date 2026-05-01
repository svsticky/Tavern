import { t } from "i18next";
import * as Icons from "lucide-react";
import ExternalLinkTile from "~/components/ExternalLinkTile";
import { PageHeader } from "~/components/UI/PageHeader/PageHeader";

/**
 * A directory page providing a curated list of external services and association platforms.
 * 
 * This page serves as a "Linktree" for the association, centralizing access to various 
 * independent sub-systems (e.g., photo albums, wiki, file storage). It utilizes a 
 * responsive grid of `ExternalLinkTile` components.
 * 
 * Key features:
 * - **Dynamic Icon Mapping**: Uses string-to-component mapping from `lucide-react` 
 *   to render specific icons defined in the data.
 * - **Localization**: All titles and descriptions are retrieved via `i18next`.
 * - **Visual Variety**: Each link specifies its own background and text colors 
 *   for branding consistency across different sub-services.
 * 
 * @page
 * @component
 */
export default function ExternalLinksPage() {
  const links = [
    {
      id: 1,
      title: t("external_link_mongoose_title"),
      description: t("external_link_mongoose_description"),
      url: "https://mongoose.svsticky.nl",
      iconName: "Mongoose",
      iconBgColor: "bg-gray-100",
      iconColor: "text-gray-600"
    },
    {
      id: 2,
      title: t("external_link_photos_title"),
      description: t("external_link_photos_description"),
      url: "https://fotos.svsticky.nl",
      iconName: "Camera",
      iconBgColor: "bg-blue-100",
      iconColor: "text-blue-600"
    },
    {
      id: 3,
      title: t("external_link_files_title"),
      description: t("external_link_files_description"),
      url: "https://files.svsticky.nl",
      iconName: "FileText",
      iconBgColor: "bg-green-100",
      iconColor: "text-green-600"
    },
    {
        id: 4,
        title: t("external_link_digidecs_title"),
        description: t("external_link_digidecs_description"),
        url: "https://digidecs.svsticky.nl",
        iconName: "Calculator",
        iconBgColor: "bg-purple-100",
        iconColor: "text-purple-600"
    },
    {
        id: 5,
        title: t("external_link_books_title"),
        description: t("external_link_books_description"),
        url: "https://svsticky.nl/boeken",
        iconName: "BookOpen",
        iconBgColor: "bg-yellow-100",
        iconColor: "text-yellow-600"
    },
    {
        id: 6,
        title: t("external_link_jobs_title"),
        description: t("external_link_jobs_description"),
        url: "https://svsticky.nl/cariere/vacatures",
        iconName: "Briefcase",
        iconBgColor: "bg-red-100",
        iconColor: "text-red-600"
    },
    {
        id: 7,
        title: t("external_link_github_title"),
        description: t("external_link_github_description"),
        url: "https://github.com/svsticky",
        iconName: "Github",
        iconBgColor: "bg-gray-100",
        iconColor: "text-gray-600"
    },
    {
        id: 8,
        title: t("external_link_discord_title"),
        description: t("external_link_discord_description"),
        url: "https://svsticky.nl/discord",
        iconName: "Discord",
        iconBgColor: "bg-blurple-100",
        iconColor: "text-blurple-600"
    },
    {
        id: 9,
        title: t("external_link_stickypedia_title"),
        description: t("external_link_stickypedia_description"),
        url: "https://wiki.svsticky.nl",
        iconName: "Book",
        iconBgColor: "bg-orange-100",
        iconColor: "text-orange-600"
    },
    {
        id: 10,
        title: t("external_link_voeljeveilig_title"),
        description: t("external_link_voeljeveilig_description"),
        url: "https://voeljeveilig.svsticky.nl",
        iconName: "ShieldAlert",
        iconBgColor: "bg-pink-100",
        iconColor: "text-pink-600"
    },
    {
        id: 11,
        title: t("external_link_commissiestrijd_title"),
        description: t("external_link_commissiestrijd_description"),
        url: "https://commissiestrijd.svsticky.nl",
        iconName: "Trophy",
        iconBgColor: "bg-yellow-100",
        iconColor: "text-yellow-600"
    }
  ]; // To do: Should be fetched from new contentful

  return (
    <>
      <PageHeader title={t("external_links")} />

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        {links.map((link) => {
          const IconComponent = (Icons as any)[link.iconName] || Icons.Link;

          return (
            <ExternalLinkTile
              key={link.id}
              title={link.title}
              description={link.description}
              url={link.url}
              iconBgColor={link.iconBgColor}
              iconColor={link.iconColor}
              icon={<IconComponent size={24} />}
            />
          );
        })}
      </div>
    </>
  );
}
