import { useQuery } from '@tanstack/react-query'
import { getSystemHealth } from '../lib/api'

export function SystemStatus() {
  const health = useQuery({ queryKey: ['system-health'], queryFn: ({ signal }) => getSystemHealth(signal) })
  const state = health.isPending ? 'Kontrol ediliyor' : health.isSuccess ? 'Sistem hazır' : 'API bağlantısı bekleniyor'
  return <div aria-live="polite" className={`system-pill ${health.isSuccess ? 'system-pill--ready' : ''}`}><span aria-hidden="true" />{state}</div>
}
