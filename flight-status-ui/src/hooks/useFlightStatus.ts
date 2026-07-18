import { useCallback, useState } from 'react'
import {
  FlightStatusApiError,
  getFlightStatus,
  type FlightStatusResult,
} from '../services/flightStatusApi'

export interface UseFlightStatusResult {
  data: FlightStatusResult | null
  isLoading: boolean
  error: string | null
  search: (flightNumber: string, date: string) => Promise<void>
}

export function useFlightStatus(): UseFlightStatusResult {
  const [data, setData] = useState<FlightStatusResult | null>(null)
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const search = useCallback(async (flightNumber: string, date: string) => {
    setIsLoading(true)
    setError(null)

    try {
      const result = await getFlightStatus(flightNumber, date)
      setData(result)
    } catch (err) {
      setData(null)
      setError(
        err instanceof FlightStatusApiError
          ? err.message
          : 'Something went wrong. Please try again.',
      )
    } finally {
      setIsLoading(false)
    }
  }, [])

  return { data, isLoading, error, search }
}
