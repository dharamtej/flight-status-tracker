import type { UnifiedFlightStatus } from '../services/flightStatusApi'

const STATUS_STYLES: Record<UnifiedFlightStatus, string> = {
  OnTime: 'bg-green-100 text-green-800 ring-green-600/20',
  Delayed: 'bg-amber-100 text-amber-800 ring-amber-600/20',
  Cancelled: 'bg-red-100 text-red-800 ring-red-600/20',
  Diverted: 'bg-red-100 text-red-800 ring-red-600/20',
  Unknown: 'bg-gray-100 text-gray-800 ring-gray-500/20',
}

const STATUS_LABELS: Record<UnifiedFlightStatus, string> = {
  OnTime: 'On Time',
  Delayed: 'Delayed',
  Cancelled: 'Cancelled',
  Diverted: 'Diverted',
  Unknown: 'Unknown',
}

function StatusIcon({ status }: { status: UnifiedFlightStatus }) {
  const shared = { className: 'h-3.5 w-3.5', viewBox: '0 0 20 20', fill: 'currentColor', 'aria-hidden': true }

  switch (status) {
    case 'OnTime':
      return (
        <svg {...shared}>
          <path
            fillRule="evenodd"
            d="M16.7 5.3a1 1 0 0 1 0 1.4l-7.5 7.5a1 1 0 0 1-1.4 0L3.3 9.7a1 1 0 1 1 1.4-1.4l3.8 3.8 6.8-6.8a1 1 0 0 1 1.4 0Z"
            clipRule="evenodd"
          />
        </svg>
      )
    case 'Delayed':
      return (
        <svg {...shared}>
          <path
            fillRule="evenodd"
            d="M10 18a8 8 0 1 0 0-16 8 8 0 0 0 0 16Zm.75-12a.75.75 0 0 0-1.5 0v4c0 .2.08.39.22.53l2.5 2.5a.75.75 0 1 0 1.06-1.06l-2.28-2.28V6Z"
            clipRule="evenodd"
          />
        </svg>
      )
    case 'Cancelled':
      return (
        <svg {...shared}>
          <path
            fillRule="evenodd"
            d="M10 18a8 8 0 1 0 0-16 8 8 0 0 0 0 16ZM8.28 7.22a.75.75 0 0 0-1.06 1.06L8.94 10l-1.72 1.72a.75.75 0 1 0 1.06 1.06L10 11.06l1.72 1.72a.75.75 0 1 0 1.06-1.06L11.06 10l1.72-1.72a.75.75 0 0 0-1.06-1.06L10 8.94 8.28 7.22Z"
            clipRule="evenodd"
          />
        </svg>
      )
    case 'Diverted':
      return (
        <svg {...shared}>
          <path
            fillRule="evenodd"
            d="M8.48 3.5a1.5 1.5 0 0 1 3.04 0l6.15 11.2a1.5 1.5 0 0 1-1.32 2.3H3.65a1.5 1.5 0 0 1-1.32-2.3L8.48 3.5ZM10 7a.75.75 0 0 1 .75.75v3.5a.75.75 0 0 1-1.5 0v-3.5A.75.75 0 0 1 10 7Zm0 7a.9.9 0 1 0 0-1.8.9.9 0 0 0 0 1.8Z"
            clipRule="evenodd"
          />
        </svg>
      )
    case 'Unknown':
      return (
        <svg {...shared}>
          <path
            fillRule="evenodd"
            d="M18 10a8 8 0 1 1-16 0 8 8 0 0 1 16 0Zm-7-3.25a1.25 1.25 0 0 0-1.213.953.75.75 0 1 1-1.454-.363A2.75 2.75 0 1 1 11 12.25a.75.75 0 0 0-.75.75v.25a.75.75 0 0 1-1.5 0v-.25A2.25 2.25 0 0 1 11 10.75a1.25 1.25 0 1 0 0-2Zm-.25 8a.875.875 0 1 0 0-1.75.875.875 0 0 0 0 1.75Z"
            clipRule="evenodd"
          />
        </svg>
      )
  }
}

interface StatusBadgeProps {
  status: UnifiedFlightStatus
}

export function StatusBadge({ status }: StatusBadgeProps) {
  return (
    <span
      className={`inline-flex items-center gap-1.5 rounded-full px-3 py-1 text-sm font-medium ring-1 ring-inset ${STATUS_STYLES[status]}`}
    >
      <StatusIcon status={status} />
      {STATUS_LABELS[status]}
    </span>
  )
}
