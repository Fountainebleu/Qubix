import { apiFetch } from './api'

export type QuizStatus = 'Draft' | 'Published' | 'Archived'
export type QuestionType = 'SingleChoice' | 'MultipleChoice'
export type SessionStatus = 'Waiting' | 'Running' | 'Finished'
export type SessionQuestionStatus = 'Pending' | 'Open' | 'Closed'

export type Quiz = {
  id: string
  title: string
  description: string | null
  category: string | null
  rules: string | null
  defaultQuestionTimeSeconds: number
  status: QuizStatus
  createdAtUtc: string
  updatedAtUtc: string
}

export type QuizDetails = {
  title: string
  description: string
  category: string
  rules: string
  defaultQuestionTimeSeconds: number
}

export type AnswerOption = {
  id: string
  text: string
  isCorrect: boolean
  position: number
}

export type QuizQuestion = {
  id: string
  text: string | null
  imageUrl: string | null
  type: QuestionType
  position: number
  timeLimitSeconds: number
  points: number
  answerOptions: AnswerOption[]
}

export type QuestionDetails = {
  text: string
  imageUrl: string
  type: QuestionType
  position: number
  timeLimitSeconds: number
  points: number
}

export type SessionParticipant = {
  id: string
  displayName: string
  score: number
}

export type SessionQuestion = {
  id: string
  position: number
  status: SessionQuestionStatus
}

export type OpenQuestion = {
  id: string
  text: string | null
  imageUrl: string | null
  type: QuestionType
  position: number
  timeLimitSeconds: number
  points: number
  closesAtUtc: string
  answerOptions: Array<{
    id: string
    text: string
    position: number
  }>
}

export type QuizSession = {
  id: string
  quizId: string
  roomCode: string
  status: SessionStatus
  currentQuestionId: string | null
  createdAtUtc: string
  startedAtUtc: string | null
  finishedAtUtc: string | null
  participants: SessionParticipant[]
  questions: SessionQuestion[]
  openQuestion: OpenQuestion | null
}

export type ParticipantHistoryItem = {
  quizTitle: string
  completedAtUtc: string
  score: number
  place: number
}

export type OrganizerHistoryItem = {
  quizTitle: string
  completedAtUtc: string
  participantCount: number
}

export const quizApi = {
  getMine: () => apiFetch<Quiz[]>('/quizzes'),

  get: (quizId: string) => apiFetch<Quiz>(`/quizzes/${quizId}`),

  create: (details: QuizDetails) =>
    apiFetch<Quiz>('/quizzes', {
      method: 'POST',
      body: JSON.stringify(details),
    }),

  update: (quizId: string, details: QuizDetails) =>
    apiFetch<Quiz>(`/quizzes/${quizId}`, {
      method: 'PUT',
      body: JSON.stringify(details),
    }),

  publish: (quizId: string) =>
    apiFetch<Quiz>(`/quizzes/${quizId}/publish`, { method: 'POST' }),

  archive: (quizId: string) =>
    apiFetch<Quiz>(`/quizzes/${quizId}/archive`, { method: 'POST' }),

  getQuestions: (quizId: string) =>
    apiFetch<QuizQuestion[]>(`/quizzes/${quizId}/questions`),

  createQuestion: (quizId: string, details: QuestionDetails) =>
    apiFetch<QuizQuestion>(`/quizzes/${quizId}/questions`, {
      method: 'POST',
      body: JSON.stringify(details),
    }),

  updateQuestion: (
    quizId: string,
    questionId: string,
    details: QuestionDetails,
  ) =>
    apiFetch<QuizQuestion>(
      `/quizzes/${quizId}/questions/${questionId}`,
      {
        method: 'PUT',
        body: JSON.stringify(details),
      },
    ),

  deleteQuestion: (quizId: string, questionId: string) =>
    apiFetch<void>(`/quizzes/${quizId}/questions/${questionId}`, {
      method: 'DELETE',
    }),

  createAnswerOption: (
    quizId: string,
    questionId: string,
    details: Omit<AnswerOption, 'id'>,
  ) =>
    apiFetch<AnswerOption>(
      `/quizzes/${quizId}/questions/${questionId}/options`,
      {
        method: 'POST',
        body: JSON.stringify(details),
      },
    ),

  updateAnswerOption: (
    quizId: string,
    questionId: string,
    option: AnswerOption,
  ) =>
    apiFetch<AnswerOption>(
      `/quizzes/${quizId}/questions/${questionId}/options/${option.id}`,
      {
        method: 'PUT',
        body: JSON.stringify({
          text: option.text,
          isCorrect: option.isCorrect,
          position: option.position,
        }),
      },
    ),

  deleteAnswerOption: (
    quizId: string,
    questionId: string,
    optionId: string,
  ) =>
    apiFetch<void>(
      `/quizzes/${quizId}/questions/${questionId}/options/${optionId}`,
      { method: 'DELETE' },
    ),

  createSession: (quizId: string) =>
    apiFetch<QuizSession>('/sessions', {
      method: 'POST',
      body: JSON.stringify({ quizId }),
    }),

  getSession: (sessionId: string) =>
    apiFetch<QuizSession>(`/sessions/${sessionId}`),

  joinSession: (roomCode: string) =>
    apiFetch<QuizSession>(
      `/sessions/join/${encodeURIComponent(roomCode.trim().toUpperCase())}`,
      { method: 'POST' },
    ),

  submitAnswer: (
    sessionId: string,
    questionId: string,
    selectedOptionIds: string[],
  ) =>
    apiFetch<void>(
      `/sessions/${sessionId}/questions/${questionId}/answers`,
      {
        method: 'POST',
        body: JSON.stringify({ selectedOptionIds }),
      },
    ),

  startSession: (sessionId: string) =>
    apiFetch<QuizSession>(`/sessions/${sessionId}/start`, {
      method: 'POST',
    }),

  openQuestion: (sessionId: string, questionId: string) =>
    apiFetch<QuizSession>(
      `/sessions/${sessionId}/questions/${questionId}/open`,
      { method: 'POST' },
    ),

  closeQuestion: (sessionId: string) =>
    apiFetch<QuizSession>(
      `/sessions/${sessionId}/current-question/close`,
      { method: 'POST' },
    ),

  finishSession: (sessionId: string) =>
    apiFetch<QuizSession>(`/sessions/${sessionId}/finish`, {
      method: 'POST',
    }),
}

export const historyApi = {
  getParticipant: () =>
    apiFetch<ParticipantHistoryItem[]>('/history/participant'),

  getOrganizer: () =>
    apiFetch<OrganizerHistoryItem[]>('/history/organizer'),
}
