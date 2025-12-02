import { defineComponent } from "vue";
import { cn } from "@/lib/utils";
import Tile from "./Tile";
import { Calendar, Megaphone } from "lucide-vue-next";
import type { Announcement } from "@/Types/Announcement";

const AnnouncementTile = defineComponent({
	props: {
		announcement: {
			type: Object as () => Announcement,
			required: true,
		},
		class: {
			type: String,
			required: false,
		},
	},

	setup(props) {
		return () => (
			<Tile class={cn("rounded-2xl border border-gray-200", props.class)}>
				<div class="flex w-full justify-between">
					<p class="mb-2">{props.announcement.title}</p>
					<p class="flex text-gray-600 gap-1 text-sm">
						<Calendar class="h-5" />{" "}
						{props.announcement.date.toLocaleDateString()}
					</p>
				</div>
				<p class="text-gray-600">{props.announcement.announcement}</p>
				<div class="h-[0.5px] w-full my-2 bg-gray-200"></div>
				<p class="text-gray-600 flex gap-2 items-center">
					<Megaphone class="h-5" /> {props.announcement.announcer}
				</p>
			</Tile>
		);
	},
});

export default AnnouncementTile;
