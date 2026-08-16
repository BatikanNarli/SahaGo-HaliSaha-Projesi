import { Link } from 'react-router-dom'

export function Brand() {
  return (
    <Link aria-label="SahaGo ana sayfa" className="brand" to="/">
      <span aria-hidden="true" className="brand-mark"><span /></span>
      <span>SahaGo</span>
    </Link>
  )
}
