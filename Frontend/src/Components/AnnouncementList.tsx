import { defineComponent } from "vue";
import AnnouncementTile from "@/Components/UI/Tile/AnnouncementTile";
import type { Announcement } from "@/Types/Announcement";

export default defineComponent({
    name: "AnnouncementsList",
    props: {
        announcements: { type: Array as () => Announcement[], required: true },
    },

    setup(props) {
        return () => (
            <div class="flex flex-col gap-5">
                {props.announcements.map((announcement) => (
                    <AnnouncementTile key={announcement.id} announcement={announcement} />
                ))}
            </div>
        );
    },
});
