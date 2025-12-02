import { defineComponent } from "vue";
import { cn } from "@/lib/utils";
import Tile from "./Tile";
import type { Activity } from "@/Types/Activity";
import { Calendar, MapPin, UsersRound } from "lucide-vue-next";

const ActivityTile = defineComponent({
	props: {
		activity: {
			type: Object as () => Activity,
			required: true,
		},
		class: {
			type: String,
			required: false,
		},
	},

	setup(props) {
		return () => (
			<Tile class={cn("inline-block w-60 rounded-2xl p-0", props.class)}>
				<img
					src={props.activity.image}
					alt={props.activity.title}
					class="rounded-t-2xl w-full"
				/>
				<div class="p-3 border-r border-l border-b rounded-b-2xl border-gray-200">
					<p class="text-[18px] font-bold mt-1.5 mb-1">
						{props.activity.title}
					</p>
					<div class="flex flex-col text-[14px] text-gray-500 mt-0">
						<div class="flex items-center gap-1.5 mt-1">
							<Calendar size={12} />
							{props.activity.startdate.getDate()}{" "}
							{props.activity.startdate.toLocaleDateString("default", {
								month: "short",
							})}{" "}
							•{" "}
							{props.activity.startdate.toLocaleTimeString("default", {
								hour: "2-digit",
								minute: "2-digit",
							})}
						</div>
						<div class="flex items-center gap-1.5 mt-1">
							<MapPin size={12} /> {props.activity.location}
						</div>
						<div class="flex items-center gap-1.5 mt-1">
							<UsersRound size={12} />{" "}
							{props.activity.maxParticipants -
								props.activity.numberOfParticipants}{" "}
							plaatsen vrij
						</div>
					</div>
				</div>
			</Tile>
		);
	},
});

export default ActivityTile;
