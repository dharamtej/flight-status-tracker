import { ErrorState } from './components/ErrorState'
import { ResultCard } from './components/ResultCard'
import { SearchForm } from './components/SearchForm'
import { useFlightStatus } from './hooks/useFlightStatus'

function App() {
  const { data, isLoading, error, search } = useFlightStatus()

  return (
    <div className="flex h-full flex-col items-center overflow-y-auto bg-gradient-to-b from-slate-100 to-slate-50 px-4 py-10">
      <div className="w-full max-w-xl">
        <div className="text-center">
          <h1 className="text-3xl font-bold tracking-tight text-slate-900">
            Flight Status Tracker
          </h1>
          <p className="mt-2 text-sm text-slate-500">
            Look up a flight's status by flight number and date.
          </p>
        </div>

        <div className="mt-6 rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
          <SearchForm onSearch={search} isLoading={isLoading} />
        </div>

        <div className="mt-6">
          {error && <ErrorState message={error} />}
          {data && !error && <ResultCard result={data} />}
        </div>
      </div>
    </div>
  )
}

export default App
