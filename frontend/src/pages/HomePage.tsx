import { Link } from 'react-router-dom'
import { Icon } from '../components/Icon'
import { SystemStatus } from '../components/SystemStatus'

const benefits = [
  { icon: 'location' as const, title: 'Yakınındaki sahalar', copy: 'İlçe, tarih ve özelliklere göre sana uygun sahayı bul.' },
  { icon: 'clock' as const, title: 'Canlı uygunluk', copy: 'Boş saatleri ve sunucuda hesaplanan net fiyatı tek yerde gör.' },
  { icon: 'shield' as const, title: 'Güvenli rezervasyon', copy: 'Çakışma koruması ve tekrar güvenli ödeme akışıyla rahat et.' },
]

export function HomePage() {
  return <>
    <section className="hero section-shell">
      <div className="hero-copy">
        <SystemStatus /><p className="eyebrow">Maç planlamak artık çok kolay</p><h1>Sahan hazır.<br /><em>Sıra sende.</em></h1>
        <p className="hero-lead">Yakınındaki halı sahaları keşfet, boş saatleri karşılaştır ve takımın için en doğru zamanı ayır.</p>
        <div className="hero-actions"><Link className="button button--primary" to="/saha-ara">Saha bul <Icon name="arrow" /></Link></div>
        <div aria-label="Platform özellikleri" className="proof-row"><span><Icon name="check" /> Anlık uygunluk</span><span><Icon name="check" /> Şeffaf fiyat</span><span><Icon name="check" /> Güvenli ödeme</span></div>
      </div>
      <div aria-label="Halı saha rezervasyon ön izlemesi" className="hero-visual" role="img">
        <div className="field-card"><div className="field-lines"><span /><i /></div><div className="match-card"><div><span>Bu akşam</span><strong>20:30</strong></div><div className="match-badge">Uygun</div></div><div className="player player--one">10</div><div className="player player--two">7</div><div className="player player--three">4</div><div className="player player--four">9</div></div>
        <div className="floating-note"><Icon name="location" /><span><strong>Kadıköy</strong> · 2,4 km</span></div>
      </div>
    </section>
    <section aria-labelledby="why-sahago" className="benefits section-shell">
      <div className="section-heading"><div><p className="eyebrow">Tek platform, net plan</p><h2 id="why-sahago">Maçtan önce yorulma.</h2></div></div>
      <div className="benefit-grid">{benefits.map((benefit, index) => <article key={benefit.title} className="benefit-card"><span className="card-index">0{index + 1}</span><div className="icon-tile"><Icon name={benefit.icon} /></div><h3>{benefit.title}</h3><p>{benefit.copy}</p></article>)}</div>
    </section>
    <section className="audience-section audience-section--single section-shell">
      <article className="audience-card audience-card--dark"><p className="eyebrow">Oyuncular için</p><h2>Takımı kur.<br />Saati seç.</h2><p>Onaylı tesisleri tek ekranda karşılaştır, rezervasyonunu ve geçmişini takip et.</p><Link className="button button--light" to="/saha-ara">Saha aramaya başla <Icon name="arrow" /></Link></article>
    </section>
  </>
}
