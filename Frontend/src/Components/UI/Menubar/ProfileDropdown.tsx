import { defineComponent, ref, type PropType } from 'vue';

export default defineComponent({
    name: 'ProfileDropdown',
    props: {
        username: { type: String, required: true },
        avatarUrl: { type: String, required: true },
        options: { type: Array as () => Array<{ label: string; action: () => void }>, default: () => [] },
        isMobile: { type: Boolean, default: false },
        onOptionClick: { type: Function as PropType<() => void>, required: false },
    },

    setup(props) {
        const isProfileOpen = ref(false);

        const toggleProfile = () => {
            if (!props.isMobile) {
                isProfileOpen.value = !isProfileOpen.value;
            }
        };

        const handleOptionClick = (action: () => void) => {
            action();
            if (!props.isMobile) {
                isProfileOpen.value = false;
            }
            if (props.onOptionClick) {
                props.onOptionClick();
            }
        };


        return () => (
            <div
                class={`${props.isMobile ? 'ml-0 w-full' : 'relative ml-5'}`}
            >
                <div
                    class={`
                        flex items-center gap-2 rounded-xl 
                        // Desktop
                        py-1 px-2
                        // mobile
                        ${props.isMobile ? 'w-full justify-start py-2 px-3' : 'cursor-pointer transition-colors duration-200 ease-in-out hover:bg-(--theme-450) '}
                    `}
                    onClick={props.isMobile ? undefined : toggleProfile}
                >
                    <img src={props.avatarUrl} class="w-8 h-8 rounded-full object-cover" />
                    <span class="text-white font-bold text-sm">{props.username}</span>
                </div>

                {(isProfileOpen.value || props.isMobile) && (
                    <div
                        class={`
                            rounded shadow-lg min-w-30 z-500 overflow-hidden flex flex-col py-1
                            
                            // Desktop
                            ${!props.isMobile ? 'absolute mt-3 top-full right-0 shadow-lg bg-white' : ''}
                            
                            // Mobile
                            ${props.isMobile ? 'relative top-auto right-auto w-full mt-1 bg-transparent shadow-none' : ''}
                        `}
                    >
                        {props.options.map((option) => (
                            <div
                                class={`
                                    flex items-center gap-2 py-2.5 px-4 text-sm no-underline w-full cursor-pointer 
                                    ${props.isMobile
                                        ? 'text-white hover:bg-(--theme-450) rounded-lg'
                                        : 'text-gray-800 bg-transparent hover:bg-[#f0f0f0]'
                                    }
                                `}
                                onClick={() => handleOptionClick(option.action)}
                            >
                                <span>{option.label}</span>
                            </div>
                        ))}
                    </div>
                )}
            </div>
        );
    },
});