import { useTranslation } from "react-i18next";
import ActivityEnrollmentOverview from "~/components/ActivityEnrollmentOverview";
import AnnouncementsList from "~/components/AnnouncementsList";
import CommitteeEnrollmentOverview from "~/components/CommitteeEnrollmentOverview";
import DashboardHeader from "~/components/DashboardHeader";
import Button from "~/components/UI/Button";
import UpcomingActivities from "~/components/UpcomingActivities";
import type { Activity } from "~/types/Activity";
import type { Announcement } from "~/types/Announcement";

export default function DashboardPage() {
  const { t } = useTranslation();

  const name = "Rens"; // TO DO: fetch from user session

  const enrolledActivities: Activity[] = [
    {
      id: 1,
      image: "https://koala.svsticky.nl/api/activities/2136/poster",
      title: "Study Trip",
      summary: "26 november is de hype-avond voor de studiereis! ...",
      price: 10,
      numberOfParticipants: 1,
      maxParticipants: 1,
      startdate: new Date("2024-06-01T10:00:00"),
      enddate: new Date("2024-06-01T10:05:00"),
      location: "Vagant",
      committee: "Studiereis",
    },
    {
      id: 2,
      image: "https://koala.svsticky.nl/api/activities/2136/poster",
      title: "Study Trip",
      summary: "26 november is de hype-avond voor de studiereis! ...",
      price: 0,
      numberOfParticipants: 1,
      maxParticipants: 1,
      startdate: new Date("2024-06-01T10:00:00"),
      enddate: new Date("2024-06-01T10:05:00"),
      location: "Vagant",
      committee: "Studiereis",
    },
    {
      id: 3,
      image: "https://koala.svsticky.nl/api/activities/2136/poster",
      title: "Study Trip",
      summary: "26 november is de hype-avond voor de studiereis! ...",
      price: 5,
      numberOfParticipants: 1,
      maxParticipants: 1,
      startdate: new Date("2024-06-01T10:00:00"),
      enddate: new Date("2024-06-01T10:05:00"),
      location: "Vagant",
      committee: "Studiereis",
    },
    {
      id: 4,
      image: "https://koala.svsticky.nl/api/activities/2136/poster",
      title: "Study Trip",
      summary: "26 november is de hype-avond voor de studiereis! ...",
      price: 15,
      numberOfParticipants: 1,
      maxParticipants: 1,
      startdate: new Date("2024-06-01T10:00:00"),
      enddate: new Date("2024-06-01T10:05:00"),
      location: "Vagant",
      committee: "Studiereis",
    },
    {
      id: 5,
      image: "https://koala.svsticky.nl/api/activities/2136/poster",
      title: "Study Trip",
      summary: "26 november is de hype-avond voor de studiereis! ...",
      price: 20,
      numberOfParticipants: 1,
      maxParticipants: 1,
      startdate: new Date("2024-06-01T10:00:00"),
      enddate: new Date("2024-06-01T10:05:00"),
      location: "Vagant",
      committee: "Studiereis",
    },
  ]; // TO DO: fetch from backend

  const committeeEnrollments = [
    {
      id: 1,
      name: "Attac",
      role: "Voorzitter",
      icon: "https://images.ctfassets.net/7cqe14fu3dhm/4FceQEboGHu8EZwRrghjC/7a9665b39e737bd1347f05fd4ef4794d/attac.svg",
    },
    {
      id: 2,
      name: "CultCo",
      role: "Fotograaf",
      icon: "https://images.ctfassets.net/7cqe14fu3dhm/4c9OXiO8n3A5dMLvDanSr5/5737507b03f86448212f1f6a90fb546c/Cultco4.png",
    },
  ]; // TO DO: fetch from backend

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
  ]; // TO DO: fetch from backend

  const activities: Activity[] = enrolledActivities; // Voor demo, of vervang door echte data

  return (
    <div className="flex flex-col items-center gap-5 max-w-8xl mx-auto">
      {/* Dashboard Header */}
      <DashboardHeader name={name} nextActivity={activities[0]} />

      {/* Content below dashboard header */}
      <div className="grid grid-cols-4 w-full gap-5">
        <div className="flex flex-col w-full gap-y-5 col-span-4 lg:col-span-3">
          {/* Upcoming Activities */}
          <div className="w-full">
            <div className="flex w-full justify-between items-center">
              <p className="font-semibold text-lg">
                {t("upcoming_activities")}
              </p>
              <Button
                showArrow
                className="bg-transparent p-0 hover:bg-transparent hover:text-(--board-primary-light)"
              >
                Bekijk alles
              </Button>
            </div>
            <UpcomingActivities activities={activities} />
          </div>

          {/* Announcements */}
          <div className="flex flex-col w-full gap-y-3">
            <div className="flex w-full justify-between items-center">
              <p className="font-semibold text-lg">
                {t("latest_announcements")}
              </p>
              <Button
                showArrow
                className="bg-transparent p-0 hover:bg-transparent hover:text-(--board-primary-light)"
              >
                Bekijk alles
              </Button>
            </div>
            <AnnouncementsList announcements={announcements} />
          </div>
        </div>

        {/* Enrollments and Committees */}
        <div className="flex flex-col col-span-4 lg:col-span-1 gap-3">
          <p className="text-md">{t("my_enrollments")}</p>
          <ActivityEnrollmentOverview enrolledActivities={enrolledActivities} />

          <p className="text-md">{t("my_committees")}</p>
          <CommitteeEnrollmentOverview
            committeeEnrollments={committeeEnrollments}
          />
        </div>
      </div>
    </div>
  );
}
