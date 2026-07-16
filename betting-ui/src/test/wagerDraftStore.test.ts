// @vitest-environment node
import { describe, it, expect, beforeEach } from 'vitest'
import { useWagerDraftStore } from '@/stores/wagerDraftStore'

describe('wagerDraftStore', () => {
  beforeEach(() => {
    useWagerDraftStore.setState({
      customerId: null,
      wagerType: 'Straight',
      riskAmount: '',
      items: [],
    })
  })

  it('adds a wager item', () => {
    useWagerDraftStore.getState().addItem({
      gamePeriodId: 1,
      homeTeam: 'Team A',
      awayTeam: 'Team B',
      periodDescription: 'Full Game',
      itemType: 'Spread',
      side: 'Home',
      line: -3.5,
    })
    expect(useWagerDraftStore.getState().items).toHaveLength(1)
  })

  it('replaces item with same gamePeriodId', () => {
    const add = useWagerDraftStore.getState().addItem
    add({ gamePeriodId: 1, homeTeam: 'A', awayTeam: 'B', periodDescription: 'FG', itemType: 'Spread', side: 'Home', line: -3 })
    add({ gamePeriodId: 1, homeTeam: 'A', awayTeam: 'B', periodDescription: 'FG', itemType: 'MoneyLine', side: 'Away', line: 130 })
    const items = useWagerDraftStore.getState().items
    expect(items).toHaveLength(1)
    expect(items[0].itemType).toBe('MoneyLine')
  })

  it('removes a wager item', () => {
    useWagerDraftStore.getState().addItem({
      gamePeriodId: 5,
      homeTeam: 'A',
      awayTeam: 'B',
      periodDescription: 'FG',
      itemType: 'Total',
      side: 'Over',
      line: 47.5,
    })
    useWagerDraftStore.getState().removeItem(5)
    expect(useWagerDraftStore.getState().items).toHaveLength(0)
  })

  it('clears draft', () => {
    useWagerDraftStore.getState().setCustomer(42)
    useWagerDraftStore.getState().setRiskAmount('110')
    useWagerDraftStore.getState().addItem({
      gamePeriodId: 1, homeTeam: 'A', awayTeam: 'B', periodDescription: 'FG',
      itemType: 'Spread', side: 'Home', line: -3,
    })
    useWagerDraftStore.getState().clearDraft()
    const s = useWagerDraftStore.getState()
    expect(s.customerId).toBeNull()
    expect(s.riskAmount).toBe('')
    expect(s.items).toHaveLength(0)
  })
})
