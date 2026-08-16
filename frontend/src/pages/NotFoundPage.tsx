import { Link } from 'react-router-dom'
import { Icon } from '../components/Icon'

export function NotFoundPage() {
  return <section className="not-found section-shell"><span>404</span><h1>Bu saha çizgilerin dışında.</h1><p>Aradığın sayfa taşınmış veya henüz oyuna girmemiş olabilir.</p><Link className="button button--primary" to="/">Ana sayfaya dön <Icon name="arrow" /></Link></section>
}
