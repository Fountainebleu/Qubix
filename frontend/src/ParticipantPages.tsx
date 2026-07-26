import {
  useEffect,
  useMemo,
  useState,
  type FormEvent,
} from 'react'
import { Link, Navigate, useNavigate, useParams } from 'react-router-dom'
import { ApiError, getApiErrorMessage } from './api'
import { useAuth } from './AuthContext'
import { OrganizerHeader } from './OrganizerPages'
import leaderAvatar from './assets/leader-avatar.svg'
import participantAvatar from './assets/participant-avatar.svg'
import podiumFirst from './assets/podium-first.svg'
import podiumOther from './assets/podium-other.svg'
import {
  historyApi,
  quizApi,
  type OrganizerHistoryItem,
  type ParticipantHistoryItem,
  type QuizSession,
  type SessionParticipant,
} from './quizApi'
import { connectToQuizSession } from './quizHub'
import './ParticipantPages.css'

function initials(name: string) {
  return (
    name
      .trim()
      .split(/\s+/)
      .slice(0, 2)
      .map((part) => part[0]?.toUpperCase())
      .join('') || 'Q'
  )
}

function formatHistoryDate(value: string) {
  return new Intl.DateTimeFormat('ru-RU', {
    day: 'numeric',
    month: 'long',
    hour: '2-digit',
    minute: '2-digit',
  }).format(new Date(value))
}

function ConnectionNotice({ message }: { message: string }) {
  return message ? (
    <div className="connection-notice" role="alert">
      {message}
    </div>
  ) : null
}

export function JoinRoomPage() {
  const navigate = useNavigate()
  const [roomCode, setRoomCode] = useState('')
  const [error, setError] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setError('')
    setIsSubmitting(true)

    try {
      const session = await quizApi.joinSession(roomCode)
      navigate(`/play/${session.id}`)
    } catch (requestError) {
      setError(getApiErrorMessage(requestError))
      setIsSubmitting(false)
    }
  }

  return (
    <main className="app-page">
      <OrganizerHeader active="none" />
      <div className="join-container">
        <section className="join-card">
          <span className="eyebrow">Подключение к игре</span>
          <h1>Введите код комнаты</h1>
          <p>Шестизначный код покажет организатор квиза.</p>
          <form onSubmit={handleSubmit}>
            <input
              value={roomCode}
              onChange={(event) =>
                setRoomCode(
                  event.target.value
                    .replace(/[^a-zA-Z0-9]/g, '')
                    .toUpperCase(),
                )
              }
              placeholder="QX7K2M"
              aria-label="Код комнаты"
              minLength={6}
              maxLength={6}
              autoComplete="off"
              required
            />
            {error && (
              <p className="form-error" role="alert">
                {error}
              </p>
            )}
            <button
              className="button button--primary button--full"
              disabled={isSubmitting || roomCode.length !== 6}
            >
              {isSubmitting ? 'Подключаемся…' : 'Подключиться'}
            </button>
          </form>
          <Link to="/history">Посмотреть историю игр</Link>
        </section>
      </div>
    </main>
  )
}

function WaitingScreen({ session }: { session: QuizSession }) {
  return (
    <main className="participant-stage">
      <OrganizerHeader dark active="none" />
      <section className="waiting-screen">
        <span className="waiting-pulse" aria-hidden="true" />
        <span className="eyebrow">Вы в комнате {session.roomCode}</span>
        <h1>Ждём начала квиза</h1>
        <p>Организатор запустит игру, когда все участники подключатся.</p>
        <div className="waiting-participants">
          <strong>{session.participants.length} подключено</strong>
          <div>
            {session.participants.map((participant) => (
              <span key={participant.id} title={participant.displayName}>
                <img src={participantAvatar} alt="" />
                <b>{initials(participant.displayName)}</b>
              </span>
            ))}
          </div>
        </div>
      </section>
    </main>
  )
}

function Leaderboard({
  participants,
  completedQuestions,
  isFinal,
}: {
  participants: SessionParticipant[]
  completedQuestions: number
  isFinal: boolean
}) {
  const podiumOrder = [1, 0, 2]

  return (
    <main className="leaderboard-page">
      <header className="leaderboard-header">
        <strong>Qubix</strong>
        <span>{isFinal ? 'Квиз завершён' : 'Вопрос завершён'}</span>
        <h1>{isFinal ? 'Итоговый лидерборд' : 'Лидерборд'}</h1>
        <p>После {completedQuestions} вопросов</p>
      </header>

      <section className="leaderboard-content">
        {participants.length === 0 ? (
          <div className="leaderboard-empty">Участников пока нет.</div>
        ) : (
          <>
            <div className="podium">
              {podiumOrder.map((participantIndex) => {
                const participant = participants[participantIndex]
                if (!participant) {
                  return null
                }

                const place = participantIndex + 1
                return (
                  <article
                    className={place === 1 ? 'podium-card is-first' : 'podium-card'}
                    key={participant.id}
                    style={{ order: podiumOrder.indexOf(participantIndex) }}
                  >
                    <span className="podium-place">
                      <img
                        src={place === 1 ? podiumFirst : podiumOther}
                        alt=""
                      />
                      <b>{place}</b>
                    </span>
                    <small>{place} место</small>
                    <strong>{participant.displayName}</strong>
                    <span>{participant.score} баллов</span>
                  </article>
                )
              })}
            </div>

            {participants.length > 3 && (
              <div className="leaderboard-list">
                {participants.slice(3).map((participant, index) => (
                  <article key={participant.id}>
                    <span>{index + 4}</span>
                    <span className="leaderboard-avatar">
                      <img src={leaderAvatar} alt="" />
                      <b>{initials(participant.displayName)}</b>
                    </span>
                    <strong>{participant.displayName}</strong>
                    <b>{participant.score} баллов</b>
                  </article>
                ))}
              </div>
            )}

            <div className="leaderboard-actions">
              {isFinal ? (
                <>
                  <Link className="button button--secondary" to="/join">
                    Другой квиз
                  </Link>
                  <Link className="button button--primary" to="/history">
                    История игр
                  </Link>
                </>
              ) : (
                <p>Ждите, пока организатор откроет следующий вопрос.</p>
              )}
            </div>
          </>
        )}
      </section>
    </main>
  )
}

function QuestionScreen({
  session,
  answeredQuestionIds,
  rememberAnswered,
}: {
  session: QuizSession
  answeredQuestionIds: Set<string>
  rememberAnswered: (questionId: string) => void
}) {
  const question = session.openQuestion
  const [selectedOptionIds, setSelectedOptionIds] = useState<string[]>([])
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [error, setError] = useState('')
  const [now, setNow] = useState(Date.now())

  useEffect(() => {
    setSelectedOptionIds([])
    setError('')
  }, [question?.id])

  useEffect(() => {
    const interval = window.setInterval(() => setNow(Date.now()), 250)
    return () => window.clearInterval(interval)
  }, [])

  if (!question) {
    return null
  }

  const secondsLeft = Math.max(
    0,
    Math.ceil((new Date(question.closesAtUtc).getTime() - now) / 1000),
  )
  const hasAnswered = answeredQuestionIds.has(question.id)
  const isLocked = hasAnswered || isSubmitting || secondsLeft === 0
  const progress =
    ((question.position + 1) / Math.max(session.questions.length, 1)) * 100

  const selectOption = (optionId: string) => {
    if (isLocked) {
      return
    }

    if (question.type === 'SingleChoice') {
      setSelectedOptionIds([optionId])
      return
    }

    setSelectedOptionIds((selected) =>
      selected.includes(optionId)
        ? selected.filter((id) => id !== optionId)
        : [...selected, optionId],
    )
  }

  const submit = async () => {
    if (selectedOptionIds.length === 0 || isLocked) {
      return
    }

    setIsSubmitting(true)
    setError('')

    try {
      await quizApi.submitAnswer(
        session.id,
        question.id,
        selectedOptionIds,
      )
      rememberAnswered(question.id)
    } catch (requestError) {
      if (requestError instanceof ApiError && requestError.status === 409) {
        rememberAnswered(question.id)
        setError('Ответ на этот вопрос уже был отправлен.')
      } else {
        setError(getApiErrorMessage(requestError))
      }
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <main className="question-page">
      <header className="question-header">
        <strong>Qubix</strong>
        <span>
          Вопрос {question.position + 1} из {session.questions.length}
        </span>
        <b className={secondsLeft <= 5 ? 'is-ending' : undefined}>
          {secondsLeft}
        </b>
      </header>
      <div className="question-progress">
        <span style={{ width: `${progress}%` }} />
      </div>

      <section className="question-content">
        <span className="eyebrow">Комната {session.roomCode}</span>
        <h1>{question.text || 'Выберите правильный ответ'}</h1>

        {question.imageUrl && (
          <img
            className="question-image"
            src={question.imageUrl}
            alt="Изображение к вопросу"
          />
        )}

        <div className="participant-answers">
          {question.answerOptions.map((option, index) => {
            const selected = selectedOptionIds.includes(option.id)
            return (
              <button
                key={option.id}
                className={selected ? 'is-selected' : undefined}
                onClick={() => selectOption(option.id)}
                disabled={isLocked}
              >
                <span>{String.fromCharCode(65 + index)}</span>
                <strong>{option.text}</strong>
              </button>
            )
          })}
        </div>

        {error && (
          <p className="question-error" role="alert">
            {error}
          </p>
        )}

        <button
          className="button button--primary answer-submit"
          onClick={() => void submit()}
          disabled={isLocked || selectedOptionIds.length === 0}
        >
          {hasAnswered
            ? 'Ответ принят'
            : secondsLeft === 0
              ? 'Время вышло'
              : isSubmitting
                ? 'Отправляем…'
                : 'Ответить'}
        </button>
        <p className="answer-hint">
          {hasAnswered
            ? 'Форма заблокирована. Ждите следующий вопрос.'
            : question.type === 'MultipleChoice'
              ? 'Можно выбрать несколько вариантов'
              : 'Можно выбрать один вариант'}
        </p>
      </section>
    </main>
  )
}

export function ParticipantSessionPage() {
  const { sessionId } = useParams()
  const { user } = useAuth()
  const storageKey = `qubix.answers.${sessionId}.${user?.id}`
  const [session, setSession] = useState<QuizSession | null>(null)
  const [answeredQuestionIds, setAnsweredQuestionIds] = useState<Set<string>>(
    () => {
      try {
        const saved = sessionStorage.getItem(storageKey)
        return new Set<string>(saved ? JSON.parse(saved) : [])
      } catch {
        return new Set<string>()
      }
    },
  )
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState('')
  const [connectionError, setConnectionError] = useState('')
  const hasInitialState = session !== null

  useEffect(() => {
    if (!sessionId) {
      return
    }

    let isActive = true

    const load = async () => {
      try {
        const state = await quizApi.getSession(sessionId)
        if (isActive) {
          setSession(state)
          setError('')
        }
      } catch (requestError) {
        if (isActive) {
          setError(getApiErrorMessage(requestError))
        }
      } finally {
        if (isActive) {
          setIsLoading(false)
        }
      }
    }

    void load()

    return () => {
      isActive = false
    }
  }, [sessionId])

  useEffect(() => {
    if (!sessionId || !hasInitialState) {
      return
    }

    let isActive = true
    let disconnect: (() => Promise<void>) | undefined

    const connect = async () => {
      try {
        const stop = await connectToQuizSession(
          sessionId,
          (state) => {
            if (isActive) {
              setSession(state)
            }
          },
          (message) => {
            if (isActive) {
              setConnectionError(message)
            }
          },
          async () => {
            const state = await quizApi.getSession(sessionId)
            if (isActive) {
              setSession(state)
            }
          },
        )

        if (!isActive) {
          await stop()
          return
        }

        disconnect = stop
        setConnectionError('')
        const refreshedState = await quizApi.getSession(sessionId)
        if (isActive) {
          setSession(refreshedState)
        }
      } catch {
        if (isActive) {
          setConnectionError(
            'Не удалось подключиться к обновлениям комнаты. Обновите страницу.',
          )
        }
      }
    }

    void connect()

    return () => {
      isActive = false
      if (disconnect) {
        void disconnect()
      }
    }
  }, [sessionId, hasInitialState])

  const rememberAnswered = (questionId: string) => {
    setAnsweredQuestionIds((current) => {
      const updated = new Set(current).add(questionId)
      sessionStorage.setItem(storageKey, JSON.stringify([...updated]))
      return updated
    })
  }

  if (!sessionId) {
    return <Navigate to="/join" replace />
  }

  if (isLoading) {
    return <div className="page-loader">Подключаемся к комнате…</div>
  }

  if (!session) {
    return (
      <main className="app-page">
        <OrganizerHeader active="none" />
        <div className="participant-error">
          <h1>Не удалось открыть комнату</h1>
          <p>{error}</p>
          <Link className="button button--secondary" to="/join">
            Ввести другой код
          </Link>
        </div>
      </main>
    )
  }

  if (session.status === 'Waiting') {
    return (
      <>
        <ConnectionNotice message={connectionError} />
        <WaitingScreen session={session} />
      </>
    )
  }

  const completedQuestions = session.questions.filter(
    (question) => question.status === 'Closed',
  ).length

  if (session.status === 'Finished') {
    return (
      <>
        <ConnectionNotice message={connectionError} />
        <Leaderboard
          participants={session.participants}
          completedQuestions={session.questions.length}
          isFinal
        />
      </>
    )
  }

  if (session.openQuestion) {
    return (
      <>
        <ConnectionNotice message={connectionError} />
        <QuestionScreen
          session={session}
          answeredQuestionIds={answeredQuestionIds}
          rememberAnswered={rememberAnswered}
        />
      </>
    )
  }

  if (session.currentQuestionId) {
    return (
      <>
        <ConnectionNotice message={connectionError} />
        <Leaderboard
          participants={session.participants}
          completedQuestions={completedQuestions}
          isFinal={false}
        />
      </>
    )
  }

  return (
    <>
      <ConnectionNotice message={connectionError} />
      <main className="participant-stage">
        <OrganizerHeader dark active="none" />
        <section className="waiting-screen">
          <span className="waiting-pulse" aria-hidden="true" />
          <h1>Квиз начался</h1>
          <p>Организатор готовит первый вопрос.</p>
        </section>
      </main>
    </>
  )
}

export function HistoryPage() {
  const { user } = useAuth()
  const isOrganizer = user?.roles.includes('Organizer') ?? false
  const [participantItems, setParticipantItems] = useState<
    ParticipantHistoryItem[]
  >([])
  const [organizerItems, setOrganizerItems] = useState<OrganizerHistoryItem[]>(
    [],
  )
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    const load = async () => {
      try {
        if (isOrganizer) {
          setOrganizerItems(await historyApi.getOrganizer())
        } else {
          setParticipantItems(await historyApi.getParticipant())
        }
      } catch (requestError) {
        setError(getApiErrorMessage(requestError))
      } finally {
        setIsLoading(false)
      }
    }

    void load()
  }, [isOrganizer])

  const totalScore = useMemo(
    () => participantItems.reduce((sum, item) => sum + item.score, 0),
    [participantItems],
  )
  const wins = useMemo(
    () => participantItems.filter((item) => item.place === 1).length,
    [participantItems],
  )
  const totalParticipants = useMemo(
    () =>
      organizerItems.reduce(
        (sum, item) => sum + item.participantCount,
        0,
      ),
    [organizerItems],
  )

  return (
    <main className="app-page">
      <OrganizerHeader active="history" />
      <div className="history-container">
        <div className="page-heading">
          <span className="eyebrow">Личный кабинет</span>
          <h1>История игр</h1>
          <p>
            {isOrganizer
              ? 'Завершённые квизы организатора'
              : 'Результаты участия в квизах'}
          </p>
        </div>

        {isOrganizer ? (
          <div className="history-stats">
            <div className="stat-card">
              <strong>{organizerItems.length}</strong>
              <span>Игр проведено</span>
            </div>
            <div className="stat-card stat-card--green">
              <strong>{totalParticipants}</strong>
              <span>Всего участников</span>
            </div>
          </div>
        ) : (
          <div className="history-stats">
            <div className="stat-card">
              <strong>{participantItems.length}</strong>
              <span>Игр сыграно</span>
            </div>
            <div className="stat-card">
              <strong>{totalScore}</strong>
              <span>Всего баллов</span>
            </div>
            <div className="stat-card stat-card--green">
              <strong>{wins}</strong>
              <span>Побед</span>
            </div>
          </div>
        )}

        {error && <div className="history-message is-error">{error}</div>}
        {isLoading && <div className="history-message">Загружаем историю…</div>}

        {!isLoading && !error && (
          <div className="history-table">
            <div className="history-row history-row--header">
              <span>Квиз</span>
              <span>Дата</span>
              {isOrganizer ? (
                <span>Участников</span>
              ) : (
                <>
                  <span>Баллы</span>
                  <span>Место</span>
                </>
              )}
            </div>

            {isOrganizer
              ? organizerItems.map((item, index) => (
                  <div className="history-row history-row--organizer" key={`${item.quizTitle}-${item.completedAtUtc}`}>
                    <HistoryQuizTitle title={item.quizTitle} index={index} />
                    <span>{formatHistoryDate(item.completedAtUtc)}</span>
                    <strong>{item.participantCount}</strong>
                  </div>
                ))
              : participantItems.map((item, index) => (
                  <div className="history-row" key={`${item.quizTitle}-${item.completedAtUtc}`}>
                    <HistoryQuizTitle title={item.quizTitle} index={index} />
                    <span>{formatHistoryDate(item.completedAtUtc)}</span>
                    <strong>{item.score}</strong>
                    <span
                      className={
                        item.place === 1
                          ? 'history-place is-winner'
                          : 'history-place'
                      }
                    >
                      {item.place} место
                    </span>
                  </div>
                ))}

            {(isOrganizer ? organizerItems : participantItems).length === 0 && (
              <div className="history-empty">Завершённых игр пока нет.</div>
            )}
          </div>
        )}
      </div>
    </main>
  )
}

function HistoryQuizTitle({
  title,
  index,
}: {
  title: string
  index: number
}) {
  return (
    <span className="history-quiz-title">
      <b className={`history-icon history-icon--${index % 3}`}>
        {index + 1}
      </b>
      <strong>{title}</strong>
    </span>
  )
}
