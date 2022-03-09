import Button from './Button.vue'
import { setupComponent, screen, fireEvent } from '~/utils/testing'

const defaultSlots = {
	default: 'Hello',
}

const defaultProps = {
	onClick: () => {
		/* noop */
	},
}

const { render } = setupComponent(Button, { defaultProps, defaultSlots })

describe('Button', () => {
	it('has a label', () => {
		render({})

		const button = screen.getByText(defaultSlots.default)

		expect(button).toBeInTheDocument()
	})

	it('can be clicked', () => {
		const onClick = jest.fn()
		const props = { onClick }

		render({ props })

		expect(onClick).not.toHaveBeenCalled()

		fireEvent.click(screen.getByText(defaultSlots.default))

		expect(onClick).toHaveBeenCalledTimes(1)
	})

	it('can not be clicked if button is disabled', () => {
		const onClick = jest.fn()
		const props = { onClick }
		const attrs = { disabled: true }

		render({ props, attrs })

		expect(onClick).not.toHaveBeenCalled()

		fireEvent.click(screen.getByText(defaultSlots.default))

		expect(onClick).not.toHaveBeenCalled()
	})

	it('can inherit aria-attributes', () => {
		const onClick = jest.fn()
		const props = { onClick }
		const attrs = { 'aria-label': 'Click me!' }

		render({ props, attrs })

		expect(screen.getByLabelText('Click me!')).toBeInTheDocument()
	})
})
