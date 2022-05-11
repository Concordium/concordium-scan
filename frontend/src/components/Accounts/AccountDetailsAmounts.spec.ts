import AccountDetailsAmounts from './AccountDetailsAmounts.vue'
import { setupComponent, screen, fireEvent, waitFor } from '~/utils/testing'

const defaultProps = {
	account: {
		amount: 1337421337,
		releaseSchedule: {
			totalAmount: 0,
		},
		baker: {
			state: {
				__typename: 'RemovedBakerState',
				stakedAmount: 0,
			},
		},
	},
}

const { render } = setupComponent(AccountDetailsAmounts, {
	defaultProps,
})

describe('AccountDetailsAmounts', () => {
	it('shows the title and amount', () => {
		render({})

		expect(screen.getByText('Balance (Ͼ)')).toBeInTheDocument()
		expect(screen.getByTestId('total-balance')).toHaveTextContent(
			'1,337.421337'
		)
	})

	it('will not show a locked or staked amount if neither is available', () => {
		render({})

		expect(screen.queryByText('Locked')).not.toBeInTheDocument()
		expect(screen.queryByText('Staked')).not.toBeInTheDocument()
	})

	describe('when there is a locked amount', () => {
		it('will show the locked amount', () => {
			const props = {
				account: {
					...defaultProps.account,
					releaseSchedule: {
						totalAmount: 42133742,
					},
				},
			}
			render({ props })

			expect(screen.getByText('Locked')).toBeInTheDocument()
			expect(screen.getByTestId('locked-amount')).toHaveTextContent('42.133742')
		})

		it('will show a tooltip on the locked amount', async () => {
			const props = {
				account: {
					...defaultProps.account,
					releaseSchedule: {
						totalAmount: 42133742,
					},
				},
			}
			render({ props })

			fireEvent.mouseEnter(screen.getByTestId('locked-amount'))

			const tooltipText = await screen.findByText(
				'3.15% of account balance is locked'
			)

			waitFor(() => {
				expect(tooltipText).toBeVisible()
			})
		})
	})

	describe('when there is a staked amount', () => {
		it('will show the staked amount if it is available', () => {
			const props = {
				account: {
					...defaultProps.account,
					baker: {
						state: {
							__typename: 'ActiveBakerState',
							stakedAmount: 42133742,
						},
					},
				},
			}
			render({ props })

			expect(screen.getByText('Staked')).toBeInTheDocument()
			expect(screen.getByTestId('staked-amount')).toHaveTextContent('42.133742')
		})

		it('will show a tooltip on the staked amount', async () => {
			const props = {
				account: {
					...defaultProps.account,
					baker: {
						state: {
							__typename: 'ActiveBakerState',
							stakedAmount: 42133742,
						},
					},
				},
			}
			render({ props })

			fireEvent.mouseEnter(screen.getByTestId('staked-amount'))

			const tooltipText = await screen.findByText(
				'3.15% of account balance is staked'
			)

			waitFor(() => {
				expect(tooltipText).toBeVisible()
			})
		})
	})
})
