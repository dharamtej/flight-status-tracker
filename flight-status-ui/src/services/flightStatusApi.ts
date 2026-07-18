export type UnifiedFlightStatus = 'OnTime' | 'Delayed' | 'Cancelled' | 'Diverted' | 'Unknown'

export interface FlightStatusResult {
  flightNumber: string
  date: string
  status: UnifiedFlightStatus
  source: 'AeroTrack' | 'QuickFlight' | 'None'
  scheduledDeparture: string
  actualDeparture: string | null
  scheduledArrival: string
  actualArrival: string | null
  terminal: string | null
  gate: string | null
  delayReason: string | null
  lastUpdatedUtc: string | null
  message: string | null
}

interface ProblemDetails {
  detail?: string
  title?: string
}

export class FlightStatusApiError extends Error {}

export async function getFlightStatus(flightNumber: string, date: string): Promise<FlightStatusResult> {
  const params = new URLSearchParams({ flightNumber, date })
  const response = await fetch(`/flights/status?${params.toString()}`)

  if (!response.ok) {
    const problem = (await response.json().catch(() => null)) as ProblemDetails | null
    throw new FlightStatusApiError(
      problem?.detail ?? `Request failed with status ${response.status}.`,
    )
  }

  return (await response.json()) as FlightStatusResult
}
