import { Navigate, Route, Routes } from 'react-router';
import { RequireAuth } from '@/auth/RequireAuth';
import { SignInPage } from '@/auth/SignInPage';
import { AppShell } from '@/components/AppShell';
import { lazyNamed } from '@/lib/lazyNamed';

// Every page is split. lazyNamed keeps the named exports the rest of the codebase uses, so a page
// does not have to grow a default export just to be routed to.
const TenantsPage = lazyNamed(() => import('@/features/tenants/TenantsPage'), 'TenantsPage');
const TenantDetailPage = lazyNamed(
  () => import('@/features/tenants/TenantDetailPage'),
  'TenantDetailPage',
);
const UsersPage = lazyNamed(() => import('@/features/users/UsersPage'), 'UsersPage');

export function App() {
  return (
    <Routes>
      <Route path="/sign-in" element={<SignInPage />} />

      <Route element={<RequireAuth />}>
        <Route element={<AppShell />}>
          <Route index element={<Navigate to="/tenants" replace />} />
          <Route path="tenants" element={<TenantsPage />} />
          <Route path="tenants/:tenantId" element={<TenantDetailPage />} />
          <Route path="users" element={<UsersPage />} />
        </Route>
      </Route>

      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
