import { defineComponent, nextTick, onBeforeUnmount, onMounted } from "vue";
import Tile from "@/Components/UI/Tile/Tile";
import ActivityTile from "@/Components/UI/Tile/ActivityTile";
import type { Activity } from "@/Types/Activity";
import {
	Calendar,
	CircleCheckBig,
	Clock,
	TrendingUp,
	UsersRound,
} from "lucide-vue-next";
import Button from "@/Components/UI/Button";
import ListTile from "@/Components/UI/Tile/ListTile";
import AnnouncementTile from "@/Components/UI/Tile/AnnouncementTile";
import type { Announcement } from "@/Types/Announcement";
import { ref } from "vue";
import { cn } from "@/lib/utils";

export default defineComponent({
	setup() {
		// TO DO: fetch from API

		const name = "Rens"; // TO DO: fetch from user session

		const enrolledActivities: Activity[] = [
			{
				id: 1,
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

		const committees: {
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
				price: 0,
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

		const containerRef = ref<HTMLDivElement | null>(null);
		const visibleActivities = ref<Activity[]>([]);
		const comingActivitiesBelowEachother = ref<boolean>(false);

		const updateVisible = () => {
			if (!containerRef.value) return;

			const containerWidth = containerRef.value.getBoundingClientRect().width;

			let tileWidth = 200;
			const possibleTileBesidesEachother = Math.floor(
				containerWidth / tileWidth,
			);
			comingActivitiesBelowEachother.value = possibleTileBesidesEachother == 1;
			const count = comingActivitiesBelowEachother.value
				? 3
				: Math.floor(containerWidth / tileWidth);

			visibleActivities.value = activities.slice(0, count);
		};

		onMounted(() => {
			nextTick(updateVisible);
			window.addEventListener("resize", updateVisible);
		});

		onBeforeUnmount(() => {
			window.removeEventListener("resize", updateVisible);
		});

		return () => (
			<div>
				<div class="flex flex-col align-items-center gap-5 max-w-8xl mx-auto">
					<Tile class="w-full m-0 bg-[linear-gradient(var(--theme-475),var(--theme))] text-white">
						<div class="flex lg:flex-row flex-col gap-5">
							<div class="flex flex-col gap-5 grow basis-0">
								<p class="text-2xl font-semibold">Hey {name}!</p>
								<div class="flex gap-5">
									<Tile class="bg-(--theme-460) border border-white/20 grow">
										<p>Aanmeldingen</p>
										<div class="flex items-center gap-2">
											<p class="text-2xl">3</p> <CircleCheckBig />
										</div>
									</Tile>
									<Tile class="bg-(--theme-460) border border-white/20 grow">
										<p>Bijgewoond</p>
										<div class="flex items-center gap-2">
											<p class="text-2xl">12</p>
											<TrendingUp />
										</div>
									</Tile>
								</div>
								<Tile class="bg-(--theme-460) border border-white/20 grow">
									<div class="flex justify-between items-center">
										<div>
											<p>Openstaand</p>
											<p>€ 45,00</p>
										</div>
										<Button>Betalen</Button>
									</div>
								</Tile>
							</div>
							<Tile class="flex flex-col gap-4 bg-(--theme-460) border border-white/20 grow basis-0">
								<div class="flex items-center gap-2">
									<Clock /> Eerstvolgende activiteit
								</div>
								<p>{activities[0]?.title}</p>
								<div class="flex items-center gap-2">
									<Calendar />{" "}
									{activities[0]?.startdate.toLocaleDateString("default", {
										day: "numeric",
										month: "long",
										year: "numeric",
										hour: "2-digit",
										minute: "2-digit",
										hour12: false,
									})}
								</div>
								<div class="flex items-center gap-2">
									<UsersRound />{" "}
									{activities[0]?.maxParticipants
										? activities[0]?.maxParticipants -
											activities[0]?.numberOfParticipants
										: 0}{" "}
									van de {activities[0]?.maxParticipants} vrij{" "}
								</div>
								<Button showArrow={true}>Bekijk details</Button>
							</Tile>
						</div>
					</Tile>
					<div class="grid grid-cols-4 w-full gap-5">
						<div class="col-span-4 lg:col-span-3">
							<div class="flex w-full justify-between horizontal-align-center">
								<p class="font-semibold text-lg">Aankomende activiteiten:</p>
								<Button
									showArrow={true}
									class="bg-transparent p-0 hover:bg-transparent hover:text-(--theme-450)"
								>
									Bekijk alles
								</Button>
							</div>
							<div
								ref={containerRef}
								class={cn(
									"flex p-2 gap-5",
									comingActivitiesBelowEachother.value
										? "flex-col"
										: "flex-row p-2 gap-5",
								)}
							>
								{visibleActivities.value.map((visibleActivity) => (
									<ActivityTile class="w-full" activity={visibleActivity} />
								))}
							</div>
							<div class="flex w-full justify-between horizontal-align-center mb-5">
								<p class="font-semibold text-lg">Laatste mededelingen:</p>
								<Button
									showArrow={true}
									class="bg-transparent p-0 hover:bg-transparent hover:text-(--theme-450)"
								>
									Bekijk alles
								</Button>
							</div>
							<div class="flex flex-col gap-5">
								{announcements.map((announcement) => (
									<AnnouncementTile
										key={announcement.id}
										announcement={announcement}
									/>
								))}
							</div>
						</div>
						<div class="flex flex-col col-span-4 lg:col-span-1 gap-4">
							<p class="text-md">Mijn aanmeldingen:</p>
							<ListTile class="w-full">
								{enrolledActivities.map((activity) => (
									<div key={activity.id} class="flex p-2 gap-2">
										<div class="bg-(--theme-200) rounded-xl w-10 h-10">
											<CircleCheckBig class="text-(--theme) h-full m-auto" />
										</div>
										<div>
											<p>{activity.title}</p>
											<p class="text-gray-500">
												{activity.startdate.toLocaleDateString("default", {
													day: "numeric",
													month: "short",
												})}
											</p>
										</div>
									</div>
								))}
							</ListTile>

							<p class="text-md">Mijn commissies:</p>
							<ListTile class="w-full">
								{committees.map((committee) => (
									<div key={committee.id} class="flex p-2 gap-2">
										<div class="bg-(--theme-200) rounded-xl w-10 h-10 p-1">
											<img
												src={committee.icon}
												class="text-(--theme) h-full m-auto"
											/>
										</div>
										<div>
											<p>{committee.name}</p>
											<p class="text-gray-500">{committee.role}</p>
										</div>
									</div>
								))}
							</ListTile>
						</div>
					</div>
				</div>
			</div>
		);
	},
});
