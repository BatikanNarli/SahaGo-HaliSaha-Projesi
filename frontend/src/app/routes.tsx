import { createBrowserRouter, type RouteObject } from 'react-router-dom'
import { AppLayout } from '../components/AppLayout'
import { HomePage } from '../pages/HomePage'
import { LoginPage } from '../pages/LoginPage'
import { NotFoundPage } from '../pages/NotFoundPage'
import { SearchPage } from '../pages/SearchPage'
import { ReservationsPage } from '../pages/ReservationsPage'

export const appRoutes: RouteObject[] = [
  {
    path: '/',
    element: <AppLayout />,
    children: [
      { index: true, element: <HomePage /> },
      { path: 'saha-ara', element: <SearchPage /> },
      { path: 'rezervasyonlarim', element: <ReservationsPage /> },
      { path: 'giris', element: <LoginPage mode="login" /> },
      { path: 'kayit', element: <LoginPage mode="register" /> },
      { path: '*', element: <NotFoundPage /> },
    ],
  },
]

export const browserRouter = createBrowserRouter(appRoutes)
