import { config } from './config';
import { tokenStore } from './tokenStore';

/**
 * SignalR, loaded only when a screen actually subscribes.
 *
 * The client is ~40 KB of the bundle and most sessions never open a live screen, so the import is
 * dynamic: a static import would put it in the entry chunk and slow down first paint for everybody.
 */
// A type-only import is erased at build time, so naming the type here does NOT pull the client
// into the entry chunk - only the dynamic import below loads it.
import type { HubConnection } from '@microsoft/signalr';

let connection: HubConnection | null = null;
let connecting: Promise<HubConnection | null> | null = null;

async function connect(): Promise<HubConnection | null> {
  if (!config().features.realtime) {
    return null;
  }

  const stored = tokenStore.read();
  if (!stored) {
    return null;
  }

  const signalR = await import('@microsoft/signalr');

  const built = new signalR.HubConnectionBuilder()
    .withUrl(config().realtimeUrl, { accessTokenFactory: () => tokenStore.read()?.accessToken ?? '' })
    .withAutomaticReconnect()
    .configureLogging(signalR.LogLevel.Warning)
    .build();

  await built.start();
  connection = built;
  return built;
}

/**
 * Subscribes to a hub event.
 *
 * @returns a function that removes the handler. The connection itself is shared and stays open.
 */
export function onRealtime(event: string, handler: (payload: unknown) => void): () => void {
  let removed = false;

  connecting ??= connect();

  void connecting.then((hub) => {
    if (hub && !removed) {
      hub.on(event, handler);
    }
  });

  return () => {
    removed = true;
    connection?.off(event, handler);
  };
}

/** Closes the shared connection. Called on sign-out so the next user does not inherit it. */
export async function stopRealtime(): Promise<void> {
  const open = connection;
  connection = null;
  connecting = null;
  await open?.stop();
}
