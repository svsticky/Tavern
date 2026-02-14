import AnnouncementsList from "~/components/AnnouncementsList";
import type { Announcement } from "~/types/Announcement";

export default function AnnouncementsPage() {
  const announcements: Announcement[] = [
    {
      id: 1,
      title: "Nieuwe activiteiten voor december!",
      announcement:
        "We hebben een aantal geweldige nieuwe activiteiten toegevoegd voor de maand december. Van sportieve uitdagingen tot gezellige sociale evenementen. Bekijk de activiteitenpagina voor het volledige overzicht.",
      announcer: "Bestuur",
      date: new Date(),
    },
    {
      id: 2,
      title: "Belangrijke wijziging ledenvergadering",
      announcement:
        "Let op! De algemene ledenvergadering van 5 december start om 19:00 uur in plaats van 20:00 uur. De locatie blijft hetzelfde: Clubhuis - Grote Zaal. We hopen jullie allemaal te zien!",
      announcer: "Secretaris",
      date: new Date(),
    },
  ];

  return (
    <div className="flex flex-col gap-5">
      <p className="text-2xl font-bold">Announcements</p>
      <AnnouncementsList announcements={announcements} />
    </div>
  );
}
