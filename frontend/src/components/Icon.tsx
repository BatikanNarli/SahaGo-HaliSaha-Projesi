import type { ReactNode } from 'react'

type IconName = 'arrow' | 'calendar' | 'check' | 'clock' | 'location' | 'menu' | 'shield' | 'users'

export function Icon({ name }: Readonly<{ name: IconName }>) {
  const paths: Record<IconName, ReactNode> = {
    arrow: <path d="m5 12h14m-5-5 5 5-5 5" />,
    calendar: <><rect x="3" y="5" width="18" height="16" rx="2" /><path d="M16 3v4M8 3v4M3 10h18" /></>,
    check: <path d="m5 12 4 4L19 6" />,
    clock: <><circle cx="12" cy="12" r="9" /><path d="M12 7v5l3 2" /></>,
    location: <><path d="M20 10c0 5-8 11-8 11S4 15 4 10a8 8 0 1 1 16 0Z" /><circle cx="12" cy="10" r="2.5" /></>,
    menu: <path d="M4 7h16M4 12h16M4 17h16" />,
    shield: <path d="M12 3 5 6v5c0 4.7 3 8.2 7 10 4-1.8 7-5.3 7-10V6l-7-3Zm-3 9 2 2 4-4" />,
    users: <><path d="M16 20v-2a4 4 0 0 0-4-4H7a4 4 0 0 0-4 4v2" /><circle cx="9.5" cy="7" r="4" /><path d="M17 11a4 4 0 0 1 4 4v2" /></>,
  }

  return <svg aria-hidden="true" className="icon" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeLinecap="round" strokeLinejoin="round" strokeWidth="1.8">{paths[name]}</svg>
}
