/** "17:00" -> "17:00:00". Native <input type="time"> gives HH:mm; the API expects HH:mm:ss. */
export function toApiTimeString(value: string): string {
  if (!value) return value;
  return value.length === 5 ? `${value}:00` : value;
}

/** "17:00:00" -> "17:00". <input type="time"> is happy with either, but this keeps values consistent. */
export function toInputTimeString(value: string): string {
  if (!value) return value;
  return value.length >= 5 ? value.slice(0, 5) : value;
}

export const MINUTES_IN_DAY = 24 * 60;

/** "17:30" -> 1050 */
export function toMinutes(time: string): number {
  const [hours, minutes] = time.split(':').map(Number);
  return hours * 60 + minutes;
}

/** 90 -> "1h 30m", 120 -> "2h" */
export function formatMinutesLabel(totalMinutes: number): string {
  const hours = Math.floor(totalMinutes / 60);
  const minutes = totalMinutes % 60;
  return minutes === 0 ? `${hours}h` : `${hours}h ${minutes}m`;
}