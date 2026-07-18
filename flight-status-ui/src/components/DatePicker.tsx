import { useEffect, useRef, useState } from 'react'
import { DayPicker } from 'react-day-picker'

interface DatePickerProps {
  id: string
  value: string
  onChange: (value: string) => void
}

function toDateOnlyString(date: Date): string {
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

function fromDateOnlyString(value: string): Date | undefined {
  const [year, month, day] = value.split('-').map(Number)
  if (!year || !month || !day) return undefined
  return new Date(year, month - 1, day)
}

export function DatePicker({ id, value, onChange }: DatePickerProps) {
  const [isOpen, setIsOpen] = useState(false)
  const containerRef = useRef<HTMLDivElement>(null)
  const selected = value ? fromDateOnlyString(value) : undefined

  useEffect(() => {
    if (!isOpen) return

    function handlePointerDown(event: MouseEvent) {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        setIsOpen(false)
      }
    }

    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') setIsOpen(false)
    }

    document.addEventListener('mousedown', handlePointerDown)
    document.addEventListener('keydown', handleKeyDown)
    return () => {
      document.removeEventListener('mousedown', handlePointerDown)
      document.removeEventListener('keydown', handleKeyDown)
    }
  }, [isOpen])

  return (
    <div ref={containerRef} className="relative">
      <button
        id={id}
        type="button"
        onClick={() => setIsOpen((open) => !open)}
        aria-haspopup="dialog"
        aria-expanded={isOpen}
        className="flex w-full cursor-pointer items-center justify-between rounded-lg border border-slate-300 px-3 py-2.5 text-left text-slate-900 transition-colors focus:border-slate-500 focus:outline-none focus:ring-2 focus:ring-slate-100"
      >
        <span className={selected ? '' : 'text-slate-400'}>
          {selected
            ? selected.toLocaleDateString(undefined, { dateStyle: 'medium' })
            : 'Select date'}
        </span>
        <svg
          className="h-4 w-4 shrink-0 text-slate-400"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          strokeWidth="2"
          aria-hidden="true"
        >
          <rect x="3" y="4" width="18" height="18" rx="2" />
          <path d="M3 10h18M8 2v4M16 2v4" />
        </svg>
      </button>

      {isOpen && (
        <div
          role="dialog"
          className="absolute z-10 mt-2 rounded-lg border border-slate-200 bg-white p-2 shadow-lg"
        >
          <DayPicker
            mode="single"
            selected={selected}
            onSelect={(date) => {
              if (date) {
                onChange(toDateOnlyString(date))
                setIsOpen(false)
              }
            }}
          />
        </div>
      )}
    </div>
  )
}
