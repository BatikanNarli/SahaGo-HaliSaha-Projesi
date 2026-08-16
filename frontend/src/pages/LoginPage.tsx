import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link, NavLink } from 'react-router-dom'
import { z } from 'zod'
import { useAuth } from '../auth/auth-state'
import { Icon } from '../components/Icon'
import { ApiError } from '../lib/api'
import { isTurkishMobile, normalizeLoginIdentifier, normalizeTurkishMobileInput, toTurkishMobileE164 } from '../lib/phone'

const loginSchema = z.object({
  identifier: z.string().min(3, 'E-posta veya telefon numaranızı girin.'),
  password: z.string().min(1, 'Parolanızı girin.'),
})

const registerSchema = z.object({
  displayName: z.string().trim().min(2, 'Ad soyad en az 2 karakter olmalıdır.').max(100),
  email: z.union([z.literal(''), z.email('Geçerli bir e-posta adresi girin.')]),
  phoneNumber: z.string()
    .transform(normalizeTurkishMobileInput)
    .refine((value) => value === '' || isTurkishMobile(value), 'Telefon 5XX XXX XX XX biçiminde olmalıdır.'),
  password: z.string().min(10, 'Parola en az 10 karakter olmalıdır.')
    .regex(/[A-Z]/, 'En az bir büyük harf gereklidir.')
    .regex(/[a-z]/, 'En az bir küçük harf gereklidir.')
    .regex(/[0-9]/, 'En az bir rakam gereklidir.'),
  confirmPassword: z.string(),
}).refine((value) => Boolean(value.email || value.phoneNumber), {
  message: 'E-posta veya telefon numarasından en az birini girin.',
  path: ['email'],
}).refine((value) => value.password === value.confirmPassword, {
  message: 'Parolalar eşleşmiyor.',
  path: ['confirmPassword'],
})

type LoginValues = z.infer<typeof loginSchema>
type RegisterValues = z.infer<typeof registerSchema>

export function LoginPage({ mode }: { mode: 'login' | 'register' }) {
  const auth = useAuth()

  return <section className="auth-page section-shell">
    <div className="auth-message">
      <p className="eyebrow">{mode === 'login' ? 'Tekrar hoş geldin' : 'Oyuna katıl'}</p>
      <h1>{mode === 'login' ? 'Sahaya dön.' : 'Takımda yerin hazır.'}</h1>
      <p>Tek güvenli hesapla saha ara, rezervasyonlarını takip et ve işletme yetkin varsa paneline eriş.</p>
      <ul><li><Icon name="shield" /> Kısa ömürlü erişim anahtarı</li><li><Icon name="check" /> Döndürülen ve iptal edilebilir oturum</li><li><Icon name="users" /> Rol ve işletme bazlı erişim</li></ul>
    </div>
    {auth.isBootstrapping
      ? <div aria-live="polite" className="auth-card"><h2>Oturum kontrol ediliyor…</h2><p>Güvenli oturum çerezi doğrulanıyor.</p></div>
      : auth.user
      ? <AuthenticatedCard />
      : mode === 'login' ? <LoginForm /> : <RegisterForm />}
  </section>
}

function LoginForm() {
  const auth = useAuth()
  const [serverError, setServerError] = useState<string | null>(null)
  const { register, handleSubmit, formState: { errors, isSubmitting } } = useForm<LoginValues>({ resolver: zodResolver(loginSchema) })

  const submit = async (values: LoginValues) => {
    setServerError(null)
    try {
      await auth.login(normalizeLoginIdentifier(values.identifier), values.password)
    } catch (error) {
      setServerError(toMessage(error))
    }
  }

  return <form className="auth-card" noValidate onSubmit={(event) => { void handleSubmit(submit)(event) }}>
    <AuthTabs />
    <h2>Giriş yap</h2>
    <p>E-posta veya Türkiye telefon numaranı kullan.</p>
    <div className="field-group"><label htmlFor="identifier">E-posta veya telefon</label><input autoComplete="username" id="identifier" placeholder="ornek@email.com" {...register('identifier')} />{errors.identifier && <span className="field-error">{errors.identifier.message}</span>}</div>
    <div className="field-group"><label htmlFor="password">Parola</label><input autoComplete="current-password" id="password" type="password" {...register('password')} />{errors.password && <span className="field-error">{errors.password.message}</span>}</div>
    <button className="button button--primary button--full" disabled={isSubmitting} type="submit">{isSubmitting ? 'Giriş yapılıyor…' : 'Giriş yap'} <Icon name="arrow" /></button>
    {serverError && <div aria-live="assertive" className="form-notice form-notice--error" role="alert">{serverError}</div>}
  </form>
}

function RegisterForm() {
  const auth = useAuth()
  const [serverError, setServerError] = useState<string | null>(null)
  const { register, handleSubmit, formState: { errors, isSubmitting } } = useForm<RegisterValues>({
    resolver: zodResolver(registerSchema),
    defaultValues: { email: '', phoneNumber: '' },
  })

  const submit = async (values: RegisterValues) => {
    setServerError(null)
    try {
      await auth.register({
        displayName: values.displayName,
        email: values.email || undefined,
        phoneNumber: values.phoneNumber ? toTurkishMobileE164(values.phoneNumber) : undefined,
        password: values.password,
      })
    } catch (error) {
      setServerError(toMessage(error))
    }
  }

  return <form className="auth-card" noValidate onSubmit={(event) => { void handleSubmit(submit)(event) }}>
    <AuthTabs />
    <h2>Hesap oluştur</h2>
    <p>E-posta veya telefon numarasından en az biri yeterli.</p>
    <div className="field-group"><label htmlFor="displayName">Ad soyad</label><input autoComplete="name" id="displayName" {...register('displayName')} />{errors.displayName && <span className="field-error">{errors.displayName.message}</span>}</div>
    <div className="auth-fields-row">
      <div className="field-group"><label htmlFor="email">E-posta</label><input autoComplete="email" id="email" type="email" {...register('email')} />{errors.email && <span className="field-error">{errors.email.message}</span>}</div>
      <div className="field-group"><label htmlFor="phoneNumber">Telefon</label><div className="phone-input"><span aria-hidden="true">+90</span><input aria-describedby="phoneNumberHint" autoComplete="tel-national" id="phoneNumber" inputMode="numeric" placeholder="555 111 22 33" type="tel" {...register('phoneNumber')} /></div><span className="field-hint" id="phoneNumberHint">Başında sıfır olmadan 10 hane.</span>{errors.phoneNumber && <span className="field-error">{errors.phoneNumber.message}</span>}</div>
    </div>
    <div className="auth-fields-row">
      <div className="field-group"><label htmlFor="newPassword">Parola</label><input autoComplete="new-password" id="newPassword" type="password" {...register('password')} />{errors.password && <span className="field-error">{errors.password.message}</span>}</div>
      <div className="field-group"><label htmlFor="confirmPassword">Parola tekrar</label><input autoComplete="new-password" id="confirmPassword" type="password" {...register('confirmPassword')} />{errors.confirmPassword && <span className="field-error">{errors.confirmPassword.message}</span>}</div>
    </div>
    <button className="button button--primary button--full" disabled={isSubmitting} type="submit">{isSubmitting ? 'Hesap oluşturuluyor…' : 'Ücretsiz hesap oluştur'} <Icon name="arrow" /></button>
    {serverError && <div aria-live="assertive" className="form-notice form-notice--error" role="alert">{serverError}</div>}
  </form>
}

function AuthTabs() {
  return <nav aria-label="Kimlik işlemleri" className="auth-tabs"><NavLink to="/giris">Giriş</NavLink><NavLink to="/kayit">Kayıt</NavLink></nav>
}

function AuthenticatedCard() {
  const auth = useAuth()
  return <div className="auth-card auth-success"><span className="auth-success__icon"><Icon name="check" /></span><h2>Hoş geldin, {auth.user?.displayName}</h2><p>Hesabın API ve SQL Server üzerinden doğrulandı.</p><div className="role-badges">{auth.user?.roles.map((role) => <span key={role}>{role}</span>)}</div><Link className="button button--primary button--full" to="/rezervasyonlarim">Rezervasyonlarımı gör</Link><button className="button button--outline button--full" onClick={() => { void auth.logout() }} type="button">Güvenli çıkış yap</button></div>
}

function toMessage(error: unknown) {
  if (error instanceof ApiError) return error.message
  return 'API bağlantısı kurulamadı. Lütfen tekrar deneyin.'
}
