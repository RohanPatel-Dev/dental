import { QueryClient } from '@tanstack/react-query';
import { ApiError } from './problem';

/**
 * One client for the app.
 *
 * The retry rule is the important part: a 4xx is the caller's answer, not a blip, and retrying it
 * three times turns one rejected request into four and triples the rate-limit pressure behind it.
 */
export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      refetchOnWindowFocus: false,
      retry: (failureCount, error) => {
        if (error instanceof ApiError && error.status < 500) {
          return false;
        }

        return failureCount < 2;
      },
    },
    mutations: { retry: false },
  },
});
