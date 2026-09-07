import { Navigate, Route, Routes } from 'react-router';
import { RequireAuth } from '@/auth/RequireAuth';
import { SignInPage } from '@/auth/SignInPage';
import { AppShell } from '@/components/AppShell';
import { lazyNamed } from '@/lib/lazyNamed';

// Every page is split. lazyNamed keeps the named exports the rest of the codebase uses, so a page
// does not have to grow a default export just to be routed to.
const PatientsPage = lazyNamed(() => import('@/features/patients/PatientsPage'), 'PatientsPage');
const PatientDetailPage = lazyNamed(
  () => import('@/features/patients/PatientDetailPage'),
  'PatientDetailPage',
);
const SchedulePage = lazyNamed(() => import('@/features/schedule/SchedulePage'), 'SchedulePage');
const BillingPage = lazyNamed(() => import('@/features/billing/BillingPage'), 'BillingPage');

export function App() {
  return (
    <Routes>
      <Route path="/sign-in" element={<SignInPage />} />

      <Route element={<RequireAuth />}>
        <Route element={<AppShell />}>
          <Route index element={<Navigate to="/patients" replace />} />
          <Route path="patients" element={<PatientsPage />} />
          <Route path="patients/:patientId" element={<PatientDetailPage />} />
          <Route path="schedule" element={<SchedulePage />} />
          <Route path="billing" element={<BillingPage />} />
        </Route>
      </Route>

      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
