import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr'

const hubPath = '/hubs/budget-month-updates'
const monthUpdatedEvent = 'MonthUpdated'
const subscribeMethod = 'SubscribeToMonth'
const unsubscribeMethod = 'UnsubscribeFromMonth'

export interface MonthUpdatesHubClient {
  start(initialMonth: string): Promise<void>
  setMonth(month: string): Promise<void>
  stop(): Promise<void>
}

function getHubUrl(): string {
  const baseUrl = (__API_BASE_URL__ || '').replace(/\/$/, '')
  return `${baseUrl}${hubPath}`
}

export function createMonthUpdatesHubClient(
  getAccessToken: () => Promise<string>,
  onMonthUpdated: (month: string) => void | Promise<void>,
): MonthUpdatesHubClient {
  let connection: HubConnection | null = null
  let desiredMonth: string | null = null
  let activeMonth: string | null = null

  function ensureConnection(): HubConnection {
    if (connection) return connection

    connection = new HubConnectionBuilder()
      .withUrl(getHubUrl(), {
        accessTokenFactory: getAccessToken,
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build()

    connection.on(monthUpdatedEvent, (month: string) => {
      void onMonthUpdated(month)
    })
    connection.onreconnected(() => {
      if (!connection || !desiredMonth) return
      void connection.invoke(subscribeMethod, desiredMonth)
      activeMonth = desiredMonth
    })

    return connection
  }

  async function setMonth(month: string): Promise<void> {
    desiredMonth = month
    if (!connection || connection.state !== 'Connected') return

    if (activeMonth === month) return
    if (activeMonth) {
      await connection.invoke(unsubscribeMethod, activeMonth)
    }

    await connection.invoke(subscribeMethod, month)
    activeMonth = month
  }

  async function start(initialMonth: string): Promise<void> {
    const hubConnection = ensureConnection()
    desiredMonth = initialMonth

    if (hubConnection.state === 'Disconnected') {
      await hubConnection.start()
    }

    await setMonth(initialMonth)
  }

  async function stop(): Promise<void> {
    const hubConnection = connection
    desiredMonth = null
    activeMonth = null
    connection = null
    if (!hubConnection) return

    await hubConnection.stop()
  }

  return {
    start,
    setMonth,
    stop,
  }
}
