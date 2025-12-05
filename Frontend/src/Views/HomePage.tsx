import { defineComponent } from "vue";
import AnnouncementsList from "@/Components/AnnouncementsList";
import DashboardHeader from "@/Components/DashboardHeader";
import EnrolledCommitteesList from "@/Components/EnrolledCommitteesList";
import Enrollments from "@/Components/Enrollments";
import Button from "@/Components/UI/Button";
import UpcomingActivities from "@/Components/UpcomingActivities";
import type { Activity } from "@/Types/Activity";
import type { Announcement } from "@/Types/Announcement";
import { useI18n } from "vue-i18n";

export default defineComponent({
  setup() {
    const { t } = useI18n();

    const name = "Rens"; // TO DO: fetch from user session

    const enrolledActivities: Activity[] = [
      {
        id: 1,
        image:
          "https://koala.svsticky.nl/rails/active_storage/representations/redirect/eyJfcmFpbHMiOnsibWVzc2FnZSI6IkJBaHBBbWNPIiwiZXhwIjpudWxsLCJwdXIiOiJibG9iX2lkIn19--4ee97e626759807734d88d6665b458613bf1f91d/eyJfcmFpbHMiOnsibWVzc2FnZSI6IkJBaDdCem9MWm05eWJXRjBTU0lJY0c1bkJqb0dSVlE2QzNKbGMybDZaVWtpRFRJMU5IZ3pOakFoQmpzR1ZBPT0iLCJleHAiOm51bGwsInB1ciI6InZhcmlhdGlvbiJ9fQ==--df7ad6130171d4ba961b6bb3a40f5477e5aac46a/Save%20the%20date%20poster.png",
        title: "Study Trip",
        summary:
          "26 november is de hype-avond voor de studiereis!\n\nWe gaan weer met Sticky het verre buitenland opzoeken! Deze keer buiten de grenzen van Europa duiken we in een compleet nieuwe wereld vol cultuur, bijzondere ervaringen en geweldig eten.\n\nTijdens de avond onthullen we eindelijk de bestemming en kun jij kijken of je goed gegokt hebt. We geven een presentatie met alle belangrijke informatie die jij nodig hebt om mee te gaan. Na de presentatie opent de inschrijfperiode en heb je tot en met 12 december de tijd om je aan te melden en je motivatie in te sturen.\n\nWij van de studiereis commissie gaan er een mooie reis van maken en we hopen dat natuurlijk jij mee gaat!",
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
        image:
          "https://koala.svsticky.nl/rails/active_storage/representations/redirect/eyJfcmFpbHMiOnsibWVzc2FnZSI6IkJBaHBBbWNPIiwiZXhwIjpudWxsLCJwdXIiOiJibG9iX2lkIn19--4ee97e626759807734d88d6665b458613bf1f91d/eyJfcmFpbHMiOnsibWVzc2FnZSI6IkJBaDdCem9MWm05eWJXRjBTU0lJY0c1bkJqb0dSVlE2QzNKbGMybDZaVWtpRFRJMU5IZ3pOakFoQmpzR1ZBPT0iLCJleHAiOm51bGwsInB1ciI6InZhcmlhdGlvbiJ9fQ==--df7ad6130171d4ba961b6bb3a40f5477e5aac46a/Save%20the%20date%20poster.png",
        title: "Study Trip",
        summary:
          "26 november is de hype-avond voor de studiereis!\n\nWe gaan weer met Sticky het verre buitenland opzoeken! Deze keer buiten de grenzen van Europa duiken we in een compleet nieuwe wereld vol cultuur, bijzondere ervaringen en geweldig eten.\n\nTijdens de avond onthullen we eindelijk de bestemming en kun jij kijken of je goed gegokt hebt. We geven een presentatie met alle belangrijke informatie die jij nodig hebt om mee te gaan. Na de presentatie opent de inschrijfperiode en heb je tot en met 12 december de tijd om je aan te melden en je motivatie in te sturen.\n\nWij van de studiereis commissie gaan er een mooie reis van maken en we hopen dat natuurlijk jij mee gaat!",
        price: 0,
        numberOfParticipants: 1,
        maxParticipants: 1,
        startdate: new Date("2024-06-01T10:00:00"),
        enddate: new Date("2024-06-01T10:05:00"),
        location: "Vagant",
        committee: "Studiereis",
      },
    ]; // TO DO: fetch from backend

    const committeeEnrollments: {
      id: number;
      name: string;
      role: string;
      icon: string;
    }[] = [
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

    const activities: Activity[] = [
      {
        id: 1,
        image:
          "https://koala.svsticky.nl/rails/active_storage/representations/redirect/eyJfcmFpbHMiOnsibWVzc2FnZSI6IkJBaHBBbWNPIiwiZXhwIjpudWxsLCJwdXIiOiJibG9iX2lkIn19--4ee97e626759807734d88d6665b458613bf1f91d/eyJfcmFpbHMiOnsibWVzc2FnZSI6IkJBaDdCem9MWm05eWJXRjBTU0lJY0c1bkJqb0dSVlE2QzNKbGMybDZaVWtpRFRJMU5IZ3pOakFoQmpzR1ZBPT0iLCJleHAiOm51bGwsInB1ciI6InZhcmlhdGlvbiJ9fQ==--df7ad6130171d4ba961b6bb3a40f5477e5aac46a/Save%20the%20date%20poster.png",
        title: "Study Trip",
        summary:
          "26 november is de hype-avond voor de studiereis!\n\nWe gaan weer met Sticky het verre buitenland opzoeken! Deze keer buiten de grenzen van Europa duiken we in een compleet nieuwe wereld vol cultuur, bijzondere ervaringen en geweldig eten.\n\nTijdens de avond onthullen we eindelijk de bestemming en kun jij kijken of je goed gegokt hebt. We geven een presentatie met alle belangrijke informatie die jij nodig hebt om mee te gaan. Na de presentatie opent de inschrijfperiode en heb je tot en met 12 december de tijd om je aan te melden en je motivatie in te sturen.\n\nWij van de studiereis commissie gaan er een mooie reis van maken en we hopen dat natuurlijk jij mee gaat!",
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
        image:
          "https://koala.svsticky.nl/rails/active_storage/representations/redirect/eyJfcmFpbHMiOnsibWVzc2FnZSI6IkJBaHBBbWNPIiwiZXhwIjpudWxsLCJwdXIiOiJibG9iX2lkIn19--4ee97e626759807734d88d6665b458613bf1f91d/eyJfcmFpbHMiOnsibWVzc2FnZSI6IkJBaDdCem9MWm05eWJXRjBTU0lJY0c1bkJqb0dSVlE2QzNKbGMybDZaVWtpRFRJMU5IZ3pOakFoQmpzR1ZBPT0iLCJleHAiOm51bGwsInB1ciI6InZhcmlhdGlvbiJ9fQ==--df7ad6130171d4ba961b6bb3a40f5477e5aac46a/Save%20the%20date%20poster.png",
        title: "Study Trip",
        summary:
          "26 november is de hype-avond voor de studiereis!\n\nWe gaan weer met Sticky het verre buitenland opzoeken! Deze keer buiten de grenzen van Europa duiken we in een compleet nieuwe wereld vol cultuur, bijzondere ervaringen en geweldig eten.\n\nTijdens de avond onthullen we eindelijk de bestemming en kun jij kijken of je goed gegokt hebt. We geven een presentatie met alle belangrijke informatie die jij nodig hebt om mee te gaan. Na de presentatie opent de inschrijfperiode en heb je tot en met 12 december de tijd om je aan te melden en je motivatie in te sturen.\n\nWij van de studiereis commissie gaan er een mooie reis van maken en we hopen dat natuurlijk jij mee gaat!",
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
        image:
          "https://koala.svsticky.nl/rails/active_storage/representations/redirect/eyJfcmFpbHMiOnsibWVzc2FnZSI6IkJBaHBBbWNPIiwiZXhwIjpudWxsLCJwdXIiOiJibG9iX2lkIn19--4ee97e626759807734d88d6665b458613bf1f91d/eyJfcmFpbHMiOnsibWVzc2FnZSI6IkJBaDdCem9MWm05eWJXRjBTU0lJY0c1bkJqb0dSVlE2QzNKbGMybDZaVWtpRFRJMU5IZ3pOakFoQmpzR1ZBPT0iLCJleHAiOm51bGwsInB1ciI6InZhcmlhdGlvbiJ9fQ==--df7ad6130171d4ba961b6bb3a40f5477e5aac46a/Save%20the%20date%20poster.png",
        title: "Study Trip",
        summary:
          "26 november is de hype-avond voor de studiereis!\n\nWe gaan weer met Sticky het verre buitenland opzoeken! Deze keer buiten de grenzen van Europa duiken we in een compleet nieuwe wereld vol cultuur, bijzondere ervaringen en geweldig eten.\n\nTijdens de avond onthullen we eindelijk de bestemming en kun jij kijken of je goed gegokt hebt. We geven een presentatie met alle belangrijke informatie die jij nodig hebt om mee te gaan. Na de presentatie opent de inschrijfperiode en heb je tot en met 12 december de tijd om je aan te melden en je motivatie in te sturen.\n\nWij van de studiereis commissie gaan er een mooie reis van maken en we hopen dat natuurlijk jij mee gaat!",
        price: 0,
        numberOfParticipants: 1,
        maxParticipants: 1,
        startdate: new Date("2024-06-01T10:00:00"),
        enddate: new Date("2024-06-01T10:05:00"),
        location: "Vagant",
        committee: "Studiereis",
      },
      {
        id: 4,
        image:
          "https://koala.svsticky.nl/rails/active_storage/representations/redirect/eyJfcmFpbHMiOnsibWVzc2FnZSI6IkJBaHBBbWNPIiwiZXhwIjpudWxsLCJwdXIiOiJibG9iX2lkIn19--4ee97e626759807734d88d6665b458613bf1f91d/eyJfcmFpbHMiOnsibWVzc2FnZSI6IkJBaDdCem9MWm05eWJXRjBTU0lJY0c1bkJqb0dSVlE2QzNKbGMybDZaVWtpRFRJMU5IZ3pOakFoQmpzR1ZBPT0iLCJleHAiOm51bGwsInB1ciI6InZhcmlhdGlvbiJ9fQ==--df7ad6130171d4ba961b6bb3a40f5477e5aac46a/Save%20the%20date%20poster.png",
        title: "Study Trip",
        summary:
          "26 november is de hype-avond voor de studiereis!\n\nWe gaan weer met Sticky het verre buitenland opzoeken! Deze keer buiten de grenzen van Europa duiken we in een compleet nieuwe wereld vol cultuur, bijzondere ervaringen en geweldig eten.\n\nTijdens de avond onthullen we eindelijk de bestemming en kun jij kijken of je goed gegokt hebt. We geven een presentatie met alle belangrijke informatie die jij nodig hebt om mee te gaan. Na de presentatie opent de inschrijfperiode en heb je tot en met 12 december de tijd om je aan te melden en je motivatie in te sturen.\n\nWij van de studiereis commissie gaan er een mooie reis van maken en we hopen dat natuurlijk jij mee gaat!",
        price: 0,
        numberOfParticipants: 1,
        maxParticipants: 1,
        startdate: new Date("2024-06-01T10:00:00"),
        enddate: new Date("2024-06-01T10:05:00"),
        location: "Vagant",
        committee: "Studiereis",
      },
    ];

    return () => (
      <div class="flex flex-col align-items-center gap-5 max-w-8xl mx-auto">
        {/* Dashboard Header */}
        <DashboardHeader name={name} nextActivity={activities[0]} />

        {/* Content below dashboard header */}
        <div class="grid grid-cols-4 w-full gap-5">
          <div class="flex flex-col w-full gap-y-5 col-span-4 lg:col-span-3">
            {/* Upcoming Activities */}
            <div class="w-full">
              <div class="flex w-full justify-between horizontal-align-center">
                <p class="font-semibold text-lg">{ t("upcomming_activities") }</p>
                <Button
                  showArrow
                  class="bg-transparent p-0 hover:bg-transparent hover:text-(--theme-450)"
                >
                  Bekijk alles
                </Button>
              </div>
              <UpcomingActivities activities={activities} />
            </div>

            {/* Announcements */}
            <div class="flex flex-col w-full gap-y-3">
              <div class="flex w-full justify-between horizontal-align-center">
                <p class="font-semibold text-lg">{ t("latest_announcements") }</p>
                <Button
                  showArrow
                  class="bg-transparent p-0 hover:bg-transparent hover:text-(--theme-450)"
                >
                  Bekijk alles
                </Button>
              </div>
              <AnnouncementsList announcements={announcements} />
            </div>
          </div>

          {/* Enrollments and Committees */}
          <div class="flex flex-col col-span-4 lg:col-span-1 gap-3">
            <p class="text-md">{ t("my_enrollments") }</p>
            <Enrollments enrolledActivities={enrolledActivities} />

            <p class="text-md">{ t("my_committees") }</p>
            <EnrolledCommitteesList
              CommitteeEnrollments={committeeEnrollments}
            />
          </div>
        </div>
      </div>
    );
  },
});
