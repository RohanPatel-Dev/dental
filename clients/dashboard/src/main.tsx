import { QueryClientProvider } from '@tanstack/react-query';
import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter } from 'react-router';
import { AuthProvider } from '@/auth/AuthProvider';
import { loadConfig } from '@/lib/config';
import { queryClient } from '@/lib/queryClient';
import { App } from './App';
import './styles/globals.css';

// Configuration is loaded BEFORE the first render: apiFetch reads it synchronously, so rendering
// first would race every query on the landing page against the config request.
const config = await loadConfig();
document.documentElement.dataset.environment = config.environment;

const container = document.getElementById('root');
if (!container) {
  throw new Error('index.html is missing its #root element.');
}

createRoot(container).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <AuthProvider>
          <App />
        </AuthProvider>
      </BrowserRouter>
    </QueryClientProvider>
  </StrictMode>,
);
