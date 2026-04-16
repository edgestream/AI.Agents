"use client";

import { useUser } from "../hooks/useUser";

/**
 * Props for the UserAvatar component.
 */
interface UserAvatarProps {
  /**
   * Size of the avatar in pixels.
   */
  size?: number;
  /**
   * Whether to show the user's name next to the avatar.
   */
  showName?: boolean;
}

/**
 * Generates initials from a display name.
 */
function getInitials(name: string | undefined): string {
  if (!name) return "?";
  const parts = name.split(/[\s@]+/);
  if (parts.length >= 2) {
    return (parts[0][0] + parts[parts.length > 2 ? 1 : parts.length - 1][0]).toUpperCase();
  }
  return name.substring(0, 2).toUpperCase();
}

/**
 * Generates a consistent color from a string (for avatar background).
 */
function stringToColor(str: string): string {
  let hash = 0;
  for (let i = 0; i < str.length; i++) {
    hash = str.charCodeAt(i) + ((hash << 5) - hash);
  }
  const hue = hash % 360;
  return `hsl(${hue}, 70%, 50%)`;
}

/**
 * Displays the current user's avatar and optionally their name.
 */
export function UserAvatar({ size = 32, showName = false }: UserAvatarProps) {
  const { user, loading } = useUser();

  if (loading) {
    return (
      <div className="flex items-center gap-2 animate-pulse">
        <div
          className="rounded-full bg-gray-300"
          style={{ width: size, height: size }}
        />
        {showName && <div className="h-4 w-20 bg-gray-300 rounded" />}
      </div>
    );
  }

  if (!user?.authenticated) {
    return (
      <a
        href="/.auth/login/aad"
        className="flex items-center gap-2 text-sm text-gray-600 hover:text-gray-900"
      >
        <div
          className="rounded-full bg-gray-200 flex items-center justify-center text-gray-500"
          style={{ width: size, height: size, fontSize: size * 0.4 }}
        >
          ?
        </div>
        {showName && <span>Sign in</span>}
      </a>
    );
  }

  const initials = getInitials(user.displayName || user.email);
  const bgColor = stringToColor(user.userId || user.email || "user");

  return (
    <div className="flex items-center gap-2">
      {user.picture ? (
        // eslint-disable-next-line @next/next/no-img-element
        <img
          src={user.picture}
          alt={user.displayName || "User avatar"}
          className="rounded-full"
          style={{ width: size, height: size }}
        />
      ) : (
        <div
          className="rounded-full flex items-center justify-center text-white font-medium"
          style={{
            width: size,
            height: size,
            fontSize: size * 0.4,
            backgroundColor: bgColor,
          }}
        >
          {initials}
        </div>
      )}
      {showName && (
        <span className="text-sm text-gray-700">
          {user.displayName || user.email}
        </span>
      )}
    </div>
  );
}

/**
 * Displays user menu with sign out option.
 */
export function UserMenu() {
  const { user, loading } = useUser();

  if (loading) {
    return (
      <div className="flex items-center gap-2 animate-pulse">
        <div className="w-8 h-8 rounded-full bg-gray-300" />
      </div>
    );
  }

  if (!user?.authenticated) {
    return (
      <a
        href="/.auth/login/aad"
        className="px-4 py-2 text-sm font-medium text-white bg-blue-600 rounded-md hover:bg-blue-700"
      >
        Sign in
      </a>
    );
  }

  return (
    <div className="flex items-center gap-3">
      <UserAvatar size={32} showName={true} />
      <a
        href="/.auth/logout"
        className="text-sm text-gray-500 hover:text-gray-700"
      >
        Sign out
      </a>
    </div>
  );
}
