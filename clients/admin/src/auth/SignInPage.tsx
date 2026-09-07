import { useState } from 'react';
import type { FormEvent } from 'react';
import { useNavigate } from 'react-router';
import { ErrorState } from '@/components/ErrorState';
import { config } from '@/lib/config';
import { useAuth } from './AuthProvider';

/** Credentials in, session out. The tenant is part of the credentials: the API resolves on it. */
export function SignInPage() {
  const { signIn } = useAuth();
  const navigate = useNavigate();

  const [tenant, setTenant] = useState(config().tenant ?? '');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<unknown>(null);
  const [busy, setBusy] = useState(false);

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setBusy(true);

    try {
      await signIn(tenant.trim(), email.trim(), password);
      await navigate('/', { replace: true });
    } catch (failure) {
      setError(failure);
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="mx-auto flex min-h-full max-w-sm flex-col justify-center gap-4 p-6">
      <h1 className="text-xl font-semibold">Operator console</h1>

      <form className="card flex flex-col gap-3 p-4" onSubmit={(event) => void submit(event)}>
        <div>
          <label className="label" htmlFor="tenant">
            Practice
          </label>
          <input
            id="tenant"
            className="field"
            autoComplete="organization"
            placeholder="root"
            value={tenant}
            onChange={(event) => setTenant(event.target.value)}
            required
          />
        </div>

        <div>
          <label className="label" htmlFor="email">
            Email
          </label>
          <input
            id="email"
            className="field"
            type="email"
            autoComplete="username"
            value={email}
            onChange={(event) => setEmail(event.target.value)}
            required
          />
        </div>

        <div>
          <label className="label" htmlFor="password">
            Password
          </label>
          <input
            id="password"
            className="field"
            type="password"
            autoComplete="current-password"
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            required
          />
        </div>

        {error !== null && <ErrorState error={error} />}

        <button className="btn-primary" type="submit" disabled={busy}>
          {busy ? 'Signing in…' : 'Sign in'}
        </button>
      </form>
    </div>
  );
}
