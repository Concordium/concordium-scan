import { render, screen, fireEvent } from '@testing-library/vue'
import Button from '../components/atoms/Button.vue'

describe('Button', () => {
	it('has a label', () => {
		render(Button, {
			slots: { default: 'Hello' },
		})

		const button = screen.getByText('Hello')

		expect(button).toBeInTheDocument()
	})

	it('can be clicked', () => {
		const onClick = jest.fn()

		render(Button, {
			slots: { default: 'Hello' },
			props: {
				onClick,
			},
		})

		expect(onClick).not.toHaveBeenCalled()

		fireEvent.click(screen.getByText('Hello'))

		expect(onClick).toHaveBeenCalledTimes(1)
	})
})
