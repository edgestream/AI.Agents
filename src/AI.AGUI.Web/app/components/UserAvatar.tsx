"use client";

import { useUser, type UserInfo } from "../hooks/useUser";

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

function getDirectoryLabel(user: UserInfo): string | undefined {
  if (!user.tenantId) {
    return undefined;
  }

  const domainPrefix = user.domain?.split(".")[0]?.trim();
  const directoryName = domainPrefix
    ? domainPrefix.charAt(0).toUpperCase() + domainPrefix.slice(1)
    : "Tenant";

  return `${directoryName} (${user.tenantId})`;
}

function getProfileDetails(user: UserInfo): Array<{ label: string; value: string }> {
  const profileDetails: Array<{ label: string; value: string }> = [];

  if (user.displayName) {
    profileDetails.push({ label: "Name", value: user.displayName });
  }

  if (user.email) {
    profileDetails.push({ label: "E-Mail", value: user.email });
  }

  if (user.userId) {
    profileDetails.push({ label: "Benutzer-ID", value: user.userId });
  }

  const directoryLabel = getDirectoryLabel(user);
  if (directoryLabel) {
    profileDetails.push({ label: "Verzeichnis", value: directoryLabel });
  }

  if (user.domain) {
    profileDetails.push({ label: "Domäne", value: user.domain });
  }

  return profileDetails;
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
  const profileDetails = getProfileDetails(user);

  const avatarContent = user.picture ? (
    // eslint-disable-next-line @next/next/no-img-element
    <img
      src={user.picture}
      alt={user.displayName || "User avatar"}
      className="rounded-full object-cover"
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
  );

  return (
    <div className="flex items-center gap-2">
      <div className="group relative" tabIndex={0}>
        {avatarContent}
        {profileDetails.length > 0 ? (
          <div className="pointer-events-none absolute right-0 top-full z-20 mt-3 w-max min-w-[320px] max-w-[440px] translate-y-1 rounded-lg border border-slate-700 bg-slate-900 px-4 py-3 text-left text-sm text-white opacity-0 shadow-2xl transition duration-150 group-hover:translate-y-0 group-hover:opacity-100 group-focus-within:translate-y-0 group-focus-within:opacity-100">
            <div className="space-y-1.5 leading-6">
              {profileDetails.map((detail) => (
                <p key={detail.label}>
                  <span className="text-slate-300">{detail.label}:</span>{" "}
                  <span>{detail.value}</span>
                </p>
              ))}
            </div>
          </div>
        ) : null}
      </div>
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
    <div className="flex items-center">
      <UserAvatar size={44} />
    </div>
  );
}
