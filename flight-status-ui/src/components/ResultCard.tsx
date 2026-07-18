import type { FlightStatusResult, UnifiedFlightStatus } from '../services/flightStatusApi'
import { StatusBadge } from './StatusBadge'

interface ResultCardProps {
  result: FlightStatusResult
}

const ACCENT_BAR: Record<UnifiedFlightStatus, string> = {
  OnTime: 'bg-green-500',
  Delayed: 'bg-amber-500',
  Cancelled: 'bg-red-500',
  Diverted: 'bg-red-500',
  Unknown: 'bg-gray-300',
}

function formatDate(value: string): string {
  return new Date(value).toLocaleDateString(undefined, { dateStyle: 'medium' })
}

function formatTimeOnly(value: string): string {
  return new Date(value).toLocaleTimeString(undefined, { timeStyle: 'short' })
}

function PlaneIcon({ className }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" fill="currentColor" className={className} aria-hidden="true">
      <path d="M21 16v-2l-8-5V3.5a1.5 1.5 0 0 0-3 0V9l-8 5v2l8-2.5V19l-2.5 1.5V22l4-1 4 1v-1.5L13 19v-5.5l8 2.5Z" />
    </svg>
  )
}

function TimeLeg({
  label,
  scheduled,
  actual,
  isDelayed,
  align,
}: {
  label: string
  scheduled: string
  actual: string | null
  isDelayed: boolean
  align: 'left' | 'right'
}) {
  return (
    <div className={align === 'right' ? 'text-right' : 'text-left'}>
      <p className="text-xs font-medium uppercase tracking-wide text-slate-400">{label}</p>
      <p className="mt-1 text-xl font-semibold text-slate-900">
        {formatTimeOnly(actual ?? scheduled)}
      </p>
      {actual && (
        <p className={`text-xs ${isDelayed ? 'text-amber-600' : 'text-slate-400'}`}>
          {isDelayed ? 'was ' : 'scheduled '}
          {formatTimeOnly(scheduled)}
        </p>
      )}
    </div>
  )
}

function Chip({
  label,
  value,
  tone = 'slate',
}: {
  label: string
  value: string
  tone?: 'slate' | 'amber'
}) {
  const toneClasses =
    tone === 'amber'
      ? 'border-amber-200 bg-amber-50 text-amber-800'
      : 'border-slate-200 bg-slate-50 text-slate-700'

  return (
    <div className={`rounded-lg border px-3 py-1.5 text-sm ${toneClasses}`}>
      <span className="font-medium">{label}:</span> {value}
    </div>
  )
}

export function ResultCard({ result }: ResultCardProps) {
  // Per spec.md, Message is populated only for the Unknown/neither-responds case, which is
  // also the only case with no real schedule data (ScheduledDeparture/Arrival are sentinels).
  const hasScheduleData = result.message === null
  const hasAeroTrackDetails = Boolean(result.terminal || result.gate || result.delayReason)
  const isDelayed = result.status === 'Delayed'

  return (
    <div className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
      <div className={`h-1.5 ${ACCENT_BAR[result.status]}`} />

      <div className="p-5">
        <div className="flex items-start justify-between gap-3">
          <div>
            <p className="text-xs font-medium uppercase tracking-wide text-slate-400">Flight</p>
            <h2 className="text-2xl font-bold text-slate-900">{result.flightNumber}</h2>
          </div>
          <StatusBadge status={result.status} />
        </div>

        {result.message && (
          <p className="mt-4 rounded-lg bg-slate-50 px-3 py-2 text-sm text-slate-600">
            {result.message}
          </p>
        )}

        {hasScheduleData && (
          <>
            <p className="mt-4 text-sm text-slate-500">{formatDate(result.scheduledDeparture)}</p>

            <div className="mt-2 flex items-center gap-3">
              <TimeLeg
                label="Departure"
                scheduled={result.scheduledDeparture}
                actual={result.actualDeparture}
                isDelayed={isDelayed}
                align="left"
              />

              <div className="flex flex-1 items-center gap-1.5 text-slate-300">
                <div className="h-px flex-1 border-t border-dashed border-slate-300" />
                <PlaneIcon className="h-4 w-4 shrink-0 rotate-90 text-slate-400" />
                <div className="h-px flex-1 border-t border-dashed border-slate-300" />
              </div>

              <TimeLeg
                label="Arrival"
                scheduled={result.scheduledArrival}
                actual={result.actualArrival}
                isDelayed={isDelayed}
                align="right"
              />
            </div>
          </>
        )}

        {hasAeroTrackDetails && (
          <div className="mt-5 flex flex-wrap gap-2 border-t border-slate-100 pt-4">
            {result.terminal && <Chip label="Terminal" value={result.terminal} />}
            {result.gate && <Chip label="Gate" value={result.gate} />}
            {result.delayReason && (
              <Chip label="Delay reason" value={result.delayReason} tone="amber" />
            )}
          </div>
        )}

        {hasScheduleData && (
          <p className="mt-4 border-t border-slate-100 pt-3 text-xs text-slate-400">
            Source: {result.source}
            {result.lastUpdatedUtc &&
              ` · Updated ${formatDate(result.lastUpdatedUtc)}, ${formatTimeOnly(result.lastUpdatedUtc)}`}
          </p>
        )}
      </div>
    </div>
  )
}
