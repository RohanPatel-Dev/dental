/**
 * Reads a text field out of a FormData.
 *
 * `FormData.get` returns `string | File`, and passing a File through String() yields
 * "[object Object]" - a bug that only shows up once somebody adds a file input to the form.
 */
export function text(form: FormData, name: string, fallback = ''): string {
  const value = form.get(name);
  return typeof value === 'string' ? value : fallback;
}

/** True when a checkbox was ticked. */
export function checked(form: FormData, name: string): boolean {
  return form.get(name) === 'on';
}
