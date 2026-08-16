import { RouterProvider } from 'react-router-dom'
import { browserRouter } from './routes'

export function App() {
  return <RouterProvider router={browserRouter} />
}
