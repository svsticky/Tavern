import {
  defineComponent,
  onBeforeUnmount,
  onMounted,
  type PropType,
  ref,
} from "vue";

export default defineComponent({
  name: "ProfileDropdown",
  props: {
    username: { type: String, required: true },
    avatarUrl: { type: String, required: true },
    options: {
      type: Array as () => Array<{ label: string; action: () => void }>,
      default: () => [],
    },
    isMobile: { type: Boolean, default: false },
    onOptionClick: { type: Function as PropType<() => void>, required: false },
  },

  setup(props) {
    const isProfileOpen = ref(false);
    const dropdownRef = ref<HTMLElement | null>(null);

    // Toggle profile dropdown
    const toggleProfile = () => {
      if (!props.isMobile) {
        isProfileOpen.value = !isProfileOpen.value;
      }
    };

    // Handle option click
    const handleOptionClick = (action: () => void) => {
      action();
      if (!props.isMobile) {
        isProfileOpen.value = false;
      }
      if (props.onOptionClick) {
        props.onOptionClick();
      }
    };

    // Handle clicks outside the dropdown to close it
    const handleClickOutside = (event: MouseEvent) => {
      if (!props.isMobile && isProfileOpen.value) {
        if (
          dropdownRef.value &&
          !dropdownRef.value.contains(event.target as Node)
        ) {
          isProfileOpen.value = false;
        }
      }
    };

    // Set up and clean up event listeners
    onMounted(() => {
      document.addEventListener("click", handleClickOutside);
    });

    // Clean up event listeners
    onBeforeUnmount(() => {
      document.removeEventListener("click", handleClickOutside);
    });

    return () => (
      <div
        ref={dropdownRef}
        class={`${props.isMobile ? "ml-0 w-full" : "relative ml-5"}`}
      >
        <button
          type="button"
          class={`
              flex items-center gap-2 rounded-xl border-2 border-transparent
              py-1 px-2
              ${props.isMobile
              ? "w-full justify-start py-2 px-3"
              : "cursor-pointer transition-colors duration-200 ease-in-out hover:bg-(--theme-460) hover:border-white/20"
            }
        `}
          onClick={props.isMobile ? undefined : toggleProfile}
        >
          <img
            src={props.avatarUrl}
            alt={`${props.username} Avatar`}
            class="w-8 h-8 rounded-full object-cover"
          />
          <span class="text-white font-bold text-sm">{props.username}</span>
        </button>

        {/* Dropdown menu (always visible on mobile, conditional on desktop) */}
        {(isProfileOpen.value || props.isMobile) && (
          <div
            class={`rounded shadow-lg min-w-30 z-500 overflow-hidden flex flex-col py-1
                  ${!props.isMobile
                ? "absolute mt-3 top-full right-0 shadow-lg bg-white"
                : ""
              }
                  ${props.isMobile
                ? "relative top-auto right-auto w-full mt-1 bg-transparent shadow-none"
                : ""
              }
              `}
          >
            {/* Option list */}
            <ul class="flex flex-col py-1">
              {props.options.map((option) => (
                <li key={option.label}>
                  <button
                    type="button"
                    class={`flex items-center gap-2 py-2.5 px-4 text-sm no-underline w-full
                      cursor-pointer
                      ${props.isMobile
                        ? "text-white hover:bg-(--theme-450) rounded-lg"
                        : "text-gray-800 bg-transparent hover:bg-[#f0f0f0]"
                      }`}
                    onClick={() => handleOptionClick(option.action)}
                  >
                    <span>{option.label}</span>
                  </button>
                </li>
              ))}
            </ul>
          </div>
        )}
      </div>
    );
  },
});
