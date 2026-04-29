import { createContext, useContext, useState, type ReactNode } from 'react'
import dayjs, { type Dayjs } from 'dayjs'

interface MonthContextValue {
  month: Dayjs
  setMonth: (m: Dayjs) => void
}

const MonthContext = createContext<MonthContextValue | null>(null)

export function MonthProvider({ children }: { children: ReactNode }) {
  const [month, setMonth] = useState<Dayjs>(dayjs().startOf('month'))
  return (
    <MonthContext.Provider value={{ month, setMonth }}>
      {children}
    </MonthContext.Provider>
  )
}

export function useMonth() {
  const ctx = useContext(MonthContext)
  if (!ctx) throw new Error('useMonth must be used within MonthProvider')
  return ctx
}
