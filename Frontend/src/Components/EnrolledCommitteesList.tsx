import { defineComponent } from "vue";
import ListTile from "@/Components/UI/Tile/ListTile";
import type { CommitteeEnrollment } from "@/Types/CommitteeEnrollment";

export default defineComponent({
  name: "EnrolledCommitteesList",
  props: {
    CommitteeEnrollments: {
      type: Array as () => CommitteeEnrollment[],
      required: true,
    },
  },
  setup(props) {
    return () => (
      <ListTile class="w-full">
        {props.CommitteeEnrollments.map((CommitteeEnrollment) => (
          <div key={CommitteeEnrollment.id} class="flex p-2 gap-2">
            {/* Icon Container */}
            <div class="bg-(--theme-200) rounded-xl w-10 h-10 p-1">
              <img
                src={CommitteeEnrollment.icon}
                class="text-(--theme) h-full m-auto"
              />
            </div>

            {/* Committee Details */}
            <div>
              <p>{CommitteeEnrollment.name}</p>
              <p class="text-gray-500">{CommitteeEnrollment.role}</p>
            </div>
          </div>
        ))}
      </ListTile>
    );
  },
});
