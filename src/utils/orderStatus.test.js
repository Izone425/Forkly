import { describe, it, expect } from 'vitest'
import { phaseOf, phaseIcon, phaseLabel, PHASES } from './orderStatus.js'

const order = (o = {}) => ({ status: 'Pending', paymentStatus: 'Paid', ...o })

describe('phaseOf', () => {
  it('maps each paid fulfilment status to its phase', () => {
    expect(phaseOf(order({ status: 'Pending' }))).toBe('confirmed')
    expect(phaseOf(order({ status: 'Preparing' }))).toBe('preparing')
    // "Completed" is the kitchen being done, not the order — the customer sees "Ready".
    expect(phaseOf(order({ status: 'Completed' }))).toBe('ready')
    expect(phaseOf(order({ status: 'OutForDelivery' }))).toBe('out')
    expect(phaseOf(order({ status: 'Delivered' }))).toBe('delivered')
    expect(phaseOf(order({ status: 'Cancelled' }))).toBe('cancelled')
  })

  it('shows "payment needed" ahead of the fulfilment status', () => {
    // Paying is the action the customer has to take, whatever the kitchen thinks.
    expect(phaseOf(order({ status: 'Pending', paymentStatus: 'Unpaid' }))).toBe('unpaid')
    expect(phaseOf(order({ status: 'Preparing', paymentStatus: 'Unpaid' }))).toBe('unpaid')
  })

  it('lets the terminal states win over unpaid', () => {
    // A cancelled order is cancelled whether or not it was ever paid for — don't nag.
    expect(phaseOf(order({ status: 'Cancelled', paymentStatus: 'Unpaid' }))).toBe('cancelled')
    expect(phaseOf(order({ status: 'Delivered', paymentStatus: 'Unpaid' }))).toBe('delivered')
  })

  it('tolerates missing or unknown input', () => {
    expect(phaseOf(null)).toBeNull()
    expect(phaseOf(undefined)).toBeNull()
    // An unrecognised status falls back to "waiting", never to a crash.
    expect(phaseOf(order({ status: 'Wat' }))).toBe('confirmed')
  })

  it('resolves every phase to an icon and a label', () => {
    for (const phase of Object.keys(PHASES)) {
      expect(phaseIcon(phase)).toBeTruthy()
      expect(phaseLabel(phase)).toBeTruthy()
    }
    // An unknown phase renders nothing rather than "undefined".
    expect(phaseIcon('nope')).toBe('')
    expect(phaseIcon(null)).toBe('')
  })

  it('covers every phase the tracking timeline can emit', () => {
    // The Tracker's step keys must all resolve, or the timeline renders blank dots.
    for (const key of ['confirmed', 'preparing', 'ready', 'out', 'delivered']) {
      expect(PHASES[key]).toBeDefined()
    }
  })
})
