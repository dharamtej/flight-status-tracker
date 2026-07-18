interface ErrorStateProps {
  message: string
}

export function ErrorState({ message }: ErrorStateProps) {
  return (
    <div role="alert" className="rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-800">
      <p className="font-medium">Couldn't fetch flight status</p>
      <p className="mt-1">{message}</p>
    </div>
  )
}
