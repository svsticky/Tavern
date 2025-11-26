type ProfileOption = { label: string; action: () => void };

export interface ProfileOptions {
    username?: string;
    avatarUrl?: string;
    options: ProfileOption[];
}