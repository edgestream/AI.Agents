"use client";

import { useState, useEffect, useCallback } from "react";

/**
 * User information from the authentication system.
 */
export interface UserInfo {
  authenticated: boolean;
  userId?: string;
  displayName?: string;
  email?: string;
  picture?: string;
  tenantId?: string;
  domain?: string;
}

/**
 * Hook for accessing the current user's authentication state.
 */
export function useUser() {
  const [user, setUser] = useState<UserInfo | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchUser = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const response = await fetch("/api/me");
      if (!response.ok) {
        throw new Error(`Failed to fetch user info: ${response.status}`);
      }
      const data: UserInfo = await response.json();
      setUser(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Unknown error");
      setUser({ authenticated: false });
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchUser();
  }, [fetchUser]);

  return {
    user,
    loading,
    error,
    refetch: fetchUser,
  };
}
