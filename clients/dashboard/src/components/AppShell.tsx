import { NavLink, Outlet } from 'react-router';
import { Suspense } from 'react';
import { useAuth } from '@/auth/AuthProvider';

const navigation: ReadonlyArray<{ to: string; label: string; permission?: string }> = [
  { to: '/patients', label: 'Patients', permission: 'Permissions.Patients.Search' },
  { to: '/schedule', label: 'Diary', permission: 'Permissions.Appointments.Search' },
  { to: '/billing', label: 'Billing', permission: 'Permissions.Invoices.Search' },
];

/** Frame around every signed-in screen: brand, navigation, current user, sign-out. */
export function AppShell() {
  const { currentUser, signOut, can } = useAuth();

  return (
    <div className="flex min-h-full flex-col">
      <header className="border-b border-border bg-surface">
        <div className="mx-auto flex max-w-6xl items-center gap-6 px-4 py-3">
          <span className="font-semibold tracking-tight">Dental Practice</span>

          <nav className="flex gap-1" aria-label="Main">
            {navigation
              .filter((item) => !item.permission || can(item.permission))
              .map((item) => (
                <NavLink
                  key={item.to}
                  to={item.to}
                  className={({ isActive }) =>
                    `rounded-md px-3 py-1.5 text-sm ${isActive ? 'bg-accent text-accent-ink' : 'hover:bg-canvas'}`
                  }
                >
                  {item.label}
                </NavLink>
              ))}
          </nav>

          <div className="ml-auto flex items-center gap-3 text-sm">
            <span className="text-muted">{currentUser?.user.fullName}</span>
            <button type="button" className="btn-ghost" onClick={signOut}>
              Sign out
            </button>
          </div>
        </div>
      </header>

      <main className="mx-auto w-full max-w-6xl flex-1 p-4">
        <Suspense fallback={<p className="text-muted">Loading…</p>}>
          <Outlet />
        </Suspense>
      </main>
    </div>
  );
}
