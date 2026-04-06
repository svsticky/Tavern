import { t } from "i18next";
import * as Icons from "lucide-react";
import ExternalLinkTile from "~/components/Tiles/ExternalLinkTile";
import { PageHeader } from "~/components/UI/PageHeader";

export default function ExternalLinksPage() {
  const links = [
    {
      id: 1,
      title: "Mongoose",
      description: "Onze mini supermarkt",
      url: "https://mongoose.svsticky.nl",
      iconName: "Mongoose",
      iconBgColor: "bg-gray-100",
      iconColor: "text-gray-600"
    },
    {
      id: 2,
      title: "Foto's",
      description: "Bekijk onze foto's.",
      url: "https://fotos.svsticky.nl",
      iconName: "Camera",
      iconBgColor: "bg-blue-100",
      iconColor: "text-blue-600"
    },
    {
      id: 3,
      title: "Bestanden",
      description: "Bekijk onze bestanden.",
      url: "https://files.svsticky.nl",
      iconName: "FileText",
      iconBgColor: "bg-green-100",
      iconColor: "text-green-600"
    },
    {
        id: 4,
        title: "DigiDecs",
        description: "Declareer je onkosten snel en eenvoudig via DigiDecs.",
        url: "https://digidecs.svsticky.nl",
        iconName: "Calculator",
        iconBgColor: "bg-purple-100",
        iconColor: "text-purple-600"
    },
    {
        id: 5,
        title: "Boeken",
        description: "Haal boeken met korting.",
        url: "https://svsticky.nl/boeken",
        iconName: "BookOpen",
        iconBgColor: "bg-yellow-100",
        iconColor: "text-yellow-600"
    },
    {
        id: 6,
        title: "Vacatures",
        description: "Bekijk de vacatures binnen onze vereniging.",
        url: "https://svsticky.nl/cariere/vacatures",
        iconName: "Briefcase",
        iconBgColor: "bg-red-100",
        iconColor: "text-red-600"
    },
    {
        id: 7,
        title: "GitHub",
        description: "Bekijk onze code op GitHub.",
        url: "https://github.com/svsticky",
        iconName: "Github",
        iconBgColor: "bg-gray-100",
        iconColor: "text-gray-600"
    },
    {
        id: 8,
        title: "Discord",
        description: "Word lid van onze Discord server.",
        url: "https://svsticky.nl/discord",
        iconName: "Discord",
        iconBgColor: "bg-blurple-100",
        iconColor: "text-blurple-600"
    },
    {
        id: 9,
        title: "Stickypedia",
        description: "Onze eigen wiki vol met informatie over de vereniging.",
        url: "https://wiki.svsticky.nl",
        iconName: "Book",
        iconBgColor: "bg-orange-100",
        iconColor: "text-orange-600"
    },
    {
        id: 10,
        title: "VoelJeVeilig",
        description: "Meld ongewenst gedrag anoniem via VoelJeVeilig.",
        url: "https://voeljeveilig.svsticky.nl",
        iconName: "ShieldAlert",
        iconBgColor: "bg-pink-100",
        iconColor: "text-pink-600"
    },
    {
        id: 11,
        title: "Commissiestrijd",
        description: "Bekijk de voortgang van de commissiestrijd.",
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