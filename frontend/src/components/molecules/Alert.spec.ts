import Alert from './Alert.vue'
import { setupComponent, screen } from '~/utils/testing'

const defaultSlots = {
	default: 'Hello, World!',
}

const { render } = setupComponent(Alert, {
	defaultSlots,
})

describe('Alert', () => {
	it('will show a heading', () => {
		render({})

		expect(screen.getByRole('heading')).toHaveTextContent('Hello, World!')
	})

	it('will show secondary text content', () => {
		const slots = {
			secondary: 'More text!',
		}
		render({ slots })

		expect(screen.getByText('More text!')).toBeInTheDocument()
	})
})
