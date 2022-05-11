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
})
