import { useState } from 'react'
import { NavLink, Outlet } from 'react-router-dom'
import { useAuth } from '../auth/auth-state'
import { Brand } from './Brand'
import { Icon } from './Icon'

const navigation = [
  { to: '/saha-ara', label: 'Saha bul' },
]

export function AppLayout() {
  const [menuOpen, setMenuOpen] = useState(false)
  const { user } = useAuth()

  return (
    <div className="app-frame">
      <a className="skip-link" href="#main-content">İçeriğe geç</a>
      <header className="site-header">
        <div className="nav-shell">
          <Brand />
          <button aria-controls="primary-navigation" aria-expanded={menuOpen} aria-label="Menüyü aç veya kapat" className="menu-button" onClick={() => setMenuOpen((open) => !open)} type="button"><Icon name="menu" /></button>
          <nav aria-label="Ana menü" className={menuOpen ? 'nav-links nav-links--open' : 'nav-links'} id="primary-navigation">
            {navigation.map((item) => <NavLink key={item.to} onClick={() => setMenuOpen(false)} to={item.to}>{item.label}</NavLink>)}
            {user && <NavLink onClick={() => setMenuOpen(false)} to="/rezervasyonlarim">Rezervasyonlarım</NavLink>}
            <NavLink className="nav-login" onClick={() => setMenuOpen(false)} to="/giris">{user?.displayName ?? 'Giriş yap'}</NavLink>
          </nav>
        </div>
      </header>
      <main id="main-content"><Outlet /></main>
      <footer className="site-footer">
        <div><Brand /><p>Sahayı bul, ekibi topla, oyuna çık.</p></div>
        <div className="footer-meta"><span>© 2026 SahaGo</span><span>Türkiye · TRY</span></div>
      </footer>
    </div>
  )
}
