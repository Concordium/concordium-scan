import BakerLink from './BakerLink.vue'
import { setupComponent, screen, fireEvent } from '~/utils/testing'

const mockDrawer = { push: jest.fn() }

jest.mock('~/composables/useDrawer', () => ({
	useDrawer: () => mockDrawer,
}))

const defaultProps = {
	id: 1337,
}

const { render } = setupComponent(BakerLink, {
	defaultProps,
})

describe('BakerLink', () => {
	it('will show the baker ID', () => {
		render({})

		expect(screen.getByText(1337)).toBeInTheDocument()
	})

	it('will open the drawer when clicking the link', () => {
		render({})

		const pushSpy = jest.spyOn(mockDrawer, 'push')

		expect(pushSpy).not.toHaveBeenCalled()

		fireEvent.click(screen.getByText(1337))

		expect(pushSpy).toHaveBeenCalledWith({
			entityTypeName: 'baker',
			bakerId: 1337,
		})
	})
})
