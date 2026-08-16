const TURKISH_MOBILE_PATTERN = /^5[0-9]{9}$/

export function normalizeTurkishMobileInput(value: string) {
  let digits = value.replace(/[^0-9]/g, '')

  if (digits.startsWith('0090')) digits = digits.slice(4)
  else if (digits.startsWith('90') && digits.length === 12) digits = digits.slice(2)
  else if (digits.startsWith('0') && digits.length === 11) digits = digits.slice(1)

  return digits
}

export function isTurkishMobile(value: string) {
  return TURKISH_MOBILE_PATTERN.test(normalizeTurkishMobileInput(value))
}

export function toTurkishMobileE164(value: string) {
  const localNumber = normalizeTurkishMobileInput(value)
  return TURKISH_MOBILE_PATTERN.test(localNumber) ? `+90${localNumber}` : value.trim()
}

export function normalizeLoginIdentifier(value: string) {
  const identifier = value.trim()
  return identifier.includes('@') ? identifier : toTurkishMobileE164(identifier)
}
