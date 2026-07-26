import { HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr'
import type { QuizSession } from './quizApi'

const sessionEvents = [
  'ParticipantJoined',
  'SessionStarted',
  'QuestionOpened',
  'QuestionClosed',
  'LeaderboardUpdated',
  'SessionFinished',
] as const

export async function connectToQuizSession(
  sessionId: string,
  onStateChanged: (state: QuizSession) => void,
  onConnectionError: (message: string) => void,
  onReconnected: () => Promise<void>,
) {
  const connection = new HubConnectionBuilder()
    .withUrl('/hubs/quiz')
    .withAutomaticReconnect([0, 2000, 5000, 10000])
    .build()

  for (const eventName of sessionEvents) {
    connection.on(eventName, onStateChanged)
  }

  connection.onreconnected(() => {
    void connection
      .invoke('JoinRoom', sessionId)
      .then(async () => {
        onConnectionError('')
        await onReconnected()
      })
      .catch(() =>
        onConnectionError(
          'Соединение восстановлено, но не удалось обновить состояние комнаты.',
        ),
      )
  })

  connection.onclose((error) => {
    if (error) {
      onConnectionError(
        'Соединение с комнатой потеряно. Обновите страницу для восстановления.',
      )
    }
  })

  try {
    await connection.start()
    await connection.invoke('JoinRoom', sessionId)
  } catch (error) {
    await connection.stop()
    throw error
  }

  return async () => {
    for (const eventName of sessionEvents) {
      connection.off(eventName, onStateChanged)
    }

    if (connection.state !== HubConnectionState.Disconnected) {
      await connection.stop()
    }
  }
}
