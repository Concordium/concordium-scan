import TokenAmount from './TokenAmount.vue'
import { setupComponent, screen } from '~/utils/testing'

const defaultProps = {
	amount: '1337421337',
}

const { render } = setupComponent(TokenAmount, { defaultProps })

describe('TokenAmount', () => {
	it('displays the amount with the correct number of decimal places', () => {
		render({
			props: {
				amount: '123456',
				fractionDigits: 2,
			},
		})

		expect(screen.getByTestId('amount')).toHaveTextContent('1,234.56')
	})

	it('displays the amount with the correct number of decimal places large numbers', () => {
		render({
			props: {
				amount: '131999000000000000',
				fractionDigits: 18,
			},
		})

		expect(screen.getByTestId('amount')).toHaveTextContent(
			'0.131999000000000000'
		)
	})

	it('displays the amount with the correct number of decimal places small numbers', () => {
		render({
			props: {
				amount: '1',
				fractionDigits: 18,
			},
		})

		expect(screen.getByTestId('amount')).toHaveTextContent(
			'0.000000000000000001'
		)
	})

	it('displays the amount with the default number of decimal places if fractionDigits prop is not provided', () => {
		render({
			props: {
				amount: '123456',
			},
		})

		expect(screen.getByTestId('amount')).toHaveTextContent('123')
	})

	it('displays the amount with the correct symbol', () => {
		render({
			props: {
				amount: '123456',
				symbol: '£',
			},
		})

		expect(screen.getByTestId('amount')).toHaveTextContent('£ 123')
	})
})
