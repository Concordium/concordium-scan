import Amount from './Amount.vue'
import { setupComponent, screen } from '~/utils/testing'

const defaultProps = {
	amount: 1337421337,
}

const { render } = setupComponent(Amount, { defaultProps })

describe('Amount', () => {
	it('will show a formatted amount', () => {
		render({})

		expect(screen.getByTestId('amount')).toHaveTextContent('1,337.421337')
	})

	it('can show a CCD symbol', () => {
		const props = { showSymbol: true }
		render({ props })

		expect(screen.getByTestId('amount')).toHaveTextContent('Ͼ 1,337.421337')
	})

	it('should render negative amounts', () => {
		const props = { amount: -1457511 }
		render({ props })
		expect(screen.getByTestId('amount')).toHaveTextContent('-1.457511')
	})
})
