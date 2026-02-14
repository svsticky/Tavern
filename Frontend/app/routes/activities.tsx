import ActivityTile from "~/components/Tiles/ActivityTile";
import type { Activity } from "~/types/Activity";

export default function ActivitiesPage() {
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
  ];

  return (
    <div className="flex flex-col gap-5 w-full">
      <p className="text-2xl font-bold">Activiteiten</p>
      <div className="grid gap-4 justify-center grid-cols-[repeat(auto-fill,minmax(250px,1fr))] w-full">
        {activities.map((activity) => (
          <ActivityTile
            key={activity.id}
            className="w-auto"
            activity={activity}
          />
        ))}
      </div>
    </div>
  );
}
