import { shallowMount } from '@vue/test-utils'
import TokenAmount from './TokenAmount.vue'
import { setupComponent, screen } from '~/utils/testing'

const defaultProps = {
	amount: 1337421337,
}

const { render } = setupComponent(TokenAmount, { defaultProps })

describe('TokenAmount', () => {
	it('displays the amount with the correct number of decimal places', () => {
		render({
			props: {
				amount: 123.456,
				fractionDigits: 2,
			},
		})

		expect(screen.getByTestId('amount')).toHaveTextContent('123.46')
	})

	it('displays the amount with the default number of decimal places if fractionDigits prop is not provided', () => {
		render({
			props: {
				amount: 123.456,
			},
		})

		expect(screen.getByTestId('amount')).toHaveTextContent('123')
	})

	it('displays the amount with the correct symbol', () => {
		render({
			props: {
				amount: 123.456,
				symbol: '£',
			},
		})

		expect(screen.getByTestId('amount')).toHaveTextContent('£ 123')
	})
})
