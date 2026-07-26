import { useState, type FormEvent } from 'react'
import {
  Link,
  Navigate,
  Outlet,
  Route,
  Routes,
  useNavigate,
} from 'react-router-dom'
import authGlow from './assets/auth-glow.svg'
import stepCircle from './assets/step-circle.svg'
import { getApiErrorMessage } from './api'
import { useAuth, type RegisterData } from './AuthContext'
import {
  CreateQuizPage,
  OrganizerSessionPage,
  QuizEditorPage,
  QuizListPage,
} from './OrganizerPages'
import {
  HistoryPage,
  JoinRoomPage,
  ParticipantSessionPage,
} from './ParticipantPages'
import './App.css'

function Logo({ inverted = false }: { inverted?: boolean }) {
  return (
    <div className="logo">
      <span className={inverted ? 'logo__mark logo__mark--inverted' : 'logo__mark'}>
        Q
      </span>
      <span>Qubix</span>
    </div>
  )
}

function LoginBrand() {
  return (
    <aside className="auth-brand auth-brand--login">
      <Logo />
      <div className="auth-brand__message">
        <h1>Квиз, который собирает всех.</h1>
        <p>
          Создавайте игры, подключайте участников по коду и следите за
          результатами в реальном времени.
        </p>
      </div>
      <img className="auth-brand__glow" src={authGlow} alt="" />
      <div className="live-card">
        <span>LIVE</span>
        <strong>Один вопрос — один общий момент.</strong>
      </div>
    </aside>
  )
}

function RegisterBrand() {
  const steps = [
    'Создайте профиль',
    'Выберите роль',
    'Подключайтесь или проводите',
  ]

  return (
    <aside className="auth-brand auth-brand--register">
      <Logo inverted />
      <div className="auth-brand__message">
        <h1>Начните свою первую игру</h1>
      </div>
      <ol className="register-steps">
        {steps.map((step, index) => (
          <li key={step}>
            <span className="register-steps__number">
              <img src={stepCircle} alt="" />
              <b>{index + 1}</b>
            </span>
            {step}
          </li>
        ))}
      </ol>
    </aside>
  )
}

function LoginPage() {
  const { login } = useAuth()
  const navigate = useNavigate()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setError('')
    setIsSubmitting(true)

    try {
      await login(email, password)
      navigate('/', { replace: true })
    } catch (requestError) {
      setError(getApiErrorMessage(requestError))
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <main className="auth-shell auth-shell--login">
      <LoginBrand />
      <section className="auth-content">
        <form className="auth-card auth-card--login" onSubmit={handleSubmit}>
          <header>
            <h2>Вход</h2>
            <p>Продолжите как участник или организатор</p>
          </header>

          <label className="form-field">
            <span>Электронная почта</span>
            <input
              type="email"
              value={email}
              onChange={(event) => setEmail(event.target.value)}
              placeholder="name@example.com"
              autoComplete="email"
              maxLength={256}
              required
            />
          </label>

          <label className="form-field">
            <span>Пароль</span>
            <input
              type="password"
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              placeholder="Введите пароль"
              autoComplete="current-password"
              maxLength={100}
              required
            />
          </label>

          {error && (
            <p className="form-error" role="alert">
              {error}
            </p>
          )}

          <button className="primary-button" disabled={isSubmitting}>
            {isSubmitting ? 'Входим…' : 'Войти'}
          </button>

          <p className="auth-card__switch">
            Нет аккаунта? <Link to="/register">Зарегистрироваться</Link>
          </p>
        </form>
      </section>
    </main>
  )
}

function RegisterPage() {
  const { register } = useAuth()
  const navigate = useNavigate()
  const [form, setForm] = useState<RegisterData>({
    displayName: '',
    email: '',
    password: '',
    role: 'Participant',
  })
  const [error, setError] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)

  const updateField = <Key extends keyof RegisterData>(
    field: Key,
    value: RegisterData[Key],
  ) => {
    setForm((current) => ({ ...current, [field]: value }))
  }

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setError('')
    setIsSubmitting(true)

    try {
      await register(form)
      navigate('/', { replace: true })
    } catch (requestError) {
      setError(getApiErrorMessage(requestError))
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <main className="auth-shell auth-shell--register">
      <RegisterBrand />
      <section className="auth-content">
        <form className="auth-card auth-card--register" onSubmit={handleSubmit}>
          <header>
            <h2>Создать аккаунт</h2>
            <p>Кем вы будете пользоваться Qubix?</p>
          </header>

          <div className="role-selector" aria-label="Роль пользователя">
            <button
              type="button"
              className={form.role === 'Participant' ? 'is-selected' : ''}
              aria-pressed={form.role === 'Participant'}
              onClick={() => updateField('role', 'Participant')}
            >
              Участник
            </button>
            <button
              type="button"
              className={form.role === 'Organizer' ? 'is-selected' : ''}
              aria-pressed={form.role === 'Organizer'}
              onClick={() => updateField('role', 'Organizer')}
            >
              Организатор
            </button>
          </div>

          <label className="form-field">
            <span>Имя</span>
            <input
              value={form.displayName}
              onChange={(event) =>
                updateField('displayName', event.target.value)
              }
              placeholder="Евгений"
              autoComplete="name"
              maxLength={50}
              required
            />
          </label>

          <label className="form-field">
            <span>Электронная почта</span>
            <input
              type="email"
              value={form.email}
              onChange={(event) => updateField('email', event.target.value)}
              placeholder="name@example.com"
              autoComplete="email"
              maxLength={256}
              required
            />
          </label>

          <label className="form-field">
            <span>Пароль</span>
            <input
              type="password"
              value={form.password}
              onChange={(event) => updateField('password', event.target.value)}
              placeholder="Не менее 8 символов"
              autoComplete="new-password"
              minLength={8}
              maxLength={100}
              required
            />
          </label>

          {error && (
            <p className="form-error" role="alert">
              {error}
            </p>
          )}

          <button className="primary-button" disabled={isSubmitting}>
            {isSubmitting ? 'Создаём аккаунт…' : 'Зарегистрироваться'}
          </button>

          <p className="auth-card__switch">
            Уже есть аккаунт? <Link to="/login">Войти</Link>
          </p>
          <p className="auth-card__terms">
            Создавая аккаунт, вы соглашаетесь с правилами сервиса
          </p>
        </form>
      </section>
    </main>
  )
}

function ProtectedRoute() {
  const { user, isLoading } = useAuth()

  if (isLoading) {
    return <div className="page-loader">Загрузка…</div>
  }

  return user ? <Outlet /> : <Navigate to="/login" replace />
}

function OrganizerRoute() {
  const { user } = useAuth()

  return user?.roles.includes('Organizer') ? (
    <Outlet />
  ) : (
    <Navigate to="/" replace />
  )
}

function HomePage() {
  const { user } = useAuth()

  return (
    <Navigate
      to={user?.roles.includes('Organizer') ? '/quizzes' : '/join'}
      replace
    />
  )
}

function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />
      <Route element={<ProtectedRoute />}>
        <Route index element={<HomePage />} />
        <Route path="/join" element={<JoinRoomPage />} />
        <Route path="/play/:sessionId" element={<ParticipantSessionPage />} />
        <Route path="/history" element={<HistoryPage />} />
        <Route element={<OrganizerRoute />}>
          <Route path="/quizzes" element={<QuizListPage />} />
          <Route path="/quizzes/new" element={<CreateQuizPage />} />
          <Route path="/quizzes/:quizId" element={<QuizEditorPage />} />
          <Route
            path="/sessions/:sessionId"
            element={<OrganizerSessionPage />}
          />
        </Route>
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}

export default App
