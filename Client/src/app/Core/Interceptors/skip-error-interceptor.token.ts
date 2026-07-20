import { HttpContextToken } from '@angular/common/http';

/**
 * Set to true on a request's HttpContext to suppress the global error
 * snackbar in errorInterceptor for a 404 on that specific call — used when
 * the caller treats "not found" as a normal, expected outcome rather than
 * an error (e.g. a doctor who hasn't created a working schedule yet).
 */
export const SKIP_ERROR_SNACKBAR = new HttpContextToken<boolean>(() => false);
