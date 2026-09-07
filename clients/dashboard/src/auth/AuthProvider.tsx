import { useQuery, useQueryClient } from '@tanstack/react-query';
import { createContext, use, useCallback, useEffect, useMemo, useState } from 'react';
import type { ReactNode } from 'react';
import { stopRealtime } from '@/lib/realtime';
import { tokenStore } from '@/lib/tokenStore';
import { fetchCurrentUser, signIn as requestToken, type CurrentUser } from './session';

interface AuthValue {
  readonly currentUser: CurrentUser | null;
  readonly isLoading: boolean;
  readonly signIn: (tenant: string, email: string, password: string) => Promise<void>;
  readonly signOut: () => void;
  /** Permission check. The API decides too - this only keeps unusable controls off the screen. */
  readonly can: (permission: string) => boolean;
}

const AuthContext = createContext<AuthValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient();

  // Token presence is STATE, not a value read during render: signing in writes to localStorage,
  // which React cannot observe, so reading it inline would leave the session query disabled and the
  // user staring at the sign-in screen with a perfectly good token in hand.
  const [hasToken, setHasToken] = useState(() => tokenStore.read() !== null);

  const { data, isLoading } = useQuery({
    queryKey: ['account', 'me'],
    queryFn: fetchCurrentUser,
    enabled: hasToken,
    staleTime: 5 * 60_000,
  });

  const signOut = useCallback(() => {
    tokenStore.clear();
    setHasToken(false);
    queryClient.clear();
    void stopRealtime();
  }, [queryClient]);

  // apiFetch raises this when a refresh fails, which is the only place that knows the session is
  // genuinely over. Without the listener the app would sit on a dead token showing empty screens.
  useEffect(() => {
    const handler = () => signOut();
    window.addEventListener('dental:signed-out', handler);
    return () => window.removeEventListener('dental:signed-out', handler);
  }, [signOut]);

  const value = useMemo<AuthValue>(() => {
    const permissions = new Set(data?.permissions ?? []);

    return {
      currentUser: data ?? null,
      isLoading: hasToken && isLoading,
      signIn: async (tenant, email, password) => {
        await requestToken(tenant, email, password);
        setHasToken(true);
        await queryClient.invalidateQueries({ queryKey: ['account', 'me'] });
      },
      signOut,
      can: (permission) => permissions.has(permission),
    };
  }, [data, hasToken, isLoading, queryClient, signOut]);

  return <AuthContext value={value}>{children}</AuthContext>;
}

export function useAuth(): AuthValue {
  const value = use(AuthContext);

  if (!value) {
    throw new Error('useAuth must be used inside an AuthProvider.');
  }

  return value;
}
