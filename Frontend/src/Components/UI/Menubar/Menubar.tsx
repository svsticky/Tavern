import { defineComponent, ref, type PropType } from "vue";
import type { MenuItem as MenuItemType } from "./Types/MenuItem";
import MenuItem from "./MenuItem";
import type { ProfileOptions } from "./Types/ProfileOptions";
import ProfileDropdown from "./ProfileDropdown";
import { cn } from "@/lib/utils";

export default defineComponent({
    name: "Menubar",
    props: {
        title: { type: String, default: "Sticky" },
        items: {
            type: Array as () => MenuItemType[],
            required: true,
            default: () => [],
        },
        profileOptions: {
            type: Object as PropType<ProfileOptions>,
            required: false,
            default: () => ({ options: [] }),
        },
        class: {
            type: String,
            required: false,
        },
    },

    setup(props) {
        const logo = "https://public.svsticky.nl/logos/hoofd_outline_wit.svg";

        const isMenuOpen = ref(false);

        const toggleMenu = () => {
            isMenuOpen.value = !isMenuOpen.value;
        };

        return () => (
            <header
                class={cn(
                    "bg-(--theme) w-full sticky top-0 z-50 shadow-lg ",
                    props.class,
                )}
            >
                <div class="flex items-center justify-between w-full py-2 px-[10%]">
                    <a
                        href="/"
                        class="flex items-center gap-x-3 text-white cursor-pointer no-underline"
                    >
                        <img src={logo} alt="Logo" class="h-10 w-auto" />
                        <p class="text-white text-2xl font-bold my-0">{props.title}</p>
                    </a>

                    <nav class="hidden lg:flex text-white text-2xl my-0 gap-2 items-center">
                        {props.items.map((item) => {
                            return <MenuItem item={item} />;
                        })}
                    </nav>

                    <div class="flex items-center">
                        {props.profileOptions &&
                            props.profileOptions.username &&
                            props.profileOptions.avatarUrl && (
                                <div class="hidden lg:block">
                                    <ProfileDropdown
                                        username={props.profileOptions.username}
                                        avatarUrl={props.profileOptions.avatarUrl}
                                        options={props.profileOptions.options}
                                    />
                                </div>
                            )}

                        <button
                            class="text-white text-3xl cursor-pointer lg:hidden"
                            onClick={toggleMenu}
                            aria-label="Open menu"
                        >
                            {isMenuOpen.value ? "✕" : "☰"}
                        </button>
                    </div>
                </div>

                {isMenuOpen.value && (
                    <div class="lg:hidden bg-(--theme) py-2 border-t border-opacity-20 border-white">
                        <div class="px-5">
                            <nav class="flex flex-col text-white text-xl gap-1">
                                {props.items.map((item) => (
                                    <MenuItem item={item} class="w-full" onClick={toggleMenu} />
                                ))}
                            </nav>
                        </div>

                        {props.profileOptions &&
                            props.profileOptions.username &&
                            props.profileOptions.avatarUrl && (
                                <div class="mt-2 pt-2 px-5 border-t border-opacity-20 border-white">
                                    <ProfileDropdown
                                        username={props.profileOptions.username}
                                        avatarUrl={props.profileOptions.avatarUrl}
                                        options={props.profileOptions.options}
                                        isMobile={true}
                                        onOptionClick={toggleMenu}
                                    />
                                </div>
                            )}
                    </div>
                )}
            </header>
        );
    },
});
