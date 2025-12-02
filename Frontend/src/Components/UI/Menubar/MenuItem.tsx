import { defineComponent, type PropType } from "vue";
import type { MenuItem } from "./Types/MenuItem";

export default defineComponent({
    name: "MenubarItem",
    props: {
        item: { type: Object as PropType<MenuItem>, required: true },
        onClick: { type: Function as PropType<() => void>, required: false },
    },

    setup(props) {
        const IconComponent = props.item.icon;

        const isActive = window.location.pathname === props.item.url;

        const handleClick = () => {
            if (props.onClick) {
                props.onClick();
            }
        };

        return () => (
            <a
                href={props.item.url}
                class={`
                    flex items-center gap-2 text-white font-bold no-underline transition-colors duration-200 ease-in-out 
                    // Mobiele Stijl
                    py-2 px-3 rounded-lg text-lg h-auto w-full justify-start
                    // Desktop Stijl (vanaf 'lg')
                    lg:py-5 lg:px-2 lg:rounded-xl lg:text-sm lg:h-4 lg:w-auto lg:justify-center
                    // Hover & Actieve Stijl
                    hover:bg-(--theme-450) ${isActive ? "bg-(--theme-450)" : ""}
                `}
                onClick={handleClick}
            >
                {IconComponent && <IconComponent />}
                <span>{props.item.label}</span>
            </a>
        );
    },
});
