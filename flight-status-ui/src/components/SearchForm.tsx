import { useState, type FormEvent } from 'react'
import { DatePicker } from './DatePicker'

interface SearchFormProps {
  onSearch: (flightNumber: string, date: string) => void
  isLoading: boolean
}

export function SearchForm({ onSearch, isLoading }: SearchFormProps) {
  const [flightNumber, setFlightNumber] = useState('')
  const [date, setDate] = useState('')
  const [dateError, setDateError] = useState(false)

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    if (!date) {
      setDateError(true)
      return
    }

    setDateError(false)
    onSearch(flightNumber.trim(), date)
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-4 sm:flex-row sm:items-end">
      <div className="flex flex-1 flex-col gap-1.5">
        <label
          htmlFor="flightNumber"
          className="text-xs font-medium uppercase tracking-wide text-slate-400"
        >
          Flight number
        </label>
        <input
          id="flightNumber"
          type="text"
          value={flightNumber}
          onChange={(event) => setFlightNumber(event.target.value)}
          placeholder="e.g. SR100"
          required
          className="rounded-lg border border-slate-300 px-3 py-2.5 text-slate-900 transition-colors focus:border-slate-500 focus:outline-none focus:ring-2 focus:ring-slate-100"
        />
      </div>

      <div className="flex flex-1 flex-col gap-1.5">
        <label htmlFor="date" className="text-xs font-medium uppercase tracking-wide text-slate-400">
          Date
        </label>
        <DatePicker
          id="date"
          value={date}
          onChange={(value) => {
            setDate(value)
            setDateError(false)
          }}
        />
        {dateError && <p className="text-xs text-red-600">Please select a date.</p>}
      </div>

      <button
        type="submit"
        disabled={isLoading}
        className="cursor-pointer rounded-lg bg-slate-900 px-5 py-2.5 font-medium text-white shadow-sm transition-colors hover:bg-slate-700 disabled:cursor-not-allowed disabled:opacity-50"
      >
        {isLoading ? 'Searching…' : 'Search'}
      </button>
    </form>
  )
}
