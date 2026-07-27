import {
  useEffect,
  useMemo,
  useState,
  type FormEvent,
  type ReactNode,
} from 'react'
import { Link, Navigate, useNavigate, useParams } from 'react-router-dom'
import { ApiError, getApiErrorMessage } from './api'
import { useAuth } from './AuthContext'
import organizerAvatar from './assets/organizer-avatar.svg'
import participantAvatar from './assets/participant-avatar.svg'
import {
  quizApi,
  type AnswerOption,
  type QuestionDetails,
  type QuestionType,
  type Quiz,
  type QuizDetails,
  type QuizQuestion,
  type QuizSession,
  type QuizStatus,
} from './quizApi'
import { connectToQuizSession } from './quizHub'
import './OrganizerPages.css'

const statusLabels: Record<QuizStatus, string> = {
  Draft: 'Черновик',
  Published: 'Опубликован',
  Archived: 'Архив',
}

const emptyQuizDetails: QuizDetails = {
  title: '',
  description: '',
  category: '',
  rules: '',
  defaultQuestionTimeSeconds: 30,
}

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

function formatDate(value: string) {
  return new Intl.DateTimeFormat('ru-RU', {
    day: 'numeric',
    month: 'long',
  }).format(new Date(value))
}

export function OrganizerHeader({
  dark = false,
  active = 'quizzes',
}: {
  dark?: boolean
  active?: 'quizzes' | 'history' | 'none'
}) {
  const { user, logout } = useAuth()
  const navigate = useNavigate()
  const [isLoggingOut, setIsLoggingOut] = useState(false)
  const [error, setError] = useState('')

  const handleLogout = async () => {
    setIsLoggingOut(true)
    setError('')

    try {
      await logout()
      navigate('/login', { replace: true })
    } catch (requestError) {
      setError(getApiErrorMessage(requestError))
      setIsLoggingOut(false)
    }
  }

  return (
    <header className={dark ? 'app-header app-header--dark' : 'app-header'}>
      <Link className="app-logo" to="/quizzes">
        <span>Q</span>
        Qubix
      </Link>
      {!dark && (
        <nav className="app-navigation" aria-label="Основная навигация">
          {user?.roles.includes('Organizer') && (
            <Link
              className={active === 'quizzes' ? 'is-active' : undefined}
              to="/quizzes"
            >
              Квизы
            </Link>
          )}
          <Link
            className={active === 'history' ? 'is-active' : undefined}
            to="/history"
          >
            История
          </Link>
        </nav>
      )}
      <div className="app-account">
        {dark && user?.roles.includes('Organizer') && (
          <span className="organizer-pill">Организатор</span>
        )}
        {error && <span className="header-error">{error}</span>}
        <span className="avatar" title={user?.displayName}>
          <img src={organizerAvatar} alt="" />
          <b>{initials(user?.displayName ?? '')}</b>
        </span>
        <button onClick={handleLogout} disabled={isLoggingOut}>
          {isLoggingOut ? 'Выходим…' : 'Выйти'}
        </button>
      </div>
    </header>
  )
}

function PageMessage({
  children,
  error = false,
}: {
  children: ReactNode
  error?: boolean
}) {
  return (
    <div className={error ? 'page-message page-message--error' : 'page-message'}>
      {children}
    </div>
  )
}

export function QuizListPage() {
  const navigate = useNavigate()
  const [quizzes, setQuizzes] = useState<Quiz[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [busyId, setBusyId] = useState('')
  const [error, setError] = useState('')

  useEffect(() => {
    const load = async () => {
      try {
        setQuizzes(await quizApi.getMine())
      } catch (requestError) {
        setError(getApiErrorMessage(requestError))
      } finally {
        setIsLoading(false)
      }
    }

    void load()
  }, [])

  const archiveQuiz = async (quizId: string) => {
    setBusyId(quizId)
    setError('')

    try {
      const updated = await quizApi.archive(quizId)
      setQuizzes((items) =>
        items.map((quiz) => (quiz.id === updated.id ? updated : quiz)),
      )
    } catch (requestError) {
      setError(getApiErrorMessage(requestError))
    } finally {
      setBusyId('')
    }
  }

  const restoreQuiz = async (quizId: string) => {
    setBusyId(quizId)
    setError('')

    try {
      const updated = await quizApi.restore(quizId)
      setQuizzes((items) =>
        items.map((quiz) => (quiz.id === updated.id ? updated : quiz)),
      )
    } catch (requestError) {
      setError(getApiErrorMessage(requestError))
    } finally {
      setBusyId('')
    }
  }

  const launchQuiz = async (quizId: string) => {
    setBusyId(quizId)
    setError('')

    try {
      const session = await quizApi.createSession(quizId)
      navigate(`/sessions/${session.id}`)
    } catch (requestError) {
      setError(getApiErrorMessage(requestError))
      setBusyId('')
    }
  }

  const publishedCount = quizzes.filter(
    (quiz) => quiz.status === 'Published',
  ).length

  return (
    <main className="app-page">
      <OrganizerHeader />
      <div className="page-container">
        <div className="page-heading page-heading--with-action">
          <div>
            <span className="eyebrow">Кабинет организатора</span>
            <h1>Мои квизы</h1>
            <p>Создавайте, публикуйте и запускайте игры</p>
          </div>
          <Link className="button button--primary" to="/quizzes/new">
            + Создать квиз
          </Link>
        </div>

        <div className="stats-row">
          <div className="stat-card">
            <strong>{quizzes.length}</strong>
            <span>Всего квизов</span>
          </div>
          <div className="stat-card stat-card--green">
            <strong>{publishedCount}</strong>
            <span>Опубликовано</span>
          </div>
        </div>

        {error && <PageMessage error>{error}</PageMessage>}
        {isLoading && <PageMessage>Загружаем квизы…</PageMessage>}

        {!isLoading && quizzes.length === 0 && (
          <PageMessage>
            <h2>Квизов пока нет</h2>
            <p>Создайте первый квиз и добавьте в него вопросы.</p>
          </PageMessage>
        )}

        <div className="quiz-list">
          {quizzes.map((quiz, index) => (
            <article className="quiz-row" key={quiz.id}>
              <div className={`quiz-cover quiz-cover--${index % 3}`}>
                {String(index + 1).padStart(2, '0')}
              </div>
              <div className="quiz-row__main">
                <h2>{quiz.title}</h2>
                <p>{quiz.category || 'Без категории'}</p>
              </div>
              <div className="quiz-row__status">
                <span className={`status-pill status-pill--${quiz.status}`}>
                  {statusLabels[quiz.status]}
                </span>
                <small>Обновлён {formatDate(quiz.updatedAtUtc)}</small>
              </div>
              <div className="quiz-row__actions">
                {quiz.status === 'Published' ? (
                  <button
                    className="button button--primary button--compact"
                    onClick={() => launchQuiz(quiz.id)}
                    disabled={busyId === quiz.id}
                  >
                    Запустить
                  </button>
                ) : (
                  <Link
                    className="button button--secondary button--compact"
                    to={`/quizzes/${quiz.id}`}
                  >
                    {quiz.status === 'Draft' ? 'Редактировать' : 'Открыть'}
                  </Link>
                )}
                {quiz.status !== 'Archived' && (
                  <button
                    className="text-button text-button--danger"
                    onClick={() => archiveQuiz(quiz.id)}
                    disabled={busyId === quiz.id}
                  >
                    В архив
                  </button>
                )}
                {quiz.status === 'Archived' && (
                  <button
                    className="text-button"
                    onClick={() => restoreQuiz(quiz.id)}
                    disabled={busyId === quiz.id}
                  >
                    Вернуть в черновик
                  </button>
                )}
              </div>
            </article>
          ))}
        </div>
      </div>
    </main>
  )
}

export function CreateQuizPage() {
  const navigate = useNavigate()
  const [form, setForm] = useState<QuizDetails>(emptyQuizDetails)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [error, setError] = useState('')

  const update = <Key extends keyof QuizDetails>(
    key: Key,
    value: QuizDetails[Key],
  ) => setForm((current) => ({ ...current, [key]: value }))

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setIsSubmitting(true)
    setError('')

    try {
      const quiz = await quizApi.create(form)
      navigate(`/quizzes/${quiz.id}`, { replace: true })
    } catch (requestError) {
      setError(getApiErrorMessage(requestError))
      setIsSubmitting(false)
    }
  }

  return (
    <main className="app-page">
      <OrganizerHeader />
      <div className="narrow-container">
        <Link className="back-link" to="/quizzes">
          ← Мои квизы
        </Link>
        <div className="page-heading">
          <span className="eyebrow">Новый квиз</span>
          <h1>Создать квиз</h1>
          <p>Укажите основные настройки. Вопросы добавляются после создания.</p>
        </div>
        <form className="settings-card" onSubmit={handleSubmit}>
          <QuizDetailsFields form={form} update={update} />
          {error && <p className="form-error">{error}</p>}
          <div className="form-actions">
            <Link className="button button--secondary" to="/quizzes">
              Отмена
            </Link>
            <button
              className="button button--primary"
              disabled={isSubmitting}
            >
              {isSubmitting ? 'Создаём…' : 'Создать и добавить вопросы'}
            </button>
          </div>
        </form>
      </div>
    </main>
  )
}

function QuizDetailsFields({
  form,
  update,
  disabled = false,
}: {
  form: QuizDetails
  update: <Key extends keyof QuizDetails>(
    key: Key,
    value: QuizDetails[Key],
  ) => void
  disabled?: boolean
}) {
  return (
    <div className="settings-grid">
      <label className="form-field settings-grid__wide">
        <span>Название</span>
        <input
          value={form.title}
          onChange={(event) => update('title', event.target.value)}
          maxLength={150}
          disabled={disabled}
          required
        />
      </label>
      <label className="form-field">
        <span>Категория</span>
        <input
          value={form.category}
          onChange={(event) => update('category', event.target.value)}
          maxLength={100}
          disabled={disabled}
          placeholder="Например, музыка"
        />
      </label>
      <label className="form-field">
        <span>Время на вопрос по умолчанию</span>
        <input
          type="number"
          value={form.defaultQuestionTimeSeconds}
          onChange={(event) =>
            update('defaultQuestionTimeSeconds', Number(event.target.value))
          }
          min={5}
          max={300}
          disabled={disabled}
          required
        />
      </label>
      <label className="form-field settings-grid__wide">
        <span>Описание</span>
        <textarea
          value={form.description}
          onChange={(event) => update('description', event.target.value)}
          maxLength={1000}
          disabled={disabled}
          rows={3}
        />
      </label>
      <label className="form-field settings-grid__wide">
        <span>Правила проведения</span>
        <textarea
          value={form.rules}
          onChange={(event) => update('rules', event.target.value)}
          maxLength={2000}
          disabled={disabled}
          rows={3}
        />
      </label>
    </div>
  )
}

function toQuizDetails(quiz: Quiz): QuizDetails {
  return {
    title: quiz.title,
    description: quiz.description ?? '',
    category: quiz.category ?? '',
    rules: quiz.rules ?? '',
    defaultQuestionTimeSeconds: quiz.defaultQuestionTimeSeconds,
  }
}

type QuestionDraft = QuestionDetails & {
  id: string
  answerOptions: AnswerOption[]
}

function toQuestionDraft(question: QuizQuestion): QuestionDraft {
  return {
    id: question.id,
    text: question.text ?? '',
    imageUrl: question.imageUrl ?? '',
    type: question.type,
    position: question.position,
    timeLimitSeconds: question.timeLimitSeconds,
    points: question.points,
    answerOptions: question.answerOptions.map((option) => ({ ...option })),
  }
}

export function QuizEditorPage() {
  const { quizId } = useParams()
  const navigate = useNavigate()
  const [quiz, setQuiz] = useState<Quiz | null>(null)
  const [details, setDetails] = useState<QuizDetails>(emptyQuizDetails)
  const [questions, setQuestions] = useState<QuizQuestion[]>([])
  const [selectedId, setSelectedId] = useState('')
  const [draft, setDraft] = useState<QuestionDraft | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [isBusy, setIsBusy] = useState(false)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')

  useEffect(() => {
    if (!quizId) {
      return
    }

    const load = async () => {
      try {
        const [loadedQuiz, loadedQuestions] = await Promise.all([
          quizApi.get(quizId),
          quizApi.getQuestions(quizId),
        ])
        setQuiz(loadedQuiz)
        setDetails(toQuizDetails(loadedQuiz))
        setQuestions(loadedQuestions)
        setSelectedId(loadedQuestions[0]?.id ?? '')
      } catch (requestError) {
        setError(getApiErrorMessage(requestError))
      } finally {
        setIsLoading(false)
      }
    }

    void load()
  }, [quizId])

  const selectedQuestion = useMemo(
    () => questions.find((question) => question.id === selectedId) ?? null,
    [questions, selectedId],
  )

  useEffect(() => {
    setDraft(selectedQuestion ? toQuestionDraft(selectedQuestion) : null)
  }, [selectedQuestion])

  if (!quizId) {
    return <Navigate to="/quizzes" replace />
  }

  const updateDetails = <Key extends keyof QuizDetails>(
    key: Key,
    value: QuizDetails[Key],
  ) => setDetails((current) => ({ ...current, [key]: value }))

  const refreshQuestions = async (preferredId?: string) => {
    const loaded = await quizApi.getQuestions(quizId)
    setQuestions(loaded)
    setSelectedId(
      preferredId && loaded.some((question) => question.id === preferredId)
        ? preferredId
        : (loaded[0]?.id ?? ''),
    )
  }

  const runAction = async (action: () => Promise<void>) => {
    setIsBusy(true)
    setError('')
    setNotice('')

    try {
      await action()
    } catch (requestError) {
      setError(getApiErrorMessage(requestError))
    } finally {
      setIsBusy(false)
    }
  }

  const saveQuiz = () =>
    runAction(async () => {
      const updated = await quizApi.update(quizId, details)
      setQuiz(updated)
      setDetails(toQuizDetails(updated))
      setNotice('Настройки квиза сохранены.')
    })

  const addQuestion = () =>
    runAction(async () => {
      const position =
        questions.length === 0
          ? 0
          : Math.max(...questions.map((question) => question.position)) + 1
      const created = await quizApi.createQuestion(quizId, {
        text: 'Новый вопрос',
        imageUrl: '',
        type: 'SingleChoice',
        position,
        timeLimitSeconds: quiz?.defaultQuestionTimeSeconds ?? 30,
        points: 100,
      })
      await refreshQuestions(created.id)
    })

  const saveQuestion = () => {
    if (!draft) {
      return
    }

    void runAction(async () => {
      await quizApi.updateQuestion(quizId, draft.id, {
        text: draft.text,
        imageUrl: draft.imageUrl,
        type: draft.type,
        position: draft.position,
        timeLimitSeconds: draft.timeLimitSeconds,
        points: draft.points,
      })

      for (const option of draft.answerOptions) {
        await quizApi.updateAnswerOption(quizId, draft.id, option)
      }

      await refreshQuestions(draft.id)
      setNotice('Вопрос и варианты ответа сохранены.')
    })
  }

  const addAnswerOption = () => {
    if (!draft) {
      return
    }

    void runAction(async () => {
      const position =
        draft.answerOptions.length === 0
          ? 0
          : Math.max(...draft.answerOptions.map((option) => option.position)) +
            1
      const created = await quizApi.createAnswerOption(quizId, draft.id, {
        text: 'Новый вариант',
        isCorrect: false,
        position,
      })
      setDraft((current) =>
        current?.id === draft.id
          ? {
              ...current,
              answerOptions: [...current.answerOptions, created],
            }
          : current,
      )
    })
  }

  const deleteAnswerOption = (optionId: string) => {
    if (!draft || !window.confirm('Удалить этот вариант ответа?')) {
      return
    }

    void runAction(async () => {
      await quizApi.deleteAnswerOption(quizId, draft.id, optionId)
      setDraft((current) =>
        current?.id === draft.id
          ? {
              ...current,
              answerOptions: current.answerOptions.filter(
                (option) => option.id !== optionId,
              ),
            }
          : current,
      )
    })
  }

  const deleteQuestion = () => {
    if (!draft || !window.confirm('Удалить выбранный вопрос?')) {
      return
    }

    void runAction(async () => {
      await quizApi.deleteQuestion(quizId, draft.id)
      await refreshQuestions()
    })
  }

  const publish = () =>
    runAction(async () => {
      const published = await quizApi.publish(quizId)
      setQuiz(published)
      setNotice('Квиз опубликован и готов к запуску.')
    })

  const archive = () =>
    runAction(async () => {
      const archived = await quizApi.archive(quizId)
      setQuiz(archived)
      setNotice('Квиз перемещён в архив.')
    })

  const restore = () =>
    runAction(async () => {
      const restored = await quizApi.restore(quizId)
      setQuiz(restored)
      setNotice('Квиз возвращён в черновик и снова доступен для редактирования.')
    })

  const launch = () =>
    runAction(async () => {
      const session = await quizApi.createSession(quizId)
      navigate(`/sessions/${session.id}`)
    })

  const updateDraft = <Key extends keyof QuestionDraft>(
    key: Key,
    value: QuestionDraft[Key],
  ) => setDraft((current) => (current ? { ...current, [key]: value } : null))

  const updateOption = (
    optionId: string,
    update: Partial<AnswerOption>,
  ) => {
    setDraft((current) => {
      if (!current) {
        return null
      }

      let answerOptions = current.answerOptions.map((option) =>
        option.id === optionId ? { ...option, ...update } : option,
      )

      if (
        current.type === 'SingleChoice' &&
        update.isCorrect === true
      ) {
        answerOptions = answerOptions.map((option) => ({
          ...option,
          isCorrect: option.id === optionId,
        }))
      }

      return { ...current, answerOptions }
    })
  }

  if (isLoading) {
    return <div className="page-loader">Загружаем редактор…</div>
  }

  if (!quiz) {
    return (
      <main className="app-page">
        <OrganizerHeader />
        <PageMessage error>{error || 'Квиз не найден.'}</PageMessage>
      </main>
    )
  }

  const editable = quiz.status === 'Draft'

  return (
    <main className="app-page">
      <OrganizerHeader />
      <div className="editor-container">
        <Link className="back-link" to="/quizzes">
          ← Мои квизы
        </Link>
        <div className="editor-heading">
          <div>
            <h1>{quiz.title}</h1>
            <span className={`status-pill status-pill--${quiz.status}`}>
              {statusLabels[quiz.status]}
            </span>
          </div>
          <div className="editor-heading__actions">
            {editable && (
              <>
                <button
                  className="button button--secondary"
                  onClick={saveQuiz}
                  disabled={isBusy}
                >
                  Сохранить настройки
                </button>
                <button
                  className="button button--primary"
                  onClick={publish}
                  disabled={isBusy}
                >
                  Опубликовать
                </button>
              </>
            )}
            {quiz.status === 'Published' && (
              <button
                className="button button--primary"
                onClick={launch}
                disabled={isBusy}
              >
                Создать комнату
              </button>
            )}
            {quiz.status !== 'Archived' && (
              <button
                className="text-button text-button--danger"
                onClick={archive}
                disabled={isBusy}
              >
                В архив
              </button>
            )}
            {quiz.status === 'Archived' && (
              <button
                className="button button--secondary"
                onClick={restore}
                disabled={isBusy}
              >
                Вернуть в черновик
              </button>
            )}
          </div>
        </div>

        {error && <PageMessage error>{error}</PageMessage>}
        {notice && <PageMessage>{notice}</PageMessage>}

        <details className="quiz-settings" open={questions.length === 0}>
          <summary>Настройки квиза</summary>
          <QuizDetailsFields
            form={details}
            update={updateDetails}
            disabled={!editable}
          />
        </details>

        <div className="editor-layout">
          <aside className="questions-panel panel">
            <div className="panel-title">
              <h2>Вопросы</h2>
              <span>{questions.length}</span>
            </div>
            <div className="question-navigation">
              {questions.map((question) => (
                <button
                  key={question.id}
                  className={
                    question.id === selectedId ? 'is-selected' : undefined
                  }
                  onClick={() => setSelectedId(question.id)}
                >
                  <span>{question.position + 1}</span>
                  <strong>{question.text || 'Вопрос с изображением'}</strong>
                  <small>{question.type}</small>
                </button>
              ))}
            </div>
            {editable && (
              <button
                className="button button--secondary button--full"
                onClick={addQuestion}
                disabled={isBusy}
              >
                + Добавить вопрос
              </button>
            )}
          </aside>

          <section className="question-editor panel">
            {!draft ? (
              <PageMessage>
                {editable
                  ? 'Добавьте первый вопрос.'
                  : 'В этом квизе нет вопросов.'}
              </PageMessage>
            ) : (
              <>
                <div className="panel-title">
                  <h2>Вопрос {draft.position + 1}</h2>
                  <span>
                    {draft.type === 'SingleChoice'
                      ? 'Один ответ'
                      : 'Несколько ответов'}
                  </span>
                </div>
                <label className="form-field">
                  <span>Текст вопроса</span>
                  <textarea
                    value={draft.text}
                    onChange={(event) =>
                      updateDraft('text', event.target.value)
                    }
                    maxLength={2000}
                    rows={3}
                    disabled={!editable}
                  />
                </label>
                <label className="form-field">
                  <span>Ссылка на изображение (необязательно)</span>
                  <input
                    type="url"
                    value={draft.imageUrl}
                    onChange={(event) =>
                      updateDraft('imageUrl', event.target.value)
                    }
                    maxLength={2048}
                    placeholder="https://..."
                    disabled={!editable}
                  />
                </label>
                <div className="answers-heading">
                  <strong>Варианты ответа</strong>
                  <small>
                    Отметьте правильный вариант
                    {draft.type === 'MultipleChoice' ? ' или варианты' : ''}
                  </small>
                </div>
                <div className="answer-options">
                  {draft.answerOptions.map((option) => (
                    <div className="answer-option" key={option.id}>
                      <button
                        type="button"
                        className={
                          option.isCorrect
                            ? 'correct-toggle is-correct'
                            : 'correct-toggle'
                        }
                        aria-label="Правильный ответ"
                        aria-pressed={option.isCorrect}
                        onClick={() =>
                          updateOption(option.id, {
                            isCorrect: !option.isCorrect,
                          })
                        }
                        disabled={!editable}
                      >
                        {option.isCorrect ? '✓' : ''}
                      </button>
                      <input
                        value={option.text}
                        onChange={(event) =>
                          updateOption(option.id, {
                            text: event.target.value,
                          })
                        }
                        maxLength={500}
                        disabled={!editable}
                      />
                      {editable && (
                        <button
                          type="button"
                          className="remove-option"
                          onClick={() => deleteAnswerOption(option.id)}
                          aria-label="Удалить вариант"
                        >
                          ×
                        </button>
                      )}
                    </div>
                  ))}
                </div>
                {editable && (
                  <div className="question-editor__actions">
                    <button
                      className="button button--secondary"
                      onClick={addAnswerOption}
                      disabled={isBusy}
                    >
                      + Вариант ответа
                    </button>
                    <button
                      className="button button--primary"
                      onClick={saveQuestion}
                      disabled={isBusy}
                    >
                      Сохранить вопрос
                    </button>
                    <button
                      className="text-button text-button--danger"
                      onClick={deleteQuestion}
                      disabled={isBusy}
                    >
                      Удалить
                    </button>
                  </div>
                )}
              </>
            )}
          </section>

          <aside className="question-settings panel">
            <h2>Настройки</h2>
            {draft && (
              <>
                <label className="form-field">
                  <span>Тип вопроса</span>
                  <select
                    value={draft.type}
                    onChange={(event) =>
                      updateDraft(
                        'type',
                        event.target.value as QuestionType,
                      )
                    }
                    disabled={!editable}
                  >
                    <option value="SingleChoice">Один ответ</option>
                    <option value="MultipleChoice">Несколько ответов</option>
                  </select>
                </label>
                <label className="form-field">
                  <span>Время, секунд</span>
                  <input
                    type="number"
                    value={draft.timeLimitSeconds}
                    onChange={(event) =>
                      updateDraft(
                        'timeLimitSeconds',
                        Number(event.target.value),
                      )
                    }
                    min={5}
                    max={300}
                    disabled={!editable}
                  />
                </label>
                <label className="form-field">
                  <span>Баллы</span>
                  <input
                    type="number"
                    value={draft.points}
                    onChange={(event) =>
                      updateDraft('points', Number(event.target.value))
                    }
                    min={1}
                    max={10000}
                    disabled={!editable}
                  />
                </label>
                <div className="publish-note">
                  <strong>Перед публикацией</strong>
                  <p>
                    Нужно минимум два варианта и правильный ответ в каждом
                    вопросе.
                  </p>
                </div>
              </>
            )}
          </aside>
        </div>
      </div>
    </main>
  )
}

export function OrganizerSessionPage() {
  const { sessionId } = useParams()
  const [session, setSession] = useState<QuizSession | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [isBusy, setIsBusy] = useState(false)
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
        const loadedSession = await quizApi.getSession(sessionId)

        if (!isActive) {
          return
        }

        setSession(loadedSession)
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

  if (!sessionId) {
    return <Navigate to="/quizzes" replace />
  }

  const runAction = async (
    action: () => Promise<QuizSession>,
  ) => {
    setIsBusy(true)
    setError('')

    try {
      setSession(await action())
    } catch (requestError) {
      setError(getApiErrorMessage(requestError))
    } finally {
      setIsBusy(false)
    }
  }

  const openQuestion = async (questionId: string) => {
    setIsBusy(true)
    setError('')

    try {
      if (session?.currentQuestionId && !session.openQuestion) {
        try {
          await quizApi.closeQuestion(sessionId)
        } catch (requestError) {
          if (!(requestError instanceof ApiError) || requestError.status !== 409) {
            throw requestError
          }
        }
      }

      setSession(await quizApi.openQuestion(sessionId, questionId))
    } catch (requestError) {
      setError(getApiErrorMessage(requestError))
    } finally {
      setIsBusy(false)
    }
  }

  if (isLoading) {
    return <div className="page-loader">Подготавливаем комнату…</div>
  }

  if (!session) {
    return (
      <main className="app-page">
        <OrganizerHeader dark />
        <PageMessage error>{error || 'Комната не найдена.'}</PageMessage>
      </main>
    )
  }

  const pendingQuestions = session.questions.filter(
    (question) => question.status === 'Pending',
  )

  return (
    <main className="session-page">
      <OrganizerHeader dark />
      <div className="session-container">
        <section className="session-summary">
          <span className="eyebrow">
            {session.status === 'Waiting'
              ? 'Лобби'
              : session.status === 'Running'
                ? 'Квиз идёт'
                : 'Квиз завершён'}
          </span>
          <h1>{session.quizTitle}</h1>
          <p>
            {session.status === 'Waiting'
              ? 'Участники подключаются по коду комнаты'
              : `${session.participants.length} участников`}
          </p>
          {session.status === 'Waiting' && (
            <div className="lobby-quiz-info panel">
              {session.quizCategory && (
                <span className="lobby-quiz-category">
                  {session.quizCategory}
                </span>
              )}
              <p>
                {session.quizDescription || 'Описание квиза не указано.'}
              </p>
              <small>
                Квиз содержит {session.questions.length} вопросов.
              </small>
              <div className="lobby-quiz-rules">
                <strong>Правила проведения</strong>
                <p>{session.quizRules || 'Дополнительные правила не указаны.'}</p>
              </div>
            </div>
          )}
          <div className="room-code">
            <small>Код комнаты</small>
            <strong>{session.roomCode}</strong>
            <span>Сообщите этот код участникам</span>
          </div>

          {session.status === 'Running' && (
            <div className="session-controls panel">
              <div className="panel-title">
                <h2>Управление</h2>
                <span>
                  {
                    session.questions.filter(
                      (question) => question.status === 'Closed',
                    ).length
                  }{' '}
                  / {session.questions.length}
                </span>
              </div>

              {session.openQuestion ? (
                <div className="current-question">
                  <small>
                    Вопрос {session.openQuestion.position + 1} открыт
                  </small>
                  <strong>
                    {session.openQuestion.text || 'Вопрос с изображением'}
                  </strong>
                  <span>
                    До{' '}
                    {new Date(
                      session.openQuestion.closesAtUtc,
                    ).toLocaleTimeString('ru-RU', {
                      hour: '2-digit',
                      minute: '2-digit',
                      second: '2-digit',
                    })}
                  </span>
                  <button
                    className="button button--primary button--full"
                    onClick={() =>
                      void runAction(() => quizApi.closeQuestion(sessionId))
                    }
                    disabled={isBusy}
                  >
                    Закрыть вопрос
                  </button>
                </div>
              ) : pendingQuestions.length > 0 ? (
                <div className="next-questions">
                  <p>Выберите следующий вопрос:</p>
                  {pendingQuestions.map((question) => (
                    <button
                      className="button button--secondary button--full"
                      key={question.id}
                      onClick={() => void openQuestion(question.id)}
                      disabled={isBusy}
                    >
                      Открыть вопрос {question.position + 1}
                    </button>
                  ))}
                </div>
              ) : (
                <p>Все вопросы показаны. Можно завершить квиз.</p>
              )}

              <button
                className="text-button text-button--danger"
                onClick={() =>
                  void runAction(() => quizApi.finishSession(sessionId))
                }
                disabled={isBusy}
              >
                Завершить квиз
              </button>
            </div>
          )}

          {session.status === 'Finished' && (
            <Link className="button button--secondary button--full" to="/quizzes">
              Вернуться к квизам
            </Link>
          )}
        </section>

        <section className="participants-panel panel">
          <div className="panel-title">
            <h2>
              {session.status === 'Finished'
                ? 'Итоговый лидерборд'
                : 'Участники'}
            </h2>
            <span>{session.participants.length} подключено</span>
          </div>

          {session.participants.length === 0 ? (
            <PageMessage>Пока никто не подключился.</PageMessage>
          ) : (
            <div className="participants-grid">
              {session.participants.map((participant, index) => (
                <article className="participant-card" key={participant.id}>
                  <span className="participant-place">{index + 1}</span>
                  <span className="participant-avatar">
                    <img src={participantAvatar} alt="" />
                    <b>{initials(participant.displayName)}</b>
                  </span>
                  <strong>{participant.displayName}</strong>
                  <small>
                    {session.status === 'Waiting'
                      ? 'подключён'
                      : `${participant.score} баллов`}
                  </small>
                </article>
              ))}
            </div>
          )}

          {session.status === 'Waiting' && (
            <button
              className="button button--primary session-start"
              onClick={() =>
                void runAction(() => quizApi.startSession(sessionId))
              }
              disabled={isBusy}
            >
              Запустить квиз
            </button>
          )}
          {(error || connectionError) && (
            <PageMessage error>{error || connectionError}</PageMessage>
          )}
        </section>
      </div>
    </main>
  )
}
