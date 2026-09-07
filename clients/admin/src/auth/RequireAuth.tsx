import { Navigate, Outlet, useLocation } from 'react-router';
import { tokenStore } from '@/lib/tokenStore';
import { useAuth } from './AuthProvider';

/** Route guard. The API enforces the same rules; this only avoids rendering a dead screen. */
export function RequireAuth() {
  const { currentUser, isLoading } = useAuth();
  const location = useLocation();

  if (!tokenStore.read()) {
    return <Navigate to="/sign-in" replace state={{ from: location.pathname }} />;
  }

  if (isLoading) {
    return <p className="p-8 text-muted">Loading your session…</p>;
  }

  if (!currentUser) {
    return <Navigate to="/sign-in" replace />;
  }

  return <Outlet />;
}
